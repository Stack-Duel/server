using StackDuel.Application.ExecutionEngine;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class TypeScriptCodeTemplateStrategy : ICodeTemplateStrategy
{
    public string LanguageName => "TypeScript";

    public string Render(CodeTemplateContext context)
    {
        return $$"""
            declare const process: {
                stdin: { on(event: "data", listener: (data: any) => void): void };
                stdout: { write(chunk: string): void };
            };

            {{context.UserCode}}

            process.stdin.on("data", (data: any) => {
                const args: any[] = JSON.parse(data.toString().trim());
                const result = ({{context.FunctionName}} as any)(...args);
                // String(result) coerces via Array.prototype.toString() for arrays —
                // String([1,3]) is "1,3", not "[1,3]" — which never matches the stored
                // JSON-shaped expected output. Strings stay raw/unquoted (matching the
                // stored convention for string-returning problems); everything else is
                // JSON-serialized.
                const output = typeof result === "string" ? result : JSON.stringify(result);
                process.stdout.write(output.trim());
            });
            """;
    }
}