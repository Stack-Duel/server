using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Submissions.Exceptions;

public sealed class SubmissionResultNotFoundException : DomainException
{
    public SubmissionResultNotFoundException(Guid testCaseId)
        : base($"Submission result for test case '{testCaseId}' was not found.") { }
}