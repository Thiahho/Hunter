using ClosedXML.Excel;
using Hunter.Application.Campaigning;
using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;

namespace Hunter.Tests.Integration;

// Reemplazo del auto-envío (ver ScheduledProspectAutomationServiceTests): en vez de mandar el
// mensaje, ProspectExportService arma un .xlsx con un link wa.me por prospecto y por plantilla
// elegida. Se valida abriendo el workbook resultante con ClosedXML, tal como lo haría alguien al
// descargarlo.
public class ProspectExportServiceTests
{
    private static async Task<(int orgId, int prospectId, int templateId)> SeedProspectWithWhatsappContactAsync(
        string dbName, string phone = "5491112345678")
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var template = new MessageTemplate
        {
            OrganizationId = org.Id,
            Name = "Bienvenida",
            Content = "Hola {{business_name}}, ¿cómo estás?",
            Channel = MessagingChannel.Whatsapp,
            IsActive = true
        };
        db.MessageTemplates.Add(template);

        var prospect = new Prospect { OrganizationId = org.Id, BusinessName = "Repuestos Oeste", City = "Moreno" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        db.ProspectContacts.Add(new ProspectContact
        {
            OrganizationId = org.Id,
            ProspectId = prospect.Id,
            Channel = ProspectContactChannel.Whatsapp,
            Value = phone,
            IsPrimary = true
        });
        await db.SaveChangesAsync();

        return (org.Id, prospect.Id, template.Id);
    }

    [Fact]
    public async Task ExportAsync_ProspectWithWhatsappContact_WritesRowWithRenderedMessageAndWaMeLink()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, prospectId, templateId) = await SeedProspectWithWhatsappContactAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectExportService(db);

        var result = await service.ExportAsync(new ExportProspectsToExcelRequest([prospectId], [templateId]));

        Assert.True(result.Succeeded);

        using var workbook = new XLWorkbook(new MemoryStream(result.Value!.Content));
        var sheet = workbook.Worksheet("Prospectos");

        // Col 11/12: WhatsApp con mensaje por defecto (siempre presente). Col 13/14: la plantilla elegida.
        Assert.Equal("WhatsApp — Mensaje predeterminado", sheet.Cell(1, 11).GetString());
        Assert.Equal("WhatsApp — Link", sheet.Cell(1, 12).GetString());
        Assert.Equal("Bienvenida — Mensaje", sheet.Cell(1, 13).GetString());
        Assert.Equal("Bienvenida — WhatsApp", sheet.Cell(1, 14).GetString());

        Assert.Equal("Repuestos Oeste", sheet.Cell(2, 1).GetString());

        Assert.Equal(
            "Hola Repuestos Oeste! ¿Cómo estás? Soy de Difrani, fábrica de mazas de rueda, rótulas, extremos y bieletas.",
            sheet.Cell(2, 11).GetString());
        var defaultLinkCell = sheet.Cell(2, 12);
        Assert.Equal("Abrir WhatsApp", defaultLinkCell.GetString());
        Assert.True(defaultLinkCell.HasHyperlink);
        Assert.StartsWith("https://wa.me/5491112345678?text=", defaultLinkCell.GetHyperlink().ExternalAddress!.ToString());

        Assert.Equal("Hola Repuestos Oeste, ¿cómo estás?", sheet.Cell(2, 13).GetString());

        var linkCell = sheet.Cell(2, 14);
        Assert.Equal("Abrir WhatsApp", linkCell.GetString());
        Assert.True(linkCell.HasHyperlink);
        var link = linkCell.GetHyperlink().ExternalAddress!.ToString();
        Assert.StartsWith("https://wa.me/5491112345678?text=", link);

        // Encabezado siempre visible (FreezeRows(1) deja SplitRow=1) + autofiltro con los
        // desplegables de ordenar/filtrar de Excel.
        Assert.Equal(1, sheet.SheetView.SplitRow);
        Assert.True(sheet.AutoFilter.IsEnabled);
    }

    [Fact]
    public async Task ExportAsync_ProspectWithoutPhone_LeavesWhatsAppColumnAsNoPhoneWithoutLink()
    {
        var dbName = TestDb.NewDbName();
        await using var seedDb = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        seedDb.Organizations.Add(org);
        await seedDb.SaveChangesAsync();

        var template = new MessageTemplate
        {
            OrganizationId = org.Id,
            Name = "Bienvenida",
            Content = "Hola {{business_name}}",
            Channel = MessagingChannel.Whatsapp,
            IsActive = true
        };
        seedDb.MessageTemplates.Add(template);

        var prospect = new Prospect { OrganizationId = org.Id, BusinessName = "Sin Teléfono" };
        seedDb.Prospects.Add(prospect);
        await seedDb.SaveChangesAsync();

        await using var db = TestDb.Create(dbName, organizationId: org.Id);
        var service = new ProspectExportService(db);

        var result = await service.ExportAsync(new ExportProspectsToExcelRequest([prospect.Id], [template.Id]));

        Assert.True(result.Succeeded);

        using var workbook = new XLWorkbook(new MemoryStream(result.Value!.Content));
        var sheet = workbook.Worksheet("Prospectos");

        var defaultLinkCell = sheet.Cell(2, 12);
        Assert.Equal("Sin teléfono", defaultLinkCell.GetString());
        Assert.False(defaultLinkCell.HasHyperlink);

        var linkCell = sheet.Cell(2, 14);
        Assert.Equal("Sin teléfono", linkCell.GetString());
        Assert.False(linkCell.HasHyperlink);
    }

    [Fact]
    public async Task ExportAsync_NoProspectIds_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        await using var db = TestDb.Create(dbName, organizationId: 1);
        var service = new ProspectExportService(db);

        var result = await service.ExportAsync(new ExportProspectsToExcelRequest([], []));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ExportAllActiveAsync_IncludesAllNonDeletedProspectsAndOnlyEligibleTemplates()
    {
        var dbName = TestDb.NewDbName();
        await using var seedDb = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        seedDb.Organizations.Add(org);
        await seedDb.SaveChangesAsync();

        var eligibleTemplate = new MessageTemplate
        {
            OrganizationId = org.Id,
            Name = "Bienvenida",
            Content = "Hola {{business_name}}",
            Channel = MessagingChannel.Whatsapp,
            IsActive = true
        };
        var inactiveTemplate = new MessageTemplate
        {
            OrganizationId = org.Id,
            Name = "Vieja",
            Content = "Vieja",
            Channel = MessagingChannel.Whatsapp,
            IsActive = false
        };
        var catalogTemplate = new MessageTemplate
        {
            OrganizationId = org.Id,
            Name = "Catálogo",
            Content = "Catálogo",
            Channel = MessagingChannel.Whatsapp,
            IsActive = true,
            IsCatalogTemplate = true
        };
        seedDb.MessageTemplates.AddRange(eligibleTemplate, inactiveTemplate, catalogTemplate);

        var active = new Prospect { OrganizationId = org.Id, BusinessName = "Activo" };
        var deleted = new Prospect { OrganizationId = org.Id, BusinessName = "Borrado", IsDeleted = true };
        seedDb.Prospects.AddRange(active, deleted);
        await seedDb.SaveChangesAsync();

        await using var db = TestDb.Create(dbName, organizationId: org.Id);
        var service = new ProspectExportService(db);

        var result = await service.ExportAllActiveAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("Prospectos.xlsx", result.Value!.FileName);

        using var workbook = new XLWorkbook(new MemoryStream(result.Value.Content));
        var sheet = workbook.Worksheet("Prospectos");

        // 10 columnas base (incluye "Agregado") + 2 de WhatsApp con mensaje por defecto (siempre)
        // + 2 de la plantilla elegible (solo la activa/no-catálogo/no-follow-up) = 14.
        Assert.Equal("WhatsApp — Mensaje predeterminado", sheet.Cell(1, 11).GetString());
        Assert.Equal("WhatsApp — Link", sheet.Cell(1, 12).GetString());
        Assert.Equal("Bienvenida — Mensaje", sheet.Cell(1, 13).GetString());
        Assert.Equal("Bienvenida — WhatsApp", sheet.Cell(1, 14).GetString());
        Assert.True(string.IsNullOrEmpty(sheet.Cell(1, 15).GetString()));

        Assert.Equal("Activo", sheet.Cell(2, 1).GetString());
        Assert.True(string.IsNullOrEmpty(sheet.Cell(3, 1).GetString())); // "Borrado" no aparece
    }
}
