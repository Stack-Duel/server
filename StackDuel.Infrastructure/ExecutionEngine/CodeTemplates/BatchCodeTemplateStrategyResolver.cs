using StackDuel.Application.ExecutionEngine;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class BatchCodeTemplateStrategyResolver(IEnumerable<IBatchCodeTemplateStrategy> strategies)
    : IBatchCodeTemplateStrategyResolver
{
    public IBatchCodeTemplateStrategy Resolve(string languageName)
    {
        var strategy = strategies.FirstOrDefault(s =>
            s.LanguageName.Equals(languageName, StringComparison.OrdinalIgnoreCase)
        );

        return strategy is null
            ? throw new InvalidOperationException(
                $"No batch code template strategy registered for language '{languageName}'."
            )
            : strategy;
    }
}