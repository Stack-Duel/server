using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Games.GetGameProblems;

internal sealed class GetGameProblemsHandler(IGameReadRepository gameReadRepository)
    : IQueryHandler<GetGameProblemsQuery, IReadOnlyList<GameProblemHistoryDto>>
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

        var problemHistories = game
            .Participants.Select(p => new GameProblemHistoryDto(
                p.UserId,
                p.ProblemSession?.SolvedProblemIds.ToList() ?? new List<Guid>(),
                p.ProblemSession?.SolvedProblemSubmissionIds.Select(kvp => new SolvedProblemSubmissionDto(
                        kvp.Key,
                        kvp.Value
                    ))
                    .ToList()
                    ?? new List<SolvedProblemSubmissionDto>()
            ))
            .ToList();

        return Result<IReadOnlyList<GameProblemHistoryDto>>.Success(problemHistories);
    }
}