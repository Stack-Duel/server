using StackDuel.Infrastructure.ProblemValidation;

namespace StackDuel.Infrastructure.Tests.Jobs.ProblemValidation;

internal sealed class StubReferenceSolutionValidationService : IReferenceSolutionValidationService
{
    private ReferenceSolutionValidationResult _result = new(true, []);
    private Exception? _failure;

    public int CallCount { get; private set; }

    public void Returns(bool allPassed, params SetupValidationOutcome[] outcomes)
    {
        _failure = null;
        _result = new ReferenceSolutionValidationResult(allPassed, outcomes);
    }

    public void Throws(Exception failure) => _failure = failure;

    public Task<ReferenceSolutionValidationResult> ValidateAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    )
    {
        CallCount++;

        return _failure is null
            ? Task.FromResult(_result)
            : Task.FromException<ReferenceSolutionValidationResult>(_failure);
    }
}