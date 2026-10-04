using StackDuel.Application.ExecutionEngine;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed class PythonCodeTemplateStrategy : ICodeTemplateStrategy
{
    public string LanguageName => "Python";

    public string Render(CodeTemplateContext context)
    {
        return $$"""
            {{context.UserCode}}

            import sys
            import json

            data = sys.stdin.read()
            args = json.loads(data)
            result = {{context.FunctionName}}(*args)
            # print(result) would coerce via str() — str(True) is "True", not valid JSON
            # and not the lowercase "true"/"false" convention stored expected outputs use.
            # Strings stay raw/unquoted (matching that convention); everything else goes
            # through json.dumps so booleans, None, and numbers serialize correctly.
            output = result if isinstance(result, str) else json.dumps(result)
            print(output)
            """;
    }
}