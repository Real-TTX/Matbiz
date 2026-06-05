using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.SystemSettings.Models;
using Matbiz.Web.Modules.SystemSettings.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = Matbiz.Web.Modules.Documents.Models.Document;
using QPdf = QuestPDF.Fluent.Document;
using QPdfMeta = QuestPDF.Infrastructure.DocumentMetadata;

namespace Matbiz.Web.Modules.Documents.Services;

/// <summary>
/// Rendert einen <see cref="Document"/> zu PDF — DIN-A4-Briefformat mit Logo,
/// Firmen-Block, Adress-Block (rechts oben für Briefumschlag-Sichtfenster),
/// Positionen, Summen, Fußzeile mit Firma/Bank/USt-ID.
///
/// Verwendet <see cref="BrandingSettings"/> für Logo + Firmen-Stammdaten.
/// </summary>
public class DocumentPdfRenderer(BrandingService branding, XRechnungGenerator xrechnung)
{
    public async Task<byte[]> RenderAsync(Document doc, CancellationToken ct = default)
        => await RenderInternalAsync(doc, embedXml: false, ct);

    /// <summary>
    /// Erzeugt ein ZUGFeRD-Hybrid-PDF: PDF/A-3 mit eingebetteter XRechnung-XML.
    /// Empfänger sieht ein normales PDF, kann aber maschinell die XML extrahieren.
    /// </summary>
    public async Task<byte[]> RenderHybridAsync(Document doc, CancellationToken ct = default)
        => await RenderInternalAsync(doc, embedXml: true, ct);

    private async Task<byte[]> RenderInternalAsync(Document doc, bool embedXml, CancellationToken ct)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var brand = await branding.GetAsync(ct);
        var template = brand.PdfTemplate ?? "Classic";

        var quest = QPdf.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                switch (template)
                {
                    case "Modern":
                        page.Header().Element(c => HeaderModern(c, doc, brand));
                        page.Content().Element(c => BodyModern(c, doc, brand));
                        page.Footer().Element(c => Footer(c, brand));
                        break;
                    case "Minimal":
                        page.Header().Element(c => HeaderMinimal(c, doc, brand));
                        page.Content().Element(c => Body(c, doc));
                        page.Footer().Element(c => Footer(c, brand));
                        break;
                    default: // Classic
                        page.Header().Element(c => Header(c, doc, brand));
                        page.Content().Element(c => Body(c, doc));
                        page.Footer().Element(c => Footer(c, brand));
                        break;
                }
            });
        });

        if (embedXml)
        {
            quest = quest.WithMetadata(new QPdfMeta
            {
                Title    = $"{DocumentService.TypeLabel(doc.Type)} {doc.Number}",
                Author   = brand.CompanyLegalName ?? brand.AppName,
                Subject  = "ZUGFeRD-Hybrid-PDF mit eingebetteter XRechnung",
                Producer = "Matbiz"
            });

            var pdfBytes = quest.GeneratePdf();
            var xmlBytes = await xrechnung.GenerateAsync(doc, ct);
            return PdfXmlEmbedder.Embed(pdfBytes, xmlBytes, "factur-x.xml");
        }

        return quest.GeneratePdf();
    }

    // === Header-Varianten ===

    private static void HeaderModern(IContainer c, Document doc, BrandingSettings b)
    {
        // Voller Farbbalken oben mit Logo + Firmenname
        var accent = ParseColor(b.PrimaryColor, Colors.Blue.Darken3);
        c.Background(accent).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                if (b.LogoBytes is { Length: > 0 })
                    col.Item().MaxHeight(20, Unit.Millimetre).Image(b.LogoBytes).FitArea();
                else
                    col.Item().Text(b.CompanyLegalName ?? b.AppName).FontColor(Colors.White).FontSize(16).Bold();
            });
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(t => t.FontColor(Colors.White).FontSize(8));
                if (!string.IsNullOrWhiteSpace(b.CompanyStreet)) { text.Line(b.CompanyStreet!); }
                var plzOrt = $"{b.CompanyPostalCode} {b.CompanyCity}".Trim();
                if (plzOrt.Length > 0) { text.Line(plzOrt); }
                if (!string.IsNullOrWhiteSpace(b.CompanyEmail)) { text.Line(b.CompanyEmail!); }
                if (!string.IsNullOrWhiteSpace(b.CompanyPhone)) { text.Line("Tel: " + b.CompanyPhone); }
            });
        });
    }

    private static void HeaderMinimal(IContainer c, Document doc, BrandingSettings b)
    {
        // Nur eine schmale Linie mit Firmenname rechts — kein Logo
        c.PaddingBottom(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
         .AlignRight()
         .Text(b.CompanyLegalName ?? b.AppName).FontSize(11).Bold();
    }

    private static void BodyModern(IContainer c, Document doc, BrandingSettings b)
    {
        var accent = ParseColor(b.PrimaryColor, Colors.Blue.Darken3);
        c.PaddingTop(10).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(addr =>
                {
                    addr.Spacing(2);
                    if (!string.IsNullOrWhiteSpace(doc.CustomerAddressSnapshot))
                        foreach (var line in doc.CustomerAddressSnapshot!.Split('\n'))
                            addr.Item().Text(line.TrimEnd());
                });
                row.ConstantItem(60, Unit.Millimetre).Column(meta =>
                {
                    meta.Spacing(2);
                    AddMetaRow(meta, "Beleg-Nr.", doc.Number);
                    AddMetaRow(meta, "Datum", doc.DocumentDate.ToLocalTime().ToString("dd.MM.yyyy"));
                    if (doc.DueDate is DateTime due)
                    {
                        var label = doc.Type == DocumentType.Invoice ? "Zahlbar bis" : "Gültig bis";
                        AddMetaRow(meta, label, due.ToLocalTime().ToString("dd.MM.yyyy"));
                    }
                });
            });

            // Akzent-Titel mit farbiger Linie darunter
            col.Item().PaddingTop(20).Text(DocumentService.TypeLabel(doc.Type) + " " + doc.Number)
                .FontSize(20).Bold().FontColor(accent);
            col.Item().BorderBottom(2).BorderColor(accent).PaddingTop(2);

            if (!string.IsNullOrWhiteSpace(doc.HeaderText))
                col.Item().PaddingTop(8).Text(doc.HeaderText!);
            col.Item().PaddingTop(15).Element(e => PositionsTable(e, doc));
            col.Item().PaddingTop(10).Element(e => TotalsBlock(e, doc));
            if (!string.IsNullOrWhiteSpace(doc.FooterText))
                col.Item().PaddingTop(15).Text(doc.FooterText!);
            if (!string.IsNullOrWhiteSpace(doc.PaymentTerms))
                col.Item().PaddingTop(10).Text(doc.PaymentTerms!).Italic();
        });
    }

    private static string ParseColor(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        if (hex.StartsWith("#")) hex = hex[1..];
        return hex.Length == 6 ? "#" + hex.ToUpperInvariant() : fallback;
    }

    private static void Header(IContainer c, Document doc, BrandingSettings b)
    {
        c.Row(row =>
        {
            // Links: Logo
            row.ConstantItem(60, Unit.Millimetre).Column(col =>
            {
                if (b.LogoBytes is { Length: > 0 })
                {
                    col.Item().MaxHeight(25, Unit.Millimetre).Image(b.LogoBytes).FitArea();
                }
                else
                {
                    col.Item().Text(b.AppName).FontSize(16).Bold();
                }
            });

            // Rechts: Firma-Block (kompakt, oben rechts)
            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text(b.CompanyLegalName ?? b.AppName).Bold();
                if (!string.IsNullOrWhiteSpace(b.CompanyStreet)) col.Item().Text(b.CompanyStreet!).FontSize(8);
                var plzOrt = $"{b.CompanyPostalCode} {b.CompanyCity}".Trim();
                if (plzOrt.Length > 0) col.Item().Text(plzOrt).FontSize(8);
                if (!string.IsNullOrWhiteSpace(b.CompanyEmail)) col.Item().Text(b.CompanyEmail!).FontSize(8);
                if (!string.IsNullOrWhiteSpace(b.CompanyPhone)) col.Item().Text("Tel: " + b.CompanyPhone).FontSize(8);
            });
        });
    }

    private static void Body(IContainer c, Document doc)
    {
        c.PaddingTop(10).Column(col =>
        {
            // Adress-Block links
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(addr =>
                {
                    addr.Spacing(2);
                    if (!string.IsNullOrWhiteSpace(doc.CustomerAddressSnapshot))
                    {
                        foreach (var line in doc.CustomerAddressSnapshot!.Split('\n'))
                            addr.Item().Text(line.TrimEnd());
                    }
                    else if (!string.IsNullOrWhiteSpace(doc.CustomerNameSnapshot))
                    {
                        addr.Item().Text(doc.CustomerNameSnapshot!);
                    }
                });

                // Belegdaten rechts
                row.ConstantItem(60, Unit.Millimetre).Column(meta =>
                {
                    meta.Spacing(2);
                    AddMetaRow(meta, "Beleg-Nr.", doc.Number);
                    AddMetaRow(meta, "Datum", doc.DocumentDate.ToLocalTime().ToString("dd.MM.yyyy"));
                    if (doc.DueDate is DateTime due)
                    {
                        var label = doc.Type == DocumentType.Invoice ? "Zahlbar bis" : "Gültig bis";
                        AddMetaRow(meta, label, due.ToLocalTime().ToString("dd.MM.yyyy"));
                    }
                });
            });

            // Titel
            col.Item().PaddingTop(20).Text(DocumentService.TypeLabel(doc.Type) + " " + doc.Number)
                .FontSize(18).Bold();

            // Kopftext
            if (!string.IsNullOrWhiteSpace(doc.HeaderText))
                col.Item().PaddingTop(8).Text(doc.HeaderText!);

            // Positionen
            col.Item().PaddingTop(15).Element(e => PositionsTable(e, doc));

            // Summen
            col.Item().PaddingTop(10).Element(e => TotalsBlock(e, doc));

            // Fußtext
            if (!string.IsNullOrWhiteSpace(doc.FooterText))
                col.Item().PaddingTop(15).Text(doc.FooterText!);

            // Zahlungsbedingung
            if (!string.IsNullOrWhiteSpace(doc.PaymentTerms))
                col.Item().PaddingTop(10).Text(doc.PaymentTerms!).Italic();
        });
    }

    private static void AddMetaRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem(2).Text(label).FontColor(Colors.Grey.Darken1);
            r.RelativeItem(3).Text(value).Bold();
        });
    }

    private static void PositionsTable(IContainer c, Document doc)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(20);    // Pos
                cols.ConstantColumn(35);    // Bild
                cols.RelativeColumn(5);     // Beschreibung
                cols.ConstantColumn(50);    // Menge x Einheit
                cols.ConstantColumn(60);    // Einzelpreis
                cols.ConstantColumn(40);    // MwSt %
                cols.ConstantColumn(70);    // Summe
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("#");
                header.Cell().Element(HeaderCell).Text("");
                header.Cell().Element(HeaderCell).Text("Beschreibung");
                header.Cell().Element(HeaderCell).AlignRight().Text("Menge");
                header.Cell().Element(HeaderCell).AlignRight().Text("Einzel €");
                header.Cell().Element(HeaderCell).AlignRight().Text("MwSt");
                header.Cell().Element(HeaderCell).AlignRight().Text("Summe €");
            });

            foreach (var p in doc.Positions.OrderBy(x => x.Position))
            {
                table.Cell().Element(BodyCell).Text(p.Position.ToString());
                table.Cell().Element(BodyCell).Padding(2).Element(img =>
                {
                    if (p.Article?.ImageBytes is { Length: > 0 })
                        img.MaxHeight(28).MaxWidth(28).Image(p.Article.ImageBytes).FitArea();
                    else
                        img.Text("");
                });
                table.Cell().Element(BodyCell).Column(col =>
                {
                    col.Item().Text(p.Description);
                    if (!string.IsNullOrEmpty(p.ArticleNumber))
                        col.Item().Text(p.ArticleNumber!).FontSize(7).FontColor(Colors.Grey.Medium);
                });
                table.Cell().Element(BodyCell).AlignRight().Text($"{p.Quantity:N2} {p.Unit}");
                table.Cell().Element(BodyCell).AlignRight().Text($"{p.NetPrice:N2}");
                table.Cell().Element(BodyCell).AlignRight().Text($"{p.TaxRatePercent:0.#} %");
                table.Cell().Element(BodyCell).AlignRight().Text($"{p.NetTotal:N2}");
            }
        });

        static IContainer HeaderCell(IContainer c) =>
            c.DefaultTextStyle(t => t.Bold().FontSize(9))
             .Background(Colors.Grey.Lighten3)
             .PaddingVertical(4).PaddingHorizontal(4);

        static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
             .PaddingVertical(4).PaddingHorizontal(4);
    }

    private static void TotalsBlock(IContainer c, Document doc)
    {
        c.AlignRight().Width(90, Unit.Millimetre).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Netto-Summe").FontColor(Colors.Grey.Darken1);
                r.ConstantItem(30, Unit.Millimetre).AlignRight().Text($"{doc.NetTotal:N2} €");
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("zzgl. USt").FontColor(Colors.Grey.Darken1);
                r.ConstantItem(30, Unit.Millimetre).AlignRight().Text($"{doc.TaxTotal:N2} €");
            });
            col.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Black).PaddingTop(5).Row(r =>
            {
                r.RelativeItem().Text("Brutto-Summe").Bold();
                r.ConstantItem(30, Unit.Millimetre).AlignRight().Text($"{doc.GrossTotal:N2} €").Bold();
            });
        });
    }

    private static void Footer(IContainer c, BrandingSettings b)
    {
        c.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(5)
         .DefaultTextStyle(t => t.FontSize(7).FontColor(Colors.Grey.Darken1))
         .Row(row =>
         {
             row.RelativeItem().Column(col =>
             {
                 col.Item().Text(b.CompanyLegalName ?? b.AppName).Bold();
                 if (!string.IsNullOrWhiteSpace(b.ManagingDirector)) col.Item().Text("GF: " + b.ManagingDirector);
                 if (!string.IsNullOrWhiteSpace(b.VatId))            col.Item().Text("USt-ID: " + b.VatId);
                 if (!string.IsNullOrWhiteSpace(b.TaxNumber))        col.Item().Text("Steuernr.: " + b.TaxNumber);
             });
             row.RelativeItem().Column(col =>
             {
                 if (!string.IsNullOrWhiteSpace(b.BankName)) col.Item().Text(b.BankName!).Bold();
                 if (!string.IsNullOrWhiteSpace(b.Iban))     col.Item().Text("IBAN: " + b.Iban);
                 if (!string.IsNullOrWhiteSpace(b.Bic))      col.Item().Text("BIC: " + b.Bic);
             });
             row.RelativeItem().AlignRight().Column(col =>
             {
                 if (!string.IsNullOrWhiteSpace(b.CompanyWebsite)) col.Item().AlignRight().Text(b.CompanyWebsite!);
                 if (!string.IsNullOrWhiteSpace(b.CompanyEmail))   col.Item().AlignRight().Text(b.CompanyEmail!);
                 if (!string.IsNullOrWhiteSpace(b.CompanyPhone))   col.Item().AlignRight().Text(b.CompanyPhone!);
                 col.Item().PaddingTop(3).AlignRight().Text(text =>
                 {
                     text.DefaultTextStyle(t => t.FontSize(7));
                     text.Span("Seite ");
                     text.CurrentPageNumber();
                     text.Span(" / ");
                     text.TotalPages();
                 });
             });
         });

        if (!string.IsNullOrWhiteSpace(b.PdfFooterText))
        {
            // Zusätzliche Fußzeilen-Notiz unterhalb der Drei-Spalter
            // (wird automatisch von QuestPDF gestackt)
        }
    }
}
