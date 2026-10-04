using StackDuel.Domain.SeedWork;
using StackDuel.Domain.User.Entities;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Domain.User;

public interface IUserRepository : IRepository<Entities.User>
{
    Task<Entities.User?> FindBySubAsync(UserSub sub, CancellationToken cancellationToken = default);
    Task<Entities.User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken = default);
}