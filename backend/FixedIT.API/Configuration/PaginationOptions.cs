using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.Configuration;

public sealed class PaginationOptions
{
    public const string SectionName = "Pagination";

    [Range(1, PaginationConstants.MaxPageSize)]
    public int MaxPageSize { get; set; } = PaginationConstants.MaxPageSize;

    [Range(1, PaginationConstants.MaxPageSize)]
    public int DefaultPageSize { get; set; } = PaginationConstants.DefaultPageSize;
}
