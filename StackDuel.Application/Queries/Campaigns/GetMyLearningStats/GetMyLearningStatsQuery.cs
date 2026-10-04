using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Campaigns.GetMyLearningStats;

public sealed record GetMyLearningStatsQuery(Guid UserId) : IQuery<UserLearningStatsDto>;