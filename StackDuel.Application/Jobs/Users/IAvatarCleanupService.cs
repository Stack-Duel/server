namespace StackDuel.Application.Jobs.Users;

public interface IAvatarCleanupService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}