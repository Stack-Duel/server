using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.ValueObjects;

namespace StackDuel.Domain.TestSuites;

public interface ITestSuiteWriteRepository : IRepository<TestSuite>
{
    Task<IReadOnlyList<Guid>> FindTestCaseIdsByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<Guid>> FindPublicTestCaseIdsByProblemSetupIdAsync(
        Guid problemSetupId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Grading's test case selection: every public (sample) case and every hand-authored
    /// hidden case always runs — those are guaranteed, not part of the random draw — topped
    /// up with a random sample of generated hidden cases until <paramref name="targetCount"/>
    /// is reached (or until the generated pool runs out). If the guaranteed set alone already
    /// meets or exceeds <paramref name="targetCount"/>, no random cases are added on top.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindGradingTestCaseIdsByProblemSetupIdAsync(
        Guid problemSetupId,
        int targetCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Value types aren't supplied by the client — each input is tagged with the value type
    /// of the existing test case input at the same position, so strictly-typed languages
    /// (C++, Java) can still generate valid parse calls for ad hoc/custom inputs.
    /// </summary>
    Task<IReadOnlyList<Guid>> CreateAdHocTestCasesAsync(
        Guid problemSetupId,
        IReadOnlyCollection<IReadOnlyCollection<string>> customTestCaseInputs,
        CancellationToken cancellationToken = default
    );
    Task<Dictionary<Guid, string>> FindExpectedOutputsByTestCaseIdsAsync(
        IEnumerable<Guid> testCaseIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Resolves display-ready input values per test case id — materialized values for
    /// authored cases, regenerated on the fly (same as grading does) for generated ones,
    /// since their inputs aren't persisted.
    /// </summary>
    Task<Dictionary<Guid, string>> FindInputsByTestCaseIdsAsync(
        IEnumerable<Guid> testCaseIds,
        CancellationToken cancellationToken = default
    );
    Task<Guid?> FindPipelineIdByProblemSetupIdAsync(Guid problemSetupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the Hidden test suite used to hold a problem's generated test cases, creating it
    /// if it doesn't exist yet, and ensures it is linked to every given problem setup.
    /// </summary>
    Task<Guid> FindOrCreateGeneratedSuiteAsync(
        string suiteName,
        IReadOnlyCollection<Guid> problemSetupIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retires all currently-active Generated-source test cases in a suite, in preparation for
    /// repopulating it with a fresh generated batch. Retiring rather than deleting means a
    /// submission that already graded against one of these cases keeps a valid reference —
    /// submission_results.test_case_id is ON DELETE RESTRICT, so a case that's ever been
    /// graded against can never actually be removed.
    /// </summary>
    Task RetireGeneratedTestCasesAsync(Guid testSuiteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds or creates the Sample test suite for a problem and replaces its admin-authored
    /// cases with exactly the given set, ensuring it stays linked to every given problem setup —
    /// sample case values are language-agnostic (only the reference solution differs per
    /// language), so one suite is shared across all of a problem's setups, same as
    /// <see cref="FindOrCreateGeneratedSuiteAsync"/> does for generated cases. Retires (rather
    /// than deletes) any cases no longer present, for the same reason
    /// <see cref="RetireGeneratedTestCasesAsync"/> retires instead of deleting — a sample case is
    /// graded against per <see cref="FindGradingTestCaseIdsByProblemSetupIdAsync"/>, so a past
    /// submission may already reference one.
    /// </summary>
    Task ReplaceSampleTestCasesAsync(
        IReadOnlyCollection<Guid> problemSetupIds,
        string suiteName,
        IReadOnlyList<AuthoredTestCaseSpec> testCases,
        CancellationToken cancellationToken = default
    );
}