using StackDuel.Application.LanguageServer;
using StackDuel.Application.Services.Users;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;

namespace StackDuel.Api.LanguageServer;

internal static class LanguageServerBridgeEndpoint
{
    private const int ReceiveBufferSize = 64 * 1024;

    private const int MaxMessageBytes = 8 * 1024 * 1024;

    private static readonly TimeSpan PumpDrainTimeout = TimeSpan.FromSeconds(5);

    public static async Task HandleAsync(
        HttpContext context,
        ILanguageServerSessionManager sessionManager,
        IUserService userService,
        ILoggerFactory loggerFactory
    )
    {
        ILogger logger = loggerFactory.CreateLogger(typeof(LanguageServerBridgeEndpoint));

        if (!context.WebSockets.IsWebSocketRequest)
        {
            BridgeLog.RejectedRequest(logger, "the request was not a WebSocket upgrade");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!Guid.TryParse(context.Request.Query["sessionId"], out Guid sessionId))
        {
            BridgeLog.RejectedRequest(logger, "the sessionId query parameter was missing or malformed");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        string? sub = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            BridgeLog.RejectedRequest(logger, "the caller had no subject claim");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userResult = await userService.GetBySubAsync(sub, context.RequestAborted);
        if (!userResult.IsSuccess)
        {
            BridgeLog.RejectedRequest(logger, "the caller's account could not be resolved");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Guid userId = userResult.Value.Id;

        ILanguageServerProcess? process = sessionManager.TryAttach(sessionId, userId);
        if (process is null)
        {
            BridgeLog.AttachRejected(logger, sessionId, userId);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();

        try
        {
            await PumpAsync(socket, process, sessionManager, sessionId, logger, context.RequestAborted);
        }
        finally
        {
            sessionManager.Detach(sessionId);
            socket.Dispose();
        }
    }

    private static async Task PumpAsync(
        WebSocket socket,
        ILanguageServerProcess process,
        ILanguageServerSessionManager sessionManager,
        Guid sessionId,
        ILogger logger,
        CancellationToken requestAborted
    )
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);

        Task stdioToWs = PumpStdioToWebSocketAsync(process, socket, sessionManager, sessionId, cts.Token);
        Task wsToStdio = PumpWebSocketToStdioAsync(socket, process, sessionManager, sessionId, logger, cts.Token);

        await Task.WhenAny(stdioToWs, wsToStdio, process.Completion);
        await cts.CancelAsync();

        await DrainPumpsAsync(stdioToWs, wsToStdio, sessionId, logger);

        if (socket.State == WebSocketState.Open)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
            }
            catch (WebSocketException) { }
        }
    }

    private static async Task DrainPumpsAsync(Task stdioToWs, Task wsToStdio, Guid sessionId, ILogger logger)
    {
        try
        {
            await Task.WhenAll(stdioToWs, wsToStdio).WaitAsync(PumpDrainTimeout);
        }
        catch (TimeoutException)
        {
            BridgeLog.PumpDrainTimedOut(logger, sessionId);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            BridgeLog.PumpFaulted(logger, sessionId, ex);
        }
    }

    private static async Task PumpStdioToWebSocketAsync(
        ILanguageServerProcess process,
        WebSocket socket,
        ILanguageServerSessionManager sessionManager,
        Guid sessionId,
        CancellationToken cancellationToken
    )
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? message;
            try
            {
                message = await LspFraming.ReadMessageAsync(process.Output, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
            {
                return;
            }

            if (message is null)
                return;

            sessionManager.Touch(sessionId);
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken
                );
            }
            catch (Exception ex)
                when (ex is WebSocketException or ObjectDisposedException or OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task PumpWebSocketToStdioAsync(
        WebSocket socket,
        ILanguageServerProcess process,
        ILanguageServerSessionManager sessionManager,
        Guid sessionId,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        byte[] buffer = new byte[ReceiveBufferSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            using MemoryStream messageBuffer = new();
            WebSocketReceiveResult result;

            do
            {
                try
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (Exception ex)
                    when (ex is WebSocketException or ObjectDisposedException or OperationCanceledException)
                {
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                if (messageBuffer.Length + result.Count > MaxMessageBytes)
                {
                    BridgeLog.MessageTooLarge(logger, sessionId, MaxMessageBytes);
                    await CloseForOversizedMessageAsync(socket);
                    return;
                }

                messageBuffer.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            string message = Encoding.UTF8.GetString(messageBuffer.ToArray());
            sessionManager.Touch(sessionId);

            try
            {
                await LspFraming.WriteMessageAsync(process.Input, message, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task CloseForOversizedMessageAsync(WebSocket socket)
    {
        if (socket.State != WebSocketState.Open)
            return;

        try
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.MessageTooBig,
                "message too large",
                CancellationToken.None
            );
        }
        catch (WebSocketException) { }
    }
}

internal static partial class BridgeLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Rejected language server bridge request: {Reason}.")]
    public static partial void RejectedRequest(ILogger logger, string reason);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Language server session {SessionId} could not be attached for user {UserId}."
    )]
    public static partial void AttachRejected(ILogger logger, Guid sessionId, Guid userId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Language server session {SessionId} sent a message larger than {MaxMessageBytes} bytes; closing the socket."
    )]
    public static partial void MessageTooLarge(ILogger logger, Guid sessionId, int maxMessageBytes);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Language server session {SessionId} pumps did not drain before the timeout."
    )]
    public static partial void PumpDrainTimedOut(ILogger logger, Guid sessionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Language server session {SessionId} pump faulted.")]
    public static partial void PumpFaulted(ILogger logger, Guid sessionId, Exception exception);
}