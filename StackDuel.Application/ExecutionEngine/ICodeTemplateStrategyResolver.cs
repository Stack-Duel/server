namespace StackDuel.Application.ExecutionEngine;

public interface ICodeTemplateStrategyResolver
{
    ICodeTemplateStrategy Resolve(string languageName);
}