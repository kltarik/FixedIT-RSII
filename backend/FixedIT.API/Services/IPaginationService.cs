using FixedIT.API.DTOs.Common;

namespace FixedIT.API.Services;

public interface IPaginationService
{
    PageParameters Normalize(PagedRequest request);
}
