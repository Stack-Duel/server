namespace StackDuel.Application.Pagination;

public class PaginationRequest
{
    public required int Page { get; init; }

    public required int Size { get; init; }

    public DateTime Timestamp { get; init; }
}