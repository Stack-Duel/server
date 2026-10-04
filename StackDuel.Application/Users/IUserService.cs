using Ardalis.Result;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Users;

public interface IUserService
{
    Task<Result<Guid>> CreateAsync(string username, string sub, string? imageUrl, string? tenant, CancellationToken cancellationToken);

    Task<Result<UserDto>> GetBySubAsync(string sub, CancellationToken cancellationToken);
}