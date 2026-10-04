namespace StackDuel.Application.Pagination;

public sealed class PageResult<T>
{
    public required IReadOnlyList<T> Results { get; init; }

    public required int Total { get; init; }

    public required int Page { get; init; }

    public required int Size { get; init; }

    public int TotalPages => Size > 0 && Total > 0 ? (int)Math.Ceiling((double)Total / Size) : 0;

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public int Offset => (Page - 1) * Size;
}