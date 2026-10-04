using StackDuel.Application.Images;

namespace StackDuel.Application.Tests.Images;

public class ImageSignatureTests
{
    [Test]
    public void Detect_PngSignature_ReturnsPng()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];

        ImageFormat? format = ImageSignature.Detect(content);

        Assert.That(format?.ContentType, Is.EqualTo("image/png"));
    }

    [Test]
    public void Detect_JpegSignature_ReturnsJpeg()
    {
        byte[] content = [0xFF, 0xD8, 0xFF, 0, 0];

        ImageFormat? format = ImageSignature.Detect(content);

        Assert.That(format?.ContentType, Is.EqualTo("image/jpeg"));
    }

    [Test]
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

        Assert.That(format?.ContentType, Is.EqualTo("image/webp"));
    }

    [TestCase(new byte[] { })]
    [TestCase(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    [TestCase(new byte[] { 0x25, 0x50, 0x44, 0x46 })]
    public void Detect_UnrecognizedOrEmptyContent_ReturnsNull(byte[] content)
    {
        ImageFormat? format = ImageSignature.Detect(content);

        Assert.That(format, Is.Null);
    }

    [Test]
    public void Detect_ContentTypeHeaderClaimingPngButBytesAreNotPng_IsNotFooled()
    {
        byte[] htmlBytes = "<html></html>"u8.ToArray();

        ImageFormat? format = ImageSignature.Detect(htmlBytes);

        Assert.That(format, Is.Null);
    }

    [TestCase("image/png", ".png")]
    [TestCase("image/jpeg", ".jpg")]
    [TestCase("image/webp", ".webp")]
    public void ExtensionFor_SupportedContentType_ReturnsMatchingExtension(string contentType, string expectedExtension)
    {
        Assert.That(ImageSignature.ExtensionFor(contentType), Is.EqualTo(expectedExtension));
    }

    [Test]
    public void ExtensionFor_UnsupportedContentType_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ImageSignature.ExtensionFor("image/gif"));
    }
}