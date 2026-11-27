namespace Kasio.Contracts.Common;

public sealed record PagedResponse<T>(int Page, int PageSize, int TotalCount, IReadOnlyList<T> Items)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
