using StackDuel.Application.ExecutionEngine;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class VanillaJsCodeTemplateStrategy : ICodeTemplateStrategy
{
    public string LanguageName => "Vanilla JS";

    public string Render(CodeTemplateContext context) => string.Empty;

    public string BuildStdin(IReadOnlyList<CodeTemplateInput> inputs)
    {
        string props = inputs.FirstOrDefault(i => i.ValueType == "props")?.Value ?? "{}";
        string actions = inputs.FirstOrDefault(i => i.ValueType == "actions")?.Value ?? "[]";

        using JsonDocument propsDoc = JsonDocument.Parse(props);
        using JsonDocument actionsDoc = JsonDocument.Parse(actions);

        return JsonSerializer.Serialize(new { props = propsDoc.RootElement, actions = actionsDoc.RootElement });
    }

    public byte[]? BuildAdditionalFiles(CodeTemplateContext context)
    {
        if (string.IsNullOrWhiteSpace(context.FunctionName))
            throw new InvalidOperationException(
                "Vanilla JS problems require a FunctionName naming the function to render."
            );

        string wrappedSource = $"{context.UserCode}\n\nmodule.exports.Solution = {context.FunctionName};\n";

        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            TemplateZipWriter writer = new(archive);

            writer.AddRuntimeEntry("harness.js", VanillaJsRuntimeAssets.Harness);
            writer.AddRuntimeEntry("solution.js", Encoding.UTF8.GetBytes(wrappedSource));
            writer.AddRuntimeEntry("compile", Encoding.UTF8.GetBytes("#!/bin/bash\ntrue\n"));
            writer.AddRuntimeEntry("run", Encoding.UTF8.GetBytes("#!/bin/bash\nnode harness.js\n"));

            foreach (CodeTemplateSourceFile file in context.AdditionalFiles ?? [])
                writer.AddUserEntry(NormalizeToJs(file.Path), Encoding.UTF8.GetBytes(file.Content));
        }

        return stream.ToArray();
    }

    private static string NormalizeToJs(string path)
    {
        string withoutExtension = System.IO.Path.ChangeExtension(path, null);
        return withoutExtension + ".js";
    }
}