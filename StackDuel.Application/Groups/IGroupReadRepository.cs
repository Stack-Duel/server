using StackDuel.Application.Groups.Dtos;

namespace StackDuel.Application.Groups;

public interface IGroupReadRepository
{
    Task<IReadOnlyList<GroupDto>> GetAllAsync(CancellationToken cancellationToken = default);
}