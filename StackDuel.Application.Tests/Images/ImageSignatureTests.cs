using StackDuel.Application.Images;

namespace StackDuel.Application.Tests.Images;

public class ImageSignatureTests
{
    [Fact]
    public void Detect_PngSignature_ReturnsPng()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];

        ImageFormat? format = ImageSignature.Detect(content);

        Assert.Equal("image/png", format?.ContentType);
    }

    [Fact]
    public void Detect_JpegSignature_ReturnsJpeg()
    {
        byte[] content = [0xFF, 0xD8, 0xFF, 0, 0];

        ImageFormat? format = ImageSignature.Detect(content);

        Assert.Equal("image/jpeg", format?.ContentType);
    }

    [Fact]
    public void Detect_WebpSignature_ReturnsWebp()
    {
        byte[] content =
        [
            (byte)'R',
            (byte)'I',
            (byte)'F',
            (byte)'F',
            0,
            0,
            0,
            0,
            (byte)'W',
            (byte)'E',
            (byte)'B',
            (byte)'P',
        ];

        ImageFormat? format = ImageSignature.Detect(content);

        Assert.Equal("image/webp", format?.ContentType);
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 })]
    public void Detect_UnrecognizedOrEmptyContent_ReturnsNull(byte[] content)
    {
        ImageFormat? format = ImageSignature.Detect(content);

        Assert.Null(format);
    }

    [Fact]
    public void Detect_ContentTypeHeaderClaimingPngButBytesAreNotPng_IsNotFooled()
    {
        byte[] htmlBytes = "<html></html>"u8.ToArray();

        ImageFormat? format = ImageSignature.Detect(htmlBytes);

        Assert.Null(format);
    }

    [Theory]
    [InlineData("image/png", ".png")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/webp", ".webp")]
    public void ExtensionFor_SupportedContentType_ReturnsMatchingExtension(string contentType, string expectedExtension)
    {
        Assert.Equal(expectedExtension, ImageSignature.ExtensionFor(contentType));
    }

    [Fact]
    public void ExtensionFor_UnsupportedContentType_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ImageSignature.ExtensionFor("image/gif"));
    }
}