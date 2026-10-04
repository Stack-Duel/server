using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Queries.Permissions.GetUserAccessContext;

public sealed record GetUserAccessContextQuery(string Sub) : IQuery<UserAccessContextDto>;