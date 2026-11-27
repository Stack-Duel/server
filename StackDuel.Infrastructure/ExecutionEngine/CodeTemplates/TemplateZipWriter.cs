using System.IO.Compression;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class TemplateZipWriter(ZipArchive archive)
{
    private static readonly string[] RuntimeReservedNames = ["harness.js", "solution.js", "compile", "run"];

    private readonly HashSet<string> _takenNames = new(RuntimeReservedNames, StringComparer.OrdinalIgnoreCase);

    public void AddRuntimeEntry(string name, byte[] content)
    {
        _takenNames.Add(name);
        Write(name, content);
    }

    public void AddUserEntry(string name, byte[] content)
    {
        if (name.Contains('/') || name.Contains('\\') || name.Contains(".."))
            throw new InvalidOperationException($"Submitted file name '{name}' is not a plain file name.");

        if (!_takenNames.Add(name))
            throw new InvalidOperationException(
                $"Submitted file name '{name}' is reserved by the execution runtime."
            );

        Write(name, content);
    }

    private void Write(string name, byte[] content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using Stream entryStream = entry.Open();
        entryStream.Write(content);
    }
}