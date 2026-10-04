using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Domain.Submissions.Factories;

public sealed record CreateSubmissionParams(
    Guid UserId,
    Guid ProblemSetupId,
    SubmissionType Type,
    SourceCode SourceCode,
    IEnumerable<Guid> TestCaseIds,
    IEnumerable<SubmissionSourceFile>? AdditionalFiles = null,
    Guid? GameId = null
);

public sealed class SubmissionFactory : IAggregateFactory<Submission, CreateSubmissionParams>
{
    public Submission Create(CreateSubmissionParams parameters)
    {
        return new Submission(
            parameters.UserId,
            parameters.ProblemSetupId,
            parameters.Type,
            parameters.SourceCode,
            parameters.TestCaseIds,
            parameters.AdditionalFiles,
            parameters.GameId
        );
    }
}