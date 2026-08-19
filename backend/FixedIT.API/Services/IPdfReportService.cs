using FixedIT.API.DTOs.Reports;

namespace FixedIT.API.Services;

public interface IPdfReportService
{
    byte[] GenerateFinancialReport(FinancialReportDocumentData report);
}
