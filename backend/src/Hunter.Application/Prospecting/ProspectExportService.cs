using ClosedXML.Excel;
using Hunter.Application.Campaigning;
using Hunter.Application.Common;
using Hunter.Application.Crm;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

// Reemplaza el envío automático de WhatsApp (ver ScheduledProspectAutomationService) para la
// prospección en frío: en vez de que el sistema mande el mensaje, arma un Excel con un link
// wa.me pre-cargado por prospecto y por plantilla elegida, para que una persona abra el chat a
// mano y decida qué mandar.
public class ProspectExportService(IHunterDbContext db) : IProspectExportService
{
    public async Task<Result<ProspectExcelExportResult>> ExportAsync(ExportProspectsToExcelRequest request, CancellationToken ct = default)
    {
        if (request.ProspectIds.Count == 0)
            return Result<ProspectExcelExportResult>.Failure("No se seleccionó ningún prospecto para exportar.");

        var prospects = await db.Prospects
            .Include(p => p.Contacts)
            .Where(p => request.ProspectIds.Contains(p.Id) && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        if (prospects.Count == 0)
            return Result<ProspectExcelExportResult>.Failure("Ninguno de los prospectos seleccionados existe.");

        var templates = request.MessageTemplateIds.Count == 0
            ? []
            : await db.MessageTemplates
                .Where(t => request.MessageTemplateIds.Contains(t.Id) && t.Channel == MessagingChannel.Whatsapp)
                .OrderBy(t => t.Name)
                .ToListAsync(ct);

        var bytes = BuildWorkbookBytes(prospects, templates);
        var fileName = $"prospectos-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
        return Result<ProspectExcelExportResult>.Success(new ProspectExcelExportResult(bytes, fileName));
    }

    // Para el archivo que se mantiene siempre sincronizado en Drive (ver ProspectDriveSyncService):
    // TODOS los prospectos activos, sin filtro de selección, con TODAS las plantillas de WhatsApp
    // vigentes (mismo criterio de "vigente" que ResolveDefaultCampaignAsync en
    // ScheduledProspectAutomationService, pero sin exigir que haya exactamente una — acá entran
    // todas las que califiquen, cada una suma su propio par de columnas).
    public async Task<Result<ProspectExcelExportResult>> ExportAllActiveAsync(CancellationToken ct = default)
    {
        var prospects = await db.Prospects
            .Include(p => p.Contacts)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var templates = await db.MessageTemplates
            .Where(t => t.Channel == MessagingChannel.Whatsapp && t.IsActive && !t.IsCatalogTemplate && !t.IsFollowUpTemplate)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var bytes = BuildWorkbookBytes(prospects, templates);
        return Result<ProspectExcelExportResult>.Success(new ProspectExcelExportResult(bytes, "Prospectos.xlsx"));
    }

    private static byte[] BuildWorkbookBytes(IReadOnlyList<Prospect> prospects, IReadOnlyList<MessageTemplate> templates)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Prospectos");

        var headers = new List<string>
        {
            "Negocio", "Contacto", "Categoría", "Ciudad", "Provincia", "Dirección", "Teléfono", "Estado", "Agregado", "Maps",
            "WhatsApp — Mensaje predeterminado", "WhatsApp — Link"
        };
        foreach (var template in templates)
        {
            headers.Add($"{template.Name} — Mensaje");
            headers.Add($"{template.Name} — WhatsApp");
        }

        for (var col = 0; col < headers.Count; col++)
            sheet.Cell(1, col + 1).Value = headers[col];
        sheet.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var prospect in prospects)
        {
            var phone = ResolveWhatsAppPhone(prospect);
            var col = 1;

            sheet.Cell(row, col++).Value = prospect.BusinessName;
            sheet.Cell(row, col++).Value = prospect.ContactName ?? string.Empty;
            sheet.Cell(row, col++).Value = prospect.Category.ToString();
            sheet.Cell(row, col++).Value = prospect.City ?? string.Empty;
            sheet.Cell(row, col++).Value = prospect.Province ?? string.Empty;
            sheet.Cell(row, col++).Value = prospect.Address ?? string.Empty;
            sheet.Cell(row, col++).Value = phone ?? string.Empty;
            sheet.Cell(row, col++).Value = prospect.Status.ToString();
            sheet.Cell(row, col++).Value = prospect.CreatedAt.UtcDateTime;

            var mapsCell = sheet.Cell(row, col++);
            mapsCell.Value = "Ver mapa";
            mapsCell.SetHyperlink(new XLHyperlink(ProspectLinkBuilder.BuildMapsLink(prospect.BusinessName, prospect.Address, prospect.City)));
            StyleAsLink(mapsCell);

            // Columna siempre presente, no depende de que haya una MessageTemplate activa (a
            // diferencia de las columnas por plantilla de abajo) — mismo saludo por defecto que
            // ya se usa en la derivación de leads por Telegram (LeadHandoffMessageBuilder), para
            // no tener dos mensajes "por defecto" distintos dando vueltas.
            var defaultMessage = LeadHandoffMessageBuilder.BuildDefaultGreeting(prospect);
            sheet.Cell(row, col++).Value = defaultMessage;

            var defaultLinkCell = sheet.Cell(row, col++);
            if (phone is null)
            {
                defaultLinkCell.Value = "Sin teléfono";
            }
            else
            {
                defaultLinkCell.Value = "Abrir WhatsApp";
                defaultLinkCell.SetHyperlink(new XLHyperlink(ProspectLinkBuilder.BuildWhatsAppLink(phone, defaultMessage)));
                StyleAsLink(defaultLinkCell);
            }

            foreach (var template in templates)
            {
                var content = TemplateRenderer.Render(template.Content, prospect);
                sheet.Cell(row, col++).Value = content;

                var linkCell = sheet.Cell(row, col++);
                if (phone is null)
                {
                    linkCell.Value = "Sin teléfono";
                }
                else
                {
                    linkCell.Value = "Abrir WhatsApp";
                    linkCell.SetHyperlink(new XLHyperlink(ProspectLinkBuilder.BuildWhatsAppLink(phone, content)));
                    StyleAsLink(linkCell);
                }
            }

            row++;
        }

        sheet.Columns().AdjustToContents();

        // Fila de encabezado siempre visible al bajar (evita el "scroll infinito" en un archivo
        // que ya tiene cientos de filas y va a seguir creciendo) + autofiltro con los desplegables
        // nativos de Excel/Sheets en cada columna, para ordenar/filtrar por Estado, Agregado, etc.
        // sin tener que tocar nada del lado del backend.
        sheet.SheetView.FreezeRows(1);
        if (row > 2)
            sheet.Range(1, 1, row - 1, headers.Count).SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void StyleAsLink(IXLCell cell)
    {
        cell.Style.Font.FontColor = XLColor.Blue;
        cell.Style.Font.Underline = XLFontUnderlineValues.Single;
    }

    // Mismo criterio que CampaignService al elegir el contacto para un envío de WhatsApp: prioriza
    // el contacto marcado Whatsapp; si no hay ninguno cargado como tal, usa el primer teléfono
    // activo (wa.me funciona igual con cualquier número, sea o no el que el prospecto identificó
    // como "de WhatsApp").
    private static string? ResolveWhatsAppPhone(Prospect prospect)
    {
        var whatsapp = prospect.Contacts
            .Where(c => c.IsActive && c.Channel == ProspectContactChannel.Whatsapp)
            .OrderByDescending(c => c.IsPrimary)
            .FirstOrDefault();
        if (whatsapp is not null)
            return whatsapp.Value;

        var phone = prospect.Contacts
            .Where(c => c.IsActive && c.Channel == ProspectContactChannel.Phone)
            .OrderByDescending(c => c.IsPrimary)
            .FirstOrDefault();
        return phone?.Value;
    }
}
