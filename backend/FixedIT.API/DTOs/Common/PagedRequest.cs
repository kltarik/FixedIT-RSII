using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Common;

public sealed record PagedRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, PaginationConstants.MaxPageSize)]
    public int? PageSize { get; init; }
}

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record PageParameters(int Page, int PageSize)
{
    public int Skip => (int)Math.Min((long)(Page - 1) * PageSize, int.MaxValue);
}
