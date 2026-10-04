using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.DailyChallenges.UpdateDailyChallenge;
using StackDuel.Application.DailyChallenges.Dtos;
using StackDuel.Application.Queries.DailyChallenges.GetTodaysDailyChallenge;
using StackDuel.Application.Queries.DailyChallenges.GetUpcomingDailyChallenges;

namespace StackDuel.Application.Services.DailyChallenges;

public interface IDailyChallengeService
{
    Task<Result<TodaysDailyChallengeDto>> GetTodaysChallengeAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<UpcomingDailyChallengeDto>>> GetUpcomingChallengesAsync(
        CancellationToken cancellationToken
    );

    Task<Result> UpdateChallengeAsync(DateOnly date, Guid problemId, CancellationToken cancellationToken);
}

internal sealed class DailyChallengeService(IMediator mediator) : IDailyChallengeService
{
    public async Task<Result<TodaysDailyChallengeDto>> GetTodaysChallengeAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetTodaysDailyChallengeQuery(userId), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<UpcomingDailyChallengeDto>>> GetUpcomingChallengesAsync(
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetUpcomingDailyChallengesQuery(), cancellationToken);
    }

    public async Task<Result> UpdateChallengeAsync(DateOnly date, Guid problemId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new UpdateDailyChallengeCommand(date, problemId), cancellationToken);
    }
}