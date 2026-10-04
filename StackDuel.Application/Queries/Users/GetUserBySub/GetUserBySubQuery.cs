using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Queries.Users.GetUserBySub;

public sealed record GetUserBySubQuery(string Sub) : IQuery<UserDto>;