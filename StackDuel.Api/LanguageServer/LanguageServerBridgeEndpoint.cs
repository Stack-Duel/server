using StackDuel.Application.LanguageServer;
using StackDuel.Application.Services.Users;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;

namespace StackDuel.Api.LanguageServer;

internal static class LanguageServerBridgeEndpoint
{
    public static async Task HandleAsync(
        HttpContext context,
        ILanguageServerSessionManager sessionManager,
        IUserService userService
    )
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!Guid.TryParse(context.Request.Query["sessionId"], out Guid sessionId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        string? sub = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userResult = await userService.GetBySubAsync(sub, context.RequestAborted);
        if (!userResult.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Guid userId = userResult.Value.Id;

        ILanguageServerProcess? process = sessionManager.TryAttach(sessionId, userId);
        if (process is null)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();

        try
        {
            await PumpAsync(socket, process, sessionManager, sessionId, context.RequestAborted);
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
        CancellationToken requestAborted
    )
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);

        Task stdioToWs = PumpStdioToWebSocketAsync(process, socket, sessionManager, sessionId, cts.Token);
        Task wsToStdio = PumpWebSocketToStdioAsync(socket, process, sessionManager, sessionId, cts.Token);

        await Task.WhenAny(stdioToWs, wsToStdio, process.Completion);
        await cts.CancelAsync();

        if (socket.State == WebSocketState.Open)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
            }
            catch (WebSocketException) { }
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

            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken
            );
        }
    }

    private static async Task PumpWebSocketToStdioAsync(
        WebSocket socket,
        ILanguageServerProcess process,
        ILanguageServerSessionManager sessionManager,
        Guid sessionId,
        CancellationToken cancellationToken
    )
    {
        byte[] buffer = new byte[64 * 1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            using MemoryStream messageBuffer = new();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                messageBuffer.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            string message = Encoding.UTF8.GetString(messageBuffer.ToArray());
            sessionManager.Touch(sessionId);

            try
            {
                await LspFraming.WriteMessageAsync(process.Input, message, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                return;
            }
        }
    }
}