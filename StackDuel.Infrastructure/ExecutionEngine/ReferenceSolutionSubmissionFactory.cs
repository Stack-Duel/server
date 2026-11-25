using StackDuel.Application.ExecutionEngine;

namespace StackDuel.Infrastructure.ExecutionEngine;

internal static class ReferenceSolutionSubmissionFactory
{
    public static ExecutionEngineSubmission Create(
        ICodeTemplateStrategy templateStrategy,
        string referenceSolutionCode,
        string? functionName,
        IReadOnlyList<CodeTemplateInput> inputs,
        int judge0LanguageId
    )
    {
        CodeTemplateContext context = new(referenceSolutionCode, functionName, inputs);

        return new ExecutionEngineSubmission(
            SourceCode: templateStrategy.Render(context),
            LanguageId: judge0LanguageId,
            Stdin: templateStrategy.BuildStdin(inputs),
            TimeLimitMs: null,
            MemoryLimitKb: null,
            AdditionalFiles: templateStrategy.BuildAdditionalFiles(context)
        );
    }
}