using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Games.Dtos;

public sealed record GameLatestSubmissionDto(Guid SubmissionId, SubmissionStatus Status, DateTime CreatedAt);

/// <summary>
/// <paramref name="LatestSubmission"/> is the participant's most recent submission against this
/// problem, and is only ever populated for the requesting user's own participant — it lets a
/// client that lost its in-memory submission id (a hard refresh mid-grading) pick the grading
/// state back up, and is nobody else's business.
/// </summary>
public sealed record GameCurrentProblemDto(Guid ProblemId, GameLatestSubmissionDto? LatestSubmission = null);

public sealed record GameParticipantScoreDto(Guid UserId, int Score, GameCurrentProblemDto? CurrentProblem);

public sealed record SolvedProblemSubmissionDto(Guid ProblemId, Guid SubmissionId);

public sealed record GameProblemHistoryDto(
    Guid UserId,
    IReadOnlyList<Guid> SolvedProblemIds,
    IReadOnlyList<SolvedProblemSubmissionDto> SolvedProblemSubmissions
);