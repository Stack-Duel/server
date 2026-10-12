using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Queries.Games.GetGameProblems;

internal sealed class GetGameProblemsHandler(
    IGameReadRepository gameReadRepository,
    ISubmissionReadRepository submissionReadRepository
) : IQueryHandler<GetGameProblemsQuery, IReadOnlyList<GameProblemHistoryDto>>
{
    public async Task<Result<IReadOnlyList<GameProblemHistoryDto>>> Handle(
        GetGameProblemsQuery request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result<IReadOnlyList<GameProblemHistoryDto>>.NotFound();

        bool isParticipant = game.Participants.Any(p => p.UserId == request.RequestedByUserId);
        if (!isParticipant)
            return Result<IReadOnlyList<GameProblemHistoryDto>>.Forbidden();

        IReadOnlyList<GameProblemSubmissionDto> latestSubmissions =
            await submissionReadRepository.GetLatestGameProblemSubmissionsAsync(request.GameId, cancellationToken);

        Dictionary<Guid, List<SolvedProblemSubmissionDto>> solvedSubmissionsByUser = latestSubmissions
            .Where(s => s.Status == SubmissionStatus.Accepted)
            .GroupBy(s => s.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(s => new SolvedProblemSubmissionDto(s.ProblemId, s.SubmissionId)).ToList()
            );

        var problemHistories = game
            .Participants.Select(p => new GameProblemHistoryDto(
                p.UserId,
                p.ProblemSession?.SolvedProblemIds.ToList() ?? [],
                solvedSubmissionsByUser.GetValueOrDefault(p.UserId) ?? []
            ))
            .ToList();

        return Result<IReadOnlyList<GameProblemHistoryDto>>.Success(problemHistories);
    }
}