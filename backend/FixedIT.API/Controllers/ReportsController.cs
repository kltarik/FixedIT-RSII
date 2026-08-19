using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reports;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.Professional)]
[Route("api/reports")]
public sealed class ReportsController(
    IReportService reportService,
    IPdfReportService pdfReportService) : ControllerBase
{
    [HttpGet("financial")]
    public async Task<ActionResult<FinancialReportResponse>> GetFinancial(
        [FromQuery] ReportFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reportService.GetFinancialReportAsync(
            User.GetUserId(),
            User.IsInRole(RoleNames.Admin),
            filters,
            request,
            cancellationToken));
    }

    [HttpGet("financial/pdf")]
    public async Task<IActionResult> DownloadFinancialPdf(
        [FromQuery] ReportFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var report = await reportService.GetFinancialReportDocumentAsync(
            User.GetUserId(),
            User.IsInRole(RoleNames.Admin),
            filters,
            cancellationToken);
        var pdf = pdfReportService.GenerateFinancialReport(report);
        return File(pdf, "application/pdf", "report.pdf");
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpGet("professionals/performance")]
    public async Task<ActionResult<PagedResponse<ProfessionalPerformanceResponse>>>
        GetProfessionalPerformance(
            [FromQuery] ReportFilterRequest filters,
            [FromQuery] PagedRequest request,
            CancellationToken cancellationToken)
    {
        return Ok(await reportService.GetProfessionalPerformanceAsync(
            filters,
            request,
            cancellationToken));
    }
}
