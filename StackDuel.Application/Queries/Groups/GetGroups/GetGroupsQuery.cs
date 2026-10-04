using StackDuel.Application.Groups.Dtos;

namespace StackDuel.Application.Queries.Groups.GetGroups;

public sealed record GetGroupsQuery : IQuery<IReadOnlyList<GroupDto>>;