using StackDuel.Application.ExecutionEngine;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class ReactCodeTemplateStrategy(IJsxTranspiler jsxTranspiler) : ICodeTemplateStrategy
{
    public string LanguageName => "React";

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
                "React problems require a FunctionName naming the component to render."
            );

        string wrappedSource =
            $"const React = require(\"react\");\n\n{context.UserCode}\n\nmodule.exports.Solution = {context.FunctionName};\n";
        string transpiled = jsxTranspiler.Transpile(wrappedSource);

        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            TemplateZipWriter writer = new(archive);

            foreach ((string zipPath, byte[] content) in ReactRuntimeAssets.ZipEntries)
                writer.AddRuntimeEntry(zipPath, content);

            writer.AddRuntimeEntry("solution.js", Encoding.UTF8.GetBytes(transpiled));
            writer.AddRuntimeEntry("compile", Encoding.UTF8.GetBytes("#!/bin/bash\ntrue\n"));
            writer.AddRuntimeEntry("run", Encoding.UTF8.GetBytes("#!/bin/bash\nnode harness.js\n"));

            // Extra files are plain Node modules the user writes themselves — real
            // `module.exports.X = X` and `require("./Other")`, no auto-wrapping beyond the
            // same `React` convenience the main file gets. Node's own module resolution
            // handles requiring between them once they're real files in the same directory,
            // so no custom loader is needed here — only the main file gets the "bare
            // function, no export" convenience since only it has a known entry point
            // (FunctionName).
            foreach (CodeTemplateSourceFile file in context.AdditionalFiles ?? [])
            {
                string extraWrapped = $"const React = require(\"react\");\n\n{file.Content}\n";
                string extraTranspiled = jsxTranspiler.Transpile(extraWrapped);
                writer.AddUserEntry(NormalizeToJs(file.Path), Encoding.UTF8.GetBytes(extraTranspiled));
            }
        }

        return stream.ToArray();
    }

    private static string NormalizeToJs(string path)
    {
        string withoutExtension = System.IO.Path.ChangeExtension(path, null);
        return withoutExtension + ".js";
    }
}