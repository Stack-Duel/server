using StackDuel.Application.Queries;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Queries.Users.GetUserAvatarHistory;

public sealed record GetUserAvatarHistoryQuery(Guid UserId) : IQuery<IReadOnlyList<UserAvatarDto>>;