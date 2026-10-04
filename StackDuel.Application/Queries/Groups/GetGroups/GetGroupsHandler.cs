using Ardalis.Result;
using StackDuel.Application.Groups;
using StackDuel.Application.Groups.Dtos;

namespace StackDuel.Application.Queries.Groups.GetGroups;

internal sealed class GetGroupsHandler(IGroupReadRepository groupReadRepository)
    : IQueryHandler<GetGroupsQuery, IReadOnlyList<GroupDto>>
{
    public async Task<Result<IReadOnlyList<GroupDto>>> Handle(
        GetGroupsQuery request,
        CancellationToken cancellationToken
    )
    {
        var groups = await groupReadRepository.GetAllAsync(cancellationToken);

        return Result.Success(groups);
    }
}