namespace StackDuel.Application.ProblemValidation;

public interface IProblemValidationConcurrencyGuard
{
    Task<T> RunExclusiveAsync<T>(
        Guid problemId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    );
}