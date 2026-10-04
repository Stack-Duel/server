using StackDuel.Domain.User;
using StackDuel.Domain.User.Entities;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Specs.Support;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        _users.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

    public Task<User?> FindBySubAsync(UserSub sub, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.SingleOrDefault(u => u.Sub == sub));

    public Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.SingleOrDefault(u => u.Username == username));

    public void Seed(User user) => _users.Add(user);
}