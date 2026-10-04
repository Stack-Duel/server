using Ardalis.Result;
using Mediator;
using StackDuel.Application.Commands.Users.CreateUser;
using StackDuel.Application.Queries.Users.GetUserBySub;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Users;

internal sealed class UserService(ISender sender) : IUserService
{
    public async Task<Result<Guid>> CreateAsync(
        string username,
        string sub,
        string? imageUrl,
        string? tenant,
        CancellationToken cancellationToken
    ) => await sender.Send(new CreateUserCommand(username, sub, imageUrl, tenant), cancellationToken);

    public async Task<Result<UserDto>> GetBySubAsync(string sub, CancellationToken cancellationToken) =>
        await sender.Send(new GetUserBySubQuery(sub), cancellationToken);
}