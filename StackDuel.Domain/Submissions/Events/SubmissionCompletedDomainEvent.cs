using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Domain.Submissions.Events;

public sealed record SubmissionCompletedDomainEvent(
    Guid UserId,
    Guid SubmissionId,
    SubmissionStatus Status,
    Guid? GameId
) : IDomainEvent;