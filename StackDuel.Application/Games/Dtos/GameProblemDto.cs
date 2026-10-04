namespace StackDuel.Application.Games.Dtos;

public sealed record GameCurrentProblemDto(Guid ProblemId);

public sealed record GameParticipantScoreDto(Guid UserId, int Score, GameCurrentProblemDto? CurrentProblem);

public sealed record SolvedProblemSubmissionDto(Guid ProblemId, Guid SubmissionId);

public sealed record GameProblemHistoryDto(
    Guid UserId,
    IReadOnlyList<Guid> SolvedProblemIds,
    IReadOnlyList<SolvedProblemSubmissionDto> SolvedProblemSubmissions
);