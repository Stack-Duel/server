using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Submissions.Exceptions;

public sealed class SubmissionNotCompleteException : DomainException
{
    public SubmissionNotCompleteException()
        : base("Submission cannot be completed because one or more results are still pending or processing.") { }
}