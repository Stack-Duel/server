using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Users;

public interface IUserWriteRepository : IRepository<User>
{
    Task<User?> FindBySubAsync(string sub, CancellationToken cancellationToken = default);
    Task<User?> FindByUsername(Username username, CancellationToken cancellationToken = default);
    Task AddToGroupAsync(Guid userId, string groupName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetGroupIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetGroupsAsync(Guid userId, IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default);
    Task SetLanguagePreferencesAsync(
        Guid userId,
        IReadOnlyList<Guid> languageIdsInOrder,
        CancellationToken cancellationToken = default
    );
}