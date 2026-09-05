using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartBillingSystem.Models;

namespace SmartBillingSystem.Services;

public class PdfGenerationService : IPdfGenerationService
{
    public byte[] GenerateBillPdf(Bill bill)
    {
        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));
                page.Header().Element(c => ComposeHeader(c, bill));
                page.Content().Element(c => ComposeContent(c, bill));
                page.Footer().Element(ComposeFooter);
            });
        })
        .GeneratePdf(stream);

        return stream.ToArray();
    }

    private void ComposeHeader(IContainer container, Bill bill)
    {
        container.Column(col =>
        {
            col.Item().Text("SMART BILLING SYSTEM").FontSize(24).Bold();
            col.Item().PaddingTop(4).LineHorizontal(2).LineColor(Colors.Grey.Darken3);
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Bill #{bill.BillId}").FontSize(14).SemiBold();
                    c.Item().Text($"Date: {bill.DateCreated:dd-MMM-yyyy}").FontSize(10).FontColor(Colors.Grey.Darken2);
                });
                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text("TAX INVOICE").FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
                    c.Item().Text($"GST: {bill.TaxRate:P0}").FontSize(10).FontColor(Colors.Grey.Darken2);
                });
            });
        });
    }

    private void ComposeContent(IContainer container, Bill bill)
    {
        container.PaddingVertical(20).Column(col =>
        {
            col.Item().Element(c => ComposeItemsTable(c, bill));
            col.Item().PaddingTop(20).Element(c => ComposeTotals(c, bill));

            if (bill.Recommendations.Any())
                col.Item().PaddingTop(20).Element(c => ComposeRecommendations(c, bill));
        });
    }

    private void ComposeItemsTable(IContainer container, Bill bill)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(4);
                columns.RelativeColumn(1.5f);
                columns.ConstantColumn(60);
                columns.RelativeColumn(1.5f);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("#").FontSize(9).Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("PRODUCT").FontSize(9).Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("PRICE").FontSize(9).Bold().FontColor(Colors.White).AlignRight();
                header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("QTY").FontSize(9).Bold().FontColor(Colors.White).AlignCenter();
                header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("TOTAL").FontSize(9).Bold().FontColor(Colors.White).AlignRight();
            });

            int index = 1;
            foreach (var item in bill.Items)
            {
                var bgColor = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                table.Cell().Background(bgColor).Padding(6).Text((index++).ToString()).FontSize(10);
                table.Cell().Background(bgColor).Padding(6).Text(item.ProductName).FontSize(10);
                table.Cell().Background(bgColor).Padding(6).Text($"₹{item.UnitPrice:N2}").FontSize(10).AlignRight();
                table.Cell().Background(bgColor).Padding(6).Text(item.Quantity.ToString()).FontSize(10).AlignCenter();
                table.Cell().Background(bgColor).Padding(6).Text($"₹{item.LineTotal:N2}").FontSize(10).AlignRight();
            }
        });
    }

    private void ComposeTotals(IContainer container, Bill bill)
    {
        container.AlignRight().Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal:").FontSize(11).AlignRight();
                row.ConstantItem(120).Text($"₹{bill.Subtotal:N2}").FontSize(11).AlignRight();
            });
            col.Item().PaddingVertical(4).Row(row =>
            {
                row.RelativeItem().Text($"GST ({bill.TaxRate:P0}):").FontSize(11).AlignRight();
                row.ConstantItem(120).Text($"₹{bill.TaxAmount:N2}").FontSize(11).AlignRight();
            });
            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Darken3);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("GRAND TOTAL:").FontSize(14).Bold().AlignRight();
                row.ConstantItem(120).Text($"₹{bill.GrandTotal:N2}").FontSize(14).Bold().AlignRight();
            });
        });
    }

    private void ComposeRecommendations(IContainer container, Bill bill)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(8).Text("AI RECOMMENDATIONS").FontSize(12).Bold();
            col.Item().PaddingTop(4).Text("You might also like:").FontSize(10).FontColor(Colors.Grey.Darken2);
            foreach (var rec in bill.Recommendations)
            {
                col.Item().PaddingVertical(2).Text($"• {rec}").FontSize(10);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Generated by Smart Billing System").FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }
}
