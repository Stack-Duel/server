using System.Net.WebSockets;
using System.Text;

namespace StackDuel.Api.Tests.LanguageServer;

internal sealed class FakeWebSocket : WebSocket
{
    private sealed record PendingMessage(byte[] Payload, WebSocketMessageType Type)
    {
        public int Offset { get; set; }
    }

    private readonly Queue<PendingMessage> _incoming = new();

    public List<string> Sent { get; } = [];

    public WebSocketCloseStatus? SentCloseStatus { get; private set; }

    public override WebSocketCloseStatus? CloseStatus { get; }

    public override string? CloseStatusDescription { get; }

    public override WebSocketState State => _state;

    private WebSocketState _state = WebSocketState.Open;

    public override string? SubProtocol { get; }

    public void EnqueueText(string message) =>
        _incoming.Enqueue(new PendingMessage(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text));

    public void EnqueueText(byte[] payload) =>
        _incoming.Enqueue(new PendingMessage(payload, WebSocketMessageType.Text));

    public void EnqueueClose() => _incoming.Enqueue(new PendingMessage([], WebSocketMessageType.Close));

    public override void Abort() { }

    public override Task CloseAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken
    )
    {
        SentCloseStatus = closeStatus;
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken
    )
    {
        SentCloseStatus = closeStatus;
        _state = WebSocketState.CloseSent;
        return Task.CompletedTask;
    }

    public override void Dispose() { }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken
    )
    {
        if (_incoming.Count == 0)
            await Task.Delay(Timeout.Infinite, cancellationToken);

        PendingMessage message = _incoming.Peek();

        if (message.Type == WebSocketMessageType.Close)
        {
            _incoming.Dequeue();
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        }

        int remaining = message.Payload.Length - message.Offset;
        int count = Math.Min(remaining, buffer.Count);

        Array.Copy(message.Payload, message.Offset, buffer.Array!, buffer.Offset, count);
        message.Offset += count;

        bool endOfMessage = message.Offset >= message.Payload.Length;
        if (endOfMessage)
            _incoming.Dequeue();

        return new WebSocketReceiveResult(count, message.Type, endOfMessage);
    }

    public override Task SendAsync(
        ArraySegment<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken
    )
    {
        Sent.Add(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
        return Task.CompletedTask;
    }
}
