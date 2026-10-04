using StackDuel.Application.DailyChallenges.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.DailyChallenges.GetTodaysDailyChallenge;

public sealed record GetTodaysDailyChallengeQuery(Guid UserId) : IQuery<TodaysDailyChallengeDto>;