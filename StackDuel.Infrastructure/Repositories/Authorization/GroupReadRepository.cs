using Microsoft.EntityFrameworkCore;
using StackDuel.Application.Groups;
using StackDuel.Application.Groups.Dtos;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Authorization;

internal sealed class GroupReadRepository(StackDuelDbContext context) : IGroupReadRepository
{
    public async Task<IReadOnlyList<GroupDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context
            .Groups.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GroupDto(g.Id.Value, g.Name.Value))
            .ToListAsync(cancellationToken);
    }
}