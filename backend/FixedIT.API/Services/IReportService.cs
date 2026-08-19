using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reports;

namespace FixedIT.API.Services;

public interface IReportService
{
    Task<FinancialReportResponse> GetFinancialReportAsync(
        string userId,
        bool isAdmin,
        ReportFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<FinancialReportDocumentData> GetFinancialReportDocumentAsync(
        string userId,
        bool isAdmin,
        ReportFilterRequest filters,
        CancellationToken cancellationToken);

    Task<PagedResponse<ProfessionalPerformanceResponse>> GetProfessionalPerformanceAsync(
        ReportFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken);
}
