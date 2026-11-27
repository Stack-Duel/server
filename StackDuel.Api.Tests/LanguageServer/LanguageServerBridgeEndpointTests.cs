using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Api.LanguageServer;
using StackDuel.Application.LanguageServer;
using StackDuel.Application.Services.Users;
using StackDuel.Application.Users.Dtos;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;

namespace StackDuel.Api.Tests.LanguageServer;

public class LanguageServerBridgeEndpointTests
{
    private readonly Mock<ILanguageServerSessionManager> _sessions = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public LanguageServerBridgeEndpointTests()
    {
        _userService
            .Setup(x => x.GetBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result<UserDto>.Success(
                    new UserDto(_userId, "auth0|abc", "alice", null, null, false, null, default, null, [])
                )
            );
    }

    private sealed class FakeWebSocketFeature(WebSocket socket) : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest { get; set; } = true;

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) => Task.FromResult(socket);
    }

    private static HttpContext CreateContext(
        WebSocket? socket,
        string? sessionId,
        string? sub = "auth0|abc",
        bool isWebSocketRequest = true
    )
    {
        DefaultHttpContext context = new();

        if (sessionId is not null)
            context.Request.QueryString = new QueryString($"?sessionId={sessionId}");

        if (sub is not null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, sub)])
            );
        }

        context.Features.Set<IHttpWebSocketFeature>(
            new FakeWebSocketFeature(socket ?? new FakeWebSocket()) { IsWebSocketRequest = isWebSocketRequest }
        );

        return context;
    }

    private Task HandleAsync(HttpContext context) =>
        LanguageServerBridgeEndpoint.HandleAsync(
            context,
            _sessions.Object,
            _userService.Object,
            NullLoggerFactory.Instance
        );

    private static ILanguageServerProcess ProcessWith(string stdout, Stream? stdin = null)
    {
        Mock<ILanguageServerProcess> process = new();
        process.SetupGet(x => x.Output).Returns(new MemoryStream(Encoding.UTF8.GetBytes(stdout)));
        process.SetupGet(x => x.Input).Returns(stdin ?? new MemoryStream());
        process.SetupGet(x => x.Completion).Returns(new TaskCompletionSource().Task);
        return process.Object;
    }

    [Fact]
    public async Task HandleAsync_RejectsARequestThatIsNotAWebSocketUpgrade()
    {
        HttpContext context = CreateContext(null, Guid.NewGuid().ToString(), isWebSocketRequest: false);

        await HandleAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_RejectsAMissingSessionId()
    {
        HttpContext context = CreateContext(null, sessionId: null);

        await HandleAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_RejectsAMalformedSessionId()
    {
        HttpContext context = CreateContext(null, "not-a-guid");

        await HandleAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_RejectsACallerWithNoSubjectClaim()
    {
        HttpContext context = CreateContext(null, Guid.NewGuid().ToString(), sub: null);

        await HandleAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_RejectsACallerWhoseAccountCannotBeResolved()
    {
        _userService
            .Setup(x => x.GetBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.NotFound());
        HttpContext context = CreateContext(null, Guid.NewGuid().ToString());

        await HandleAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflictWhenTheSessionCannotBeAttached()
    {
        _sessions.Setup(x => x.TryAttach(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns((ILanguageServerProcess?)null);
        HttpContext context = CreateContext(null, _sessionId.ToString());

        await HandleAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ForwardsServerOutputToTheSocketAndDetachesOnClose()
    {
        const string payload = "{\"jsonrpc\":\"2.0\"}";
        string framed = $"Content-Length: {Encoding.UTF8.GetByteCount(payload)}\r\n\r\n{payload}";

        FakeWebSocket socket = new();
        socket.EnqueueClose();
        _sessions.Setup(x => x.TryAttach(_sessionId, _userId)).Returns(ProcessWith(framed));

        await HandleAsync(CreateContext(socket, _sessionId.ToString()));

        Assert.Contains(payload, socket.Sent);
        _sessions.Verify(x => x.Detach(_sessionId), Times.Once);
        _sessions.Verify(x => x.Touch(_sessionId), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_ForwardsClientMessagesToTheServerStdin()
    {
        MemoryStream stdin = new();
        FakeWebSocket socket = new();
        socket.EnqueueText("{\"id\":1}");
        socket.EnqueueClose();
        _sessions.Setup(x => x.TryAttach(_sessionId, _userId)).Returns(ProcessWith(string.Empty, stdin));

        await HandleAsync(CreateContext(socket, _sessionId.ToString()));

        string written = Encoding.UTF8.GetString(stdin.ToArray());
        Assert.Contains("Content-Length:", written);
        Assert.Contains("{\"id\":1}", written);
    }

    [Fact]
    public async Task HandleAsync_ClosesTheSocketWhenAClientMessageExceedsTheSizeCap()
    {
        FakeWebSocket socket = new();
        socket.EnqueueText(new byte[9 * 1024 * 1024]);
        socket.EnqueueClose();

        MemoryStream stdin = new();
        _sessions.Setup(x => x.TryAttach(_sessionId, _userId)).Returns(ProcessWith(string.Empty, stdin));

        await HandleAsync(CreateContext(socket, _sessionId.ToString()));

        Assert.Equal(WebSocketCloseStatus.MessageTooBig, socket.SentCloseStatus);
        Assert.Empty(stdin.ToArray());
        _sessions.Verify(x => x.Detach(_sessionId), Times.Once);
    }
}
