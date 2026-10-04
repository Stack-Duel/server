using StackDuel.Application.Queries;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Queries.Users.GetUserGameStats;

public sealed record GetUserGameStatsQuery(string Username, Guid? RequestingUserId) : IQuery<UserGameStatsDto>;