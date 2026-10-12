using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Games.Dtos;

public sealed record GameProblemSubmissionStateDto(Guid SubmissionId, SubmissionStatus Status, DateTime CreatedAt);

/// <summary>
/// A participant's current problem, plus — for the requesting participant only — the live state of
/// their latest submission on it, which is how the client knows a solve is still being graded.
/// </summary>
public sealed record GameCurrentProblemDto(Guid ProblemId, GameProblemSubmissionStateDto? LatestSubmission = null);

public sealed record GameParticipantScoreDto(Guid UserId, int Score, GameCurrentProblemDto? CurrentProblem);

public sealed record SolvedProblemSubmissionDto(Guid ProblemId, Guid SubmissionId);

public sealed record GameProblemHistoryDto(
    Guid UserId,
    IReadOnlyList<Guid> SolvedProblemIds,
    IReadOnlyList<SolvedProblemSubmissionDto> SolvedProblemSubmissions
);