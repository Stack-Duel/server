using StackDuel.Application.ExecutionEngine;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class CodeTemplateStrategyResolver(IEnumerable<ICodeTemplateStrategy> strategies)
    : ICodeTemplateStrategyResolver
{
    public ICodeTemplateStrategy Resolve(string languageName)
    {
        var strategy = strategies.FirstOrDefault(s =>
            s.LanguageName.Equals(languageName, StringComparison.OrdinalIgnoreCase)
        );

        return strategy is null
            ? throw new InvalidOperationException(
                $"No code template strategy registered for language '{languageName}'."
            )
            : strategy;
    }
}