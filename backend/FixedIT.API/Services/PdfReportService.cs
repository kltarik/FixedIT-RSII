using System.Globalization;
using FixedIT.API.Configuration;
using FixedIT.API.DTOs.Reports;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FixedIT.API.Services;

public sealed class PdfReportService(IOptions<ReportOptions> options) : IPdfReportService
{
    private readonly ReportOptions _options = options.Value;

    public byte[] GenerateFinancialReport(FinancialReportDocumentData report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(style => style.FontSize(9));
                page.Header().Column(column =>
                {
                    column.Item().Text(_options.CompanyName)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);
                    column.Item().Text("Finansijski izvještaj").FontSize(14).SemiBold();
                    column.Item().Text(BuildPeriodText(report));
                });
                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(
                            $"Ukupan prihod: {FormatMoney(report.TotalRevenue, report.Currency)}")
                            .Bold();
                        row.RelativeItem().Text($"Završena plaćanja: {report.PaymentCount}");
                        row.RelativeItem().AlignRight().Text(
                            $"Generisano: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");
                    });

                    if (report.IsTruncated)
                    {
                        column.Item()
                            .Background(Colors.Orange.Lighten4)
                            .Padding(8)
                            .Text($"Tabela rezervacija ograničena je na {_options.MaxPdfRows} redova.");
                    }

                    column.Item().Element(tableContainer => ComposeTable(
                        tableContainer,
                        report.Reservations));
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Stranica ");
                    text.CurrentPageNumber();
                    text.Span(" od ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeTable(
        IContainer container,
        IReadOnlyCollection<FinancialReservationResponse> reservations)
    {
        if (reservations.Count == 0)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(15)
                .AlignCenter()
                .Text("Nema završenih plaćanja za odabrane filtere.");
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(55);
                columns.ConstantColumn(95);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(80);
            });
            table.Header(header =>
            {
                HeaderCell(header.Cell(), "Rezervacija");
                HeaderCell(header.Cell(), "Završeno UTC");
                HeaderCell(header.Cell(), "Klijent");
                HeaderCell(header.Cell(), "Profesionalac");
                HeaderCell(header.Cell(), "Kategorije");
                HeaderCell(header.Cell(), "Iznos");
            });

            foreach (var reservation in reservations)
            {
                BodyCell(table.Cell(), reservation.ReservationId.ToString(CultureInfo.InvariantCulture));
                BodyCell(table.Cell(), reservation.CompletedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                BodyCell(table.Cell(), reservation.ClientName);
                BodyCell(table.Cell(), reservation.ProfessionalName);
                BodyCell(table.Cell(), string.Join(", ", reservation.Categories.Select(item => item.Name)));
                BodyCell(table.Cell(), FormatMoney(reservation.Amount, reservation.Currency), alignRight: true);
            }
        });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container
            .Background(Colors.Blue.Darken2)
            .Padding(6)
            .Text(text)
            .FontColor(Colors.White)
            .SemiBold();
    }

    private static void BodyCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(6);
        if (alignRight)
        {
            cell = cell.AlignRight();
        }

        cell.Text(text);
    }

    private static string BuildPeriodText(FinancialReportDocumentData report)
    {
        var from = report.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "početak";
        var to = report.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "danas";
        var category = report.CategoryId.HasValue
            ? $" | ID kategorije: {report.CategoryId.Value}"
            : string.Empty;
        return $"Period: {from} do {to}{category}";
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        return $"{amount.ToString("N2", CultureInfo.InvariantCulture)} {currency}";
    }
}
