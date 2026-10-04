using Microsoft.EntityFrameworkCore;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestSuites;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Domain.TestSuites.ValueObjects;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.TestSuites;

internal sealed class TestSuiteWriteRepository(StackDuelDbContext context, ITestCaseGeneratorService testCaseGenerator)
    : ITestSuiteWriteRepository
{
    public async Task AddAsync(TestSuite testSuite, CancellationToken cancellationToken = default)
    {
        await context.TestSuites.AddAsync(testSuite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TestSuite testSuite, CancellationToken cancellationToken = default)
    {
        context.TestSuites.Update(testSuite);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TestSuite?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context
            .TestSuites.Include(s => s.TestCases)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> FindTestCaseIdsByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Problems.AsNoTracking()
            .SelectMany(p => p.Setups)
            .Where(s => s.Id == problemSetupId)
            .SelectMany(s => s.TestSuites)
            .SelectMany(ts => ts.TestCases)
            .Select(tc => tc.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> FindPublicTestCaseIdsByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Problems.AsNoTracking()
            .SelectMany(p => p.Setups)
            .Where(s => s.Id == problemSetupId)
            .SelectMany(s => s.TestSuites)
            .Where(ts => ts.Type == TestSuiteType.Sample)
            .SelectMany(ts => ts.TestCases)
            .Select(tc => tc.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> FindGradingTestCaseIdsByProblemSetupIdAsync(
        Guid problemSetupId,
        int targetCount,
        CancellationToken cancellationToken = default
    )
    {
        var rows = await context
            .Problems.AsNoTracking()
            .SelectMany(p => p.Setups)
            .Where(s => s.Id == problemSetupId)
            .SelectMany(s => s.TestSuites)
            .SelectMany(
                ts => ts.TestCases,
                (ts, tc) =>
                    new
                    {
                        tc.Id,
                        ts.Type,
                        tc.Source,
                        tc.RetiredAt,
                    }
            )
            .Where(r => r.RetiredAt == null)
            .ToListAsync(cancellationToken);

        // Guaranteed: every public example and every hand-authored hidden case — these always
        // run, they're not part of the random draw. Only the generated pool is sampled, and
        // only to fill whatever's left after the guaranteed set.
        List<Guid> guaranteedIds =
        [
            .. rows.Where(r =>
                    r.Type == TestSuiteType.Sample
                    || (r.Type == TestSuiteType.Hidden && r.Source == TestCaseSource.Authored)
                )
                .Select(r => r.Id),
        ];

        List<Guid> generatedPoolIds =
        [
            .. rows.Where(r => r.Type == TestSuiteType.Hidden && r.Source == TestCaseSource.Generated)
                .Select(r => r.Id),
        ];

        int remaining = targetCount <= 0 ? generatedPoolIds.Count : Math.Max(targetCount - guaranteedIds.Count, 0);
        int take = Math.Min(remaining, generatedPoolIds.Count);

        List<Guid> randomPicks = [.. generatedPoolIds.OrderBy(_ => Guid.NewGuid()).Take(take)];

        return [.. guaranteedIds, .. randomPicks];
    }

    public async Task<IReadOnlyList<Guid>> CreateAdHocTestCasesAsync(
        Guid problemSetupId,
        IReadOnlyCollection<IReadOnlyCollection<string>> customTestCaseInputs,
        CancellationToken cancellationToken = default
    )
    {
        if (customTestCaseInputs.Count == 0)
            return [];

        var valueTypesByPosition = await context
            .Problems.AsNoTracking()
            .SelectMany(p => p.Setups)
            .Where(s => s.Id == problemSetupId)
            .SelectMany(s => s.TestSuites)
            .SelectMany(ts => ts.TestCases)
            .Where(tc => tc.RetiredAt == null)
            .SelectMany(tc => tc.Inputs)
            .GroupBy(i => i.Position)
            .Select(g => new { Position = g.Key, ValueType = g.First().ValueType })
            .ToDictionaryAsync(g => g.Position, g => g.ValueType, cancellationToken);

        var suite = new TestSuite($"AdHoc Submission Suite {Guid.NewGuid():N}", TestSuiteType.Sample);

        int index = 1;
        foreach (var customInputs in customTestCaseInputs)
        {
            var testCase = suite.AddTestCase($"Custom {index}");
            int position = 0;
            foreach (string input in customInputs)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    position++;
                    continue;
                }

                string valueType = valueTypesByPosition.GetValueOrDefault(position, "json");
                testCase.AddInput(input, valueType);
                position++;
            }

            index++;
        }

        await context.TestSuites.AddAsync(suite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return [.. suite.TestCases.Select(tc => tc.Id)];
    }

    public async Task<Dictionary<Guid, string>> FindExpectedOutputsByTestCaseIdsAsync(
        IEnumerable<Guid> testCaseIds,
        CancellationToken cancellationToken = default
    )
    {
        var ids = testCaseIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var rows = await context
            .TestSuites.AsNoTracking()
            .SelectMany(ts => ts.TestCases)
            .Where(tc => ids.Contains(tc.Id))
            .Select(tc => new
            {
                tc.Id,
                ExpectedOutput = string.Join(", ", tc.ExpectedOutputs.Select(output => output.Value)),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Id, x => x.ExpectedOutput);
    }

    public async Task<Dictionary<Guid, string>> FindInputsByTestCaseIdsAsync(
        IEnumerable<Guid> testCaseIds,
        CancellationToken cancellationToken = default
    )
    {
        var ids = testCaseIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var testCases = await context
            .Set<TestCase>()
            .AsNoTracking()
            .Include(tc => tc.Inputs.OrderBy(i => i.Position))
            .Where(tc => ids.Contains(tc.Id))
            .ToListAsync(cancellationToken);

        var generationSpecIds = testCases
            .Where(tc => tc.Source == TestCaseSource.Generated)
            .Select(tc => tc.GenerationSpecId!.Value)
            .Distinct()
            .ToArray();

        Dictionary<Guid, TestCaseGenerationSpec> specsById = [];
        if (generationSpecIds.Length > 0)
        {
            specsById = await context
                .Set<TestCaseGenerationSpec>()
                .AsNoTracking()
                .Where(s => generationSpecIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);
        }

        var result = new Dictionary<Guid, string>();
        foreach (var testCase in testCases)
        {
            if (
                testCase.Source == TestCaseSource.Generated
                && testCase.GenerationSpecId is Guid specId
                && testCase.GenerationCaseIndex is int caseIndex
                && specsById.TryGetValue(specId, out var spec)
            )
            {
                var generatedInputs = testCaseGenerator.GenerateInputs(spec, caseIndex);
                result[testCase.Id] = string.Join(", ", generatedInputs.Select(v => v.Value));
            }
            else
            {
                result[testCase.Id] = string.Join(", ", testCase.Inputs.Select(i => i.Value));
            }
        }

        return result;
    }

    public async Task<Guid?> FindPipelineIdByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Problems.AsNoTracking()
            .SelectMany(p => p.Setups)
            .Where(s => s.Id == problemSetupId)
            .Select(s => (Guid?)s.PipelineId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> FindOrCreateGeneratedSuiteAsync(
        string suiteName,
        IReadOnlyCollection<Guid> problemSetupIds,
        CancellationToken cancellationToken = default
    )
    {
        TestSuite? existing = await context
            .TestSuites.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == suiteName, cancellationToken);

        Guid suiteId;
        if (existing is null)
        {
            var suite = new TestSuite(suiteName, TestSuiteType.Hidden);
            await context.TestSuites.AddAsync(suite, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            suiteId = suite.Id;
        }
        else
        {
            suiteId = existing.Id;
        }

        foreach (Guid setupId in problemSetupIds)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO problem_setup_test_suites (problem_setup_id, test_suite_id)
                VALUES ({setupId}, {suiteId})
                ON CONFLICT DO NOTHING
                """,
                cancellationToken
            );
        }

        return suiteId;
    }

    public async Task RetireGeneratedTestCasesAsync(Guid testSuiteId, CancellationToken cancellationToken = default)
    {
        // Retire (not delete) — a submission_result FK is ON DELETE RESTRICT, so a generated
        // case that's ever been graded against can never actually be removed. Retired cases
        // are simply excluded from FindGradingTestCaseIdsByProblemSetupIdAsync's selection.
        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE test_cases
            SET retired_at = {DateTime.UtcNow}
            WHERE test_suite_id = {testSuiteId} AND source = {(int)TestCaseSource.Generated} AND retired_at IS NULL
            """,
            cancellationToken
        );
    }

    public async Task ReplaceSampleTestCasesAsync(
        IReadOnlyCollection<Guid> problemSetupIds,
        string suiteName,
        IReadOnlyList<AuthoredTestCaseSpec> testCases,
        CancellationToken cancellationToken = default
    )
    {
        TestSuite? existing = await context
            .TestSuites.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == suiteName, cancellationToken);

        Guid suiteId;
        if (existing is null)
        {
            var suite = new TestSuite(suiteName, TestSuiteType.Sample);
            await context.TestSuites.AddAsync(suite, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            suiteId = suite.Id;
        }
        else
        {
            suiteId = existing.Id;
        }

        foreach (Guid problemSetupId in problemSetupIds)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO problem_setup_test_suites (problem_setup_id, test_suite_id)
                VALUES ({problemSetupId}, {suiteId})
                ON CONFLICT DO NOTHING
                """,
                cancellationToken
            );
        }

        // Retire (not delete) the suite's previous authored cases — same reasoning as
        // RetireGeneratedTestCasesAsync: a submission may already have graded against one.
        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE test_cases
            SET retired_at = {DateTime.UtcNow}
            WHERE test_suite_id = {suiteId} AND source = {(int)TestCaseSource.Authored} AND retired_at IS NULL
            """,
            cancellationToken
        );

        TestSuite reloadedSuite =
            await FindByIdAsync(suiteId, cancellationToken)
            ?? throw new InvalidOperationException($"Sample suite {suiteId} could not be reloaded.");

        int index = 1;
        foreach (AuthoredTestCaseSpec spec in testCases)
        {
            TestCase testCase = reloadedSuite.AddTestCase(spec.Name ?? $"Sample {index}");
            context.Entry(testCase).State = EntityState.Added;

            foreach ((string value, string valueType) in spec.Inputs)
            {
                TestCaseInput input = testCase.AddInput(value, valueType);
                context.Entry(input).State = EntityState.Added;
            }

            TestCaseExpectedOutput output = testCase.AddExpectedOutput(
                spec.ExpectedOutputValue,
                spec.ExpectedOutputValueType
            );
            context.Entry(output).State = EntityState.Added;

            index++;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}