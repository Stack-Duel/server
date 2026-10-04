namespace StackDuel.Application.Images;

public static class ImageSignature
{
    public static readonly IReadOnlyList<ImageFormat> SupportedFormats =
    [
        new("image/png", ".png", IsPng),
        new("image/jpeg", ".jpg", IsJpeg),
        new("image/webp", ".webp", IsWebp),
    ];

    public static ImageFormat? Detect(byte[] content) =>
        SupportedFormats.FirstOrDefault(format => format.Matches(content));

    public static string ExtensionFor(string contentType) =>
        SupportedFormats.First(format => format.ContentType == contentType).FileExtension;

    private static bool IsPng(byte[] content) =>
        content.Length >= 8
        && content[0] == 0x89
        && content[1] == 0x50
        && content[2] == 0x4E
        && content[3] == 0x47
        && content[4] == 0x0D
        && content[5] == 0x0A
        && content[6] == 0x1A
        && content[7] == 0x0A;

    private static bool IsJpeg(byte[] content) =>
        content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;

    private static bool IsWebp(byte[] content) =>
        content.Length >= 12
        && content[0] == (byte)'R'
        && content[1] == (byte)'I'
        && content[2] == (byte)'F'
        && content[3] == (byte)'F'
        && content[8] == (byte)'W'
        && content[9] == (byte)'E'
        && content[10] == (byte)'B'
        && content[11] == (byte)'P';
}