using StackDuel.Application.DailyChallenges.Dtos;

namespace StackDuel.Application.Queries.DailyChallenges.GetUpcomingDailyChallenges;

public sealed record GetUpcomingDailyChallengesQuery : IQuery<IReadOnlyList<UpcomingDailyChallengeDto>>;