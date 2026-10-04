using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Users.Factories;

public sealed record CreateUserParams(string Username, string Sub, string? ImageUrl, string? Tenant = null);

public sealed class UserFactory : IAggregateFactory<User, CreateUserParams>
{
    public User Create(CreateUserParams parameters)
    {
        var username = new Username(parameters.Username);
        var imageUrl = parameters.ImageUrl is not null ? new ImageUrl(parameters.ImageUrl) : null;
        var user = new User(username, parameters.Sub, parameters.Tenant);
        user.UpdateImageUrl(imageUrl);
        user.MarkCreated();
        return user;
    }
}