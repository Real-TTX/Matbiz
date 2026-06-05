using Matbiz.Web.Modules.Documents.Services;
using Microsoft.AspNetCore.Authorization;

namespace Matbiz.Web.Modules.Documents;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentPdf(this IEndpointRouteBuilder app)
    {
        app.MapGet("/documents/{id:guid}/pdf", [Authorize] async (
            Guid id, bool? download, bool? hybrid,
            DocumentService docs, DocumentPdfRenderer renderer) =>
        {
            var doc = await docs.GetAsync(id);
            if (doc is null) return Results.NotFound();

            var asHybrid = (hybrid ?? false) &&
                           (doc.Type == Matbiz.Web.Modules.Documents.Models.DocumentType.Invoice
                            || doc.Type == Matbiz.Web.Modules.Documents.Models.DocumentType.CreditNote);
            var bytes = asHybrid ? await renderer.RenderHybridAsync(doc) : await renderer.RenderAsync(doc);
            var prefix = asHybrid ? "ZUGFeRD" : Matbiz.Web.Modules.Documents.Services.DocumentService.TypeLabel(doc.Type);
            var filename = $"{prefix}_{doc.Number}.pdf".Replace(' ', '_');

            return Results.File(bytes,
                contentType: "application/pdf",
                fileDownloadName: (download ?? false) ? filename : null,
                enableRangeProcessing: false);
        });

        app.MapGet("/documents/{id:guid}/xrechnung", [Authorize] async (
            Guid id, DocumentService docs, XRechnungGenerator xr) =>
        {
            var doc = await docs.GetAsync(id);
            if (doc is null) return Results.NotFound();
            if (doc.Type != Matbiz.Web.Modules.Documents.Models.DocumentType.Invoice
                && doc.Type != Matbiz.Web.Modules.Documents.Models.DocumentType.CreditNote)
                return Results.BadRequest("XRechnung nur für Rechnungen/Gutschriften.");

            var bytes = await xr.GenerateAsync(doc);
            var filename = $"XRechnung_{doc.Number}.xml".Replace(' ', '_');
            return Results.File(bytes, contentType: "application/xml", fileDownloadName: filename);
        });

        app.MapGet("/documents/export/datev", [Authorize(Roles = "Admin")] async (
            DateTime? from, DateTime? to,
            Matbiz.Web.Modules.Accounting.Services.DatevExporter exporter) =>
        {
            var fromD = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var toD = to ?? DateTime.UtcNow.Date;
            var bytes = await exporter.ExportAsync(fromD, toD);
            var filename = $"DATEV_Buchungsstapel_{fromD:yyyyMMdd}_{toD:yyyyMMdd}.csv";
            return Results.File(bytes, "text/csv", filename);
        });

        return app;
    }
}
