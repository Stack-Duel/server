using StackDuel.Application.LanguageServer;

namespace StackDuel.Application.Tests.LanguageServer;

public class LspFramingTests
{
    [Fact]
    public async Task WriteThenRead_RoundTripsMessage()
    {
        using MemoryStream stream = new();

        await LspFraming.WriteMessageAsync(stream, "{\"hello\":\"world\"}", CancellationToken.None);
        stream.Position = 0;

        string? message = await LspFraming.ReadMessageAsync(stream, CancellationToken.None);

        Assert.Equal("{\"hello\":\"world\"}", message);
    }

    [Fact]
    public async Task WriteThenRead_HandlesMultiByteUtf8ContentLengthCorrectly()
    {
        using MemoryStream stream = new();
        string message = "{\"emoji\":\"🚀\",\"text\":\"café\"}";

        await LspFraming.WriteMessageAsync(stream, message, CancellationToken.None);
        stream.Position = 0;

        string? read = await LspFraming.ReadMessageAsync(stream, CancellationToken.None);

        Assert.Equal(message, read);
    }

    [Fact]
    public async Task ReadMessageAsync_ReadsMultipleSequentialMessages()
    {
        using MemoryStream stream = new();
        await LspFraming.WriteMessageAsync(stream, "first", CancellationToken.None);
        await LspFraming.WriteMessageAsync(stream, "second", CancellationToken.None);
        stream.Position = 0;

        string? first = await LspFraming.ReadMessageAsync(stream, CancellationToken.None);
        string? second = await LspFraming.ReadMessageAsync(stream, CancellationToken.None);

        Assert.Equal("first", first);
        Assert.Equal("second", second);
    }

    [Fact]
    public async Task ReadMessageAsync_ToleratesAdditionalHeaders()
    {
        using MemoryStream stream = new();
        byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
        byte[] header = System.Text.Encoding.ASCII.GetBytes(
            $"Content-Type: application/vscode-jsonrpc; charset=utf-8\r\nContent-Length: {body.Length}\r\n\r\n"
        );
        await stream.WriteAsync(header);
        await stream.WriteAsync(body);
        stream.Position = 0;

        string? message = await LspFraming.ReadMessageAsync(stream, CancellationToken.None);

        Assert.Equal("{}", message);
    }

    [Fact]
    public async Task ReadMessageAsync_EmptyStream_ReturnsNull()
    {
        using MemoryStream stream = new();

        string? message = await LspFraming.ReadMessageAsync(stream, CancellationToken.None);

        Assert.Null(message);
    }

    [Fact]
    public async Task ReadMessageAsync_MissingContentLength_Throws()
    {
        using MemoryStream stream = new();
        byte[] header = System.Text.Encoding.ASCII.GetBytes("Content-Type: application/json\r\n\r\n");
        stream.Write(header);
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            LspFraming.ReadMessageAsync(stream, CancellationToken.None)
        );
    }
}