namespace StackDuel.Api.Requests.Audit;

public sealed record GetAuditLogPageableRequest(int Page, int Size, DateTime Timestamp);