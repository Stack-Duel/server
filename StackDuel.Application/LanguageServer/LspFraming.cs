using System.Text;

namespace StackDuel.Application.LanguageServer;

public static class LspFraming
{
    private const string ContentLengthHeader = "Content-Length";

    public static async Task<string?> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        int? contentLength = null;

        while (true)
        {
            string? line = await ReadHeaderLineAsync(stream, cancellationToken);
            if (line is null)
                return null;

            if (line.Length == 0)
                break;

            int separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0)
                continue;

            string name = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();

            if (
                name.Equals(ContentLengthHeader, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value, out int parsed)
            )
            {
                contentLength = parsed;
            }
        }

        if (contentLength is not int length)
            throw new InvalidDataException("LSP message header did not include a valid Content-Length.");

        byte[] body = new byte[length];
        await stream.ReadExactlyAsync(body, cancellationToken);

        return Encoding.UTF8.GetString(body);
    }

    public static async Task WriteMessageAsync(Stream stream, string message, CancellationToken cancellationToken)
    {
        byte[] body = Encoding.UTF8.GetBytes(message);
        byte[] header = Encoding.ASCII.GetBytes($"{ContentLengthHeader}: {body.Length}\r\n\r\n");

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<string?> ReadHeaderLineAsync(Stream stream, CancellationToken cancellationToken)
    {
        List<byte> bytes = [];
        byte[] single = new byte[1];

        while (true)
        {
            int read = await stream.ReadAsync(single, cancellationToken);
            if (read == 0)
                return bytes.Count == 0 ? null : throw new EndOfStreamException("Stream ended mid-header.");

            if (single[0] == '\n')
            {
                if (bytes.Count > 0 && bytes[^1] == '\r')
                    bytes.RemoveAt(bytes.Count - 1);
                break;
            }

            bytes.Add(single[0]);
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}