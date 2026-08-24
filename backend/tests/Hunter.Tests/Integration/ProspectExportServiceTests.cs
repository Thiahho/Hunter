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

        Assert.Equal("Bienvenida — Mensaje", sheet.Cell(1, 10).GetString());
        Assert.Equal("Bienvenida — WhatsApp", sheet.Cell(1, 11).GetString());

        Assert.Equal("Repuestos Oeste", sheet.Cell(2, 1).GetString());
        Assert.Equal("Hola Repuestos Oeste, ¿cómo estás?", sheet.Cell(2, 10).GetString());

        var linkCell = sheet.Cell(2, 11);
        Assert.Equal("Abrir WhatsApp", linkCell.GetString());
        Assert.True(linkCell.HasHyperlink);
        var link = linkCell.GetHyperlink().ExternalAddress!.ToString();
        Assert.StartsWith("https://wa.me/5491112345678?text=", link);
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

        var linkCell = sheet.Cell(2, 11);
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
}
