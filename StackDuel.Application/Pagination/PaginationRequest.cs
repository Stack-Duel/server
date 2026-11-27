namespace StackDuel.Application.Pagination;

public class PaginationRequest
{
    public const int MinPage = 1;

    public const int MinSize = 1;

    public const int MaxSize = 200;

    public required int Page
    {
        get;
        init => field = value < MinPage ? MinPage : value;
    } = MinPage;

    public required int Size
    {
        get;
        init => field = Math.Clamp(value, MinSize, MaxSize);
    } = MinSize;

    public DateTime Timestamp { get; init; }
}