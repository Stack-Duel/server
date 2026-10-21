using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.ValueTypes;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.ValueTypes;

internal sealed class ValueTypeUsageRepository(StackDuelDbContext db) : IValueTypeUsageRepository
{
    public async Task<bool> IsKeyInUseAsync(string key, CancellationToken cancellationToken = default)
    {
        return await IsUsedByTestCasesAsync(key, cancellationToken)
            || await IsUsedByGeneratedSpecsAsync(key, cancellationToken)
            || await IsUsedByProblemGenerationSpecsAsync(key, cancellationToken);
    }

    private async Task<bool> IsUsedByTestCasesAsync(string key, CancellationToken cancellationToken)
    {
        if (await db.Set<TestCaseInput>().AnyAsync(input => input.ValueType == key, cancellationToken))
            return true;

        return await db.Set<TestCaseExpectedOutput>()
            .AnyAsync(expectedOutput => expectedOutput.ValueType == key, cancellationToken);
    }

    private async Task<bool> IsUsedByGeneratedSpecsAsync(string key, CancellationToken cancellationToken)
    {
        if (
            await db.Set<TestCaseGenerationSpec>()
                .AnyAsync(spec => spec.OutputValueType == key, cancellationToken)
        )
            return true;

        List<IReadOnlyList<Domain.TestCaseGeneration.ValueObjects.GenerationParameterSpec>> parameterSets = await db
            .Set<TestCaseGenerationSpec>()
            .AsNoTracking()
            .Select(spec => spec.Parameters)
            .ToListAsync(cancellationToken);

        return parameterSets.Any(parameters => parameters.Any(parameter => parameter.ValueType == key));
    }

    private async Task<bool> IsUsedByProblemGenerationSpecsAsync(string key, CancellationToken cancellationToken)
    {
        List<ProblemGenerationSpec> specs = await db
            .Set<Problem>()
            .AsNoTracking()
            .Where(problem => problem.GenerationSpec != null)
            .Select(problem => problem.GenerationSpec!)
            .ToListAsync(cancellationToken);

        return specs.Any(spec =>
            spec.OutputValueType == key || spec.Parameters.Any(parameter => parameter.ValueType == key)
        );
    }
}