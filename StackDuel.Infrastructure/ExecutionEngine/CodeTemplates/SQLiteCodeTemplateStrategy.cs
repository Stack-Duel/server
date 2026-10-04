using StackDuel.Application.ExecutionEngine;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class SQLiteCodeTemplateStrategy : ICodeTemplateStrategy
{
    public string LanguageName => "SQLite";

    public string Render(CodeTemplateContext context)
    {
        string setup = context.Inputs.Count > 0 ? context.Inputs[0].Value : string.Empty;

        return $$"""
            .mode list
            .headers off
            .separator ","
            .nullvalue NULL

            {{setup}}

            {{context.UserCode.Trim()}}
            """;
    }

    public string BuildStdin(IReadOnlyList<CodeTemplateInput> inputs) => string.Empty;

    public ParsedExecutionOutput ParseOutput(string? stdout)
    {
        if (string.IsNullOrEmpty(stdout))
            return new ParsedExecutionOutput(UserLogs: null, ActualResult: null);

        string trimmed = stdout.TrimEnd('\r', '\n');
        return new ParsedExecutionOutput(
            UserLogs: null,
            ActualResult: string.IsNullOrWhiteSpace(trimmed) ? null : trimmed
        );
    }
}