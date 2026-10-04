namespace StackDuel.Api.Requests.Problem;

public sealed record GetProblemPoolMembersPagedRequest(int Page, int Size, DateTime Timestamp);