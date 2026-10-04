namespace StackDuel.Api.Requests.User;

public sealed record UpdateUserGroupsRequest(IReadOnlyList<Guid> GroupIds);