using Ardalis.Result;
using MediatR;
using StackDuel.Application.Groups.Dtos;
using StackDuel.Application.Queries.Groups.GetGroups;

namespace StackDuel.Application.Services.Groups;

public interface IGroupService
{
    Task<Result<IReadOnlyList<GroupDto>>> GetAllAsync(CancellationToken cancellationToken);
}

internal sealed class GroupService(IMediator mediator) : IGroupService
{
    public async Task<Result<IReadOnlyList<GroupDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetGroupsQuery(), cancellationToken);
        return result;
    }
}