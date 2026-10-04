using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Submissions.Events;

public sealed record SubmissionCreatedDomainEvent(Guid SubmissionId) : IDomainEvent;