using StackDuel.Application.Queries;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Queries.Users.GetUserProfileByUsername;

public sealed record GetUserProfileByUsernameQuery(string Username, Guid? RequestingUserId) : IQuery<UserProfileDto>;