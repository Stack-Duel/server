using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.IO.Compression;
using System.Text;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class TemplateZipWriterTests
{
    private static byte[] Bytes(string content) => Encoding.UTF8.GetBytes(content);

    private static (TemplateZipWriter Writer, ZipArchive Archive, MemoryStream Stream) CreateWriter()
    {
        MemoryStream stream = new();
        ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true);
        return (new TemplateZipWriter(archive), archive, stream);
    }

    [Fact]
    public void AddUserEntry_WritesAFileThatDoesNotCollideWithTheRuntime()
    {
        (TemplateZipWriter writer, ZipArchive archive, MemoryStream stream) = CreateWriter();

        writer.AddRuntimeEntry("harness.js", Bytes("grader"));
        writer.AddUserEntry("helper.js", Bytes("helper"));
        archive.Dispose();

        stream.Position = 0;
        using ZipArchive read = new(stream, ZipArchiveMode.Read);
        Assert.Equal(["harness.js", "helper.js"], read.Entries.Select(e => e.FullName).Order());
    }

    [Fact]
    public void AddUserEntry_RejectsANameAlreadyTakenByTheRuntime()
    {
        (TemplateZipWriter writer, ZipArchive archive, _) = CreateWriter();

        writer.AddRuntimeEntry("harness.js", Bytes("grader"));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => writer.AddUserEntry("harness.js", Bytes("cheat"))
        );

        Assert.Contains("reserved", error.Message, StringComparison.OrdinalIgnoreCase);
        archive.Dispose();
    }

    [Fact]
    public void AddUserEntry_IgnoresCasingWhenDetectingACollision()
    {
        (TemplateZipWriter writer, ZipArchive archive, _) = CreateWriter();

        writer.AddRuntimeEntry("harness.js", Bytes("grader"));

        Assert.Throws<InvalidOperationException>(() => writer.AddUserEntry("HARNESS.JS", Bytes("cheat")));
        archive.Dispose();
    }

    [Fact]
    public void AddUserEntry_RejectsASecondUserFileWithTheSameName()
    {
        (TemplateZipWriter writer, ZipArchive archive, _) = CreateWriter();

        writer.AddUserEntry("helper.js", Bytes("one"));

        Assert.Throws<InvalidOperationException>(() => writer.AddUserEntry("helper.js", Bytes("two")));
        archive.Dispose();
    }

    [Theory]
    [InlineData("../harness.js")]
    [InlineData("../../run")]
    [InlineData("nested/helper.js")]
    [InlineData("nested\\helper.js")]
    public void AddUserEntry_RejectsANameThatIsNotAPlainFileName(string name)
    {
        (TemplateZipWriter writer, ZipArchive archive, _) = CreateWriter();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => writer.AddUserEntry(name, Bytes("payload"))
        );

        Assert.Contains("plain file name", error.Message, StringComparison.OrdinalIgnoreCase);
        archive.Dispose();
    }

    [Fact]
    public void AddRuntimeEntry_WritesTheContentItWasGiven()
    {
        (TemplateZipWriter writer, ZipArchive archive, MemoryStream stream) = CreateWriter();

        writer.AddRuntimeEntry("run", Bytes("#!/bin/bash\nnode harness.js\n"));
        archive.Dispose();

        stream.Position = 0;
        using ZipArchive read = new(stream, ZipArchiveMode.Read);
        using StreamReader reader = new(read.GetEntry("run")!.Open());
        Assert.Equal("#!/bin/bash\nnode harness.js\n", reader.ReadToEnd());
    }
}