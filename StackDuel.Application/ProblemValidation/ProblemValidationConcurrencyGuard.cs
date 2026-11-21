using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.ProblemValidation;

internal sealed class ProblemValidationConcurrencyGuard(IAdvisoryLockService advisoryLockService)
    : IProblemValidationConcurrencyGuard
{
    private const string LockNamespace = "problem-validation";

    public Task<T> RunExclusiveAsync<T>(
        Guid problemId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    ) => advisoryLockService.RunExclusiveAsync(LockNamespace, problemId, action, cancellationToken);
}