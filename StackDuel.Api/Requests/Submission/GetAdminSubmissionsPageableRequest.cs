namespace StackDuel.Api.Requests.Submission;

public sealed record GetAdminSubmissionsPageableRequest(int Page, int Size, DateTime Timestamp, Guid? Id);