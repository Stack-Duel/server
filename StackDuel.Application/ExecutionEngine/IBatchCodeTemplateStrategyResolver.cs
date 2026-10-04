namespace StackDuel.Application.ExecutionEngine;

public interface IBatchCodeTemplateStrategyResolver
{
    IBatchCodeTemplateStrategy Resolve(string languageName);
}