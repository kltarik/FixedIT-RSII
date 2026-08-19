using FixedIT.API.Configuration;
using FixedIT.API.DTOs.Common;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

public sealed class PaginationService(IOptions<PaginationOptions> options) : IPaginationService
{
    private readonly PaginationOptions _options = options.Value;

    public PageParameters Normalize(PagedRequest request)
    {
        var requestedPageSize = request.PageSize ?? _options.DefaultPageSize;
        return new PageParameters(
            request.Page,
            Math.Min(requestedPageSize, _options.MaxPageSize));
    }
}
