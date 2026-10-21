using System.Text.RegularExpressions;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed partial class CppCodeTemplateStrategy
{
    [GeneratedRegex(@"\b(class|struct)\s+Solution\b")]
    private static partial Regex CppSolutionTypeRegex();
}