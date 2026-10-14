using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

/// <summary>
/// The data a solved round carries that no other outcome does: the submission that solved it. Lives
/// in its own table rather than as a nullable column on the completion, so
/// <see cref="SubmissionId"/> can be genuinely required — a solution row cannot exist without one.
/// </summary>
/// <remarks>
/// This is the template for any future outcome with data of its own: a table keyed on the completion
/// plus a fixed <c>outcome</c> value, so the row can only attach to a completion already carrying
/// that outcome. Two such tables therefore can't both attach to the same completion.
/// </remarks>
public sealed class GameRoundSolution : Entity
{
    public GameRoundSolution(Guid submissionId)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("Submission id must not be empty.", nameof(submissionId));

        SubmissionId = submissionId;
    }

    private GameRoundSolution() { }

    public Guid SubmissionId { get; private set; }
}