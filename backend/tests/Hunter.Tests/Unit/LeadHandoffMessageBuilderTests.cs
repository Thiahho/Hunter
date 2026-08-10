using Hunter.Application.Crm;
using Hunter.Domain.Prospecting;

namespace Hunter.Tests.Unit;

public class LeadHandoffMessageBuilderTests
{
    private static Prospect FullProspect() => new()
    {
        BusinessName = "Repuestos Oeste",
        City = "Moreno",
        Category = ProspectCategory.AutoPartsStore,
        CommercialScore = 92
    };

    [Fact]
    public void BuildFreeText_FullProspect_IncludesAllLines()
    {
        var text = LeadHandoffMessageBuilder.BuildFreeText(FullProspect(), "¿Me pasás catálogo?", "5491112345678");

        Assert.Contains("🔥 NUEVO LEAD", text);
        Assert.Contains("Repuestos Oeste", text);
        Assert.Contains("📍 Moreno", text);
        Assert.Contains("🏷 Casa de repuestos", text);
        Assert.Contains("📊 Score: 92", text);
        Assert.Contains("📱 https://wa.me/5491112345678", text);
        Assert.Contains("¿Me pasás catálogo?", text);
    }

    [Fact]
    public void BuildFreeText_NoCityNoScore_OmitsThoseLines()
    {
        var prospect = new Prospect { BusinessName = "Taller Juan", Category = ProspectCategory.Workshop };

        var text = LeadHandoffMessageBuilder.BuildFreeText(prospect, "hola", "5491112345678");

        Assert.DoesNotContain("📍", text);
        Assert.DoesNotContain("📊", text);
        Assert.Contains("🏷 Taller", text);
    }

    [Fact]
    public void BuildFreeText_TruncatesLongProspectMessage()
    {
        var longMessage = new string('a', 500);

        var text = LeadHandoffMessageBuilder.BuildFreeText(FullProspect(), longMessage, "5491112345678");

        Assert.Contains("…", text);
        Assert.DoesNotContain(new string('a', 400), text);
    }

    [Fact]
    public void BuildFreeText_NoAssigneeFirstName_OmitsSuggestedReply()
    {
        var text = LeadHandoffMessageBuilder.BuildFreeText(FullProspect(), "hola", "5491112345678");

        Assert.DoesNotContain("Sugerencia de respuesta", text);
        Assert.DoesNotContain("Mi nombre es", text);
    }

    [Fact]
    public void BuildFreeText_WithAssigneeFirstName_IncludesSuggestedReply_UsingContactNameWhenPresent()
    {
        var prospect = FullProspect();
        prospect.ContactName = "Marcelo";

        var text = LeadHandoffMessageBuilder.BuildFreeText(prospect, "hola", "5491112345678", "Juan");

        Assert.Contains("💬 Sugerencia de respuesta:", text);
        Assert.Contains("Hola Marcelo!", text);
        Assert.Contains("Difrani", text);
    }

    [Fact]
    public void BuildFreeText_WithAssigneeFirstName_NoContactName_UsesBusinessNameInGreeting()
    {
        var text = LeadHandoffMessageBuilder.BuildFreeText(FullProspect(), "hola", "5491112345678", "Juan");

        Assert.Contains("Hola Repuestos Oeste!", text);
    }

    [Fact]
    public void BuildFreeText_NoAssigneeFirstName_WhatsAppLinkHasNoPrefilledText()
    {
        var text = LeadHandoffMessageBuilder.BuildFreeText(FullProspect(), "hola", "5491112345678");

        Assert.Contains("📱 https://wa.me/5491112345678", text);
        Assert.DoesNotContain("?text=", text);
    }

    [Fact]
    public void BuildFreeText_WithAssigneeFirstName_WhatsAppLinkHasUrlEncodedPrefilledText()
    {
        var text = LeadHandoffMessageBuilder.BuildFreeText(FullProspect(), "hola", "5491112345678", "Juan");

        // wa.me/<numero>?text=<sugerencia url-encoded>, para que un clic abra el chat correcto
        // con la respuesta ya cargada en el textbox, lista para revisar y mandar.
        var expectedText = Uri.EscapeDataString(
            "Hola Repuestos Oeste! ¿Cómo estás? Soy de Difrani, fábrica de mazas de rueda, rótulas, extremos y bieletas.");
        Assert.Contains($"📱 https://wa.me/5491112345678?text={expectedText}", text);
    }

    [Fact]
    public void BuildFreeText_AlwaysIncludesDispatchLink_WithCustomerDataAsList()
    {
        var prospect = FullProspect();
        prospect.Address = "Av. Rivadavia 1234";

        var text = LeadHandoffMessageBuilder.BuildFreeText(prospect, "hola", "5491112345678");

        Assert.Contains("📋 Derivar: https://wa.me/5491132948959?text=", text);

        var expectedListText = Uri.EscapeDataString(
            "Nombre: Repuestos Oeste\nUbicación: Moreno\nDirección: Av. Rivadavia 1234\nNúmero: 5491112345678\nRubro: Casa de repuestos\nMaps: " +
            Uri.EscapeDataString("https://www.google.com/maps/search/?api=1&query=Repuestos Oeste Av. Rivadavia 1234 Moreno"));

        // El "Maps:" queda doblemente encodeado dentro del texto de la lista (el link interno ya
        // trae su propio query encodeado, y todo el bloque se vuelve a encodear para el ?text=).
        Assert.Contains(Uri.EscapeDataString("Nombre: Repuestos Oeste"), text);
        Assert.Contains(Uri.EscapeDataString("Dirección: Av. Rivadavia 1234"), text);
        Assert.Contains(Uri.EscapeDataString("Número: 5491112345678"), text);
        Assert.Contains(Uri.EscapeDataString("Rubro: Casa de repuestos"), text);
        Assert.Contains(Uri.EscapeDataString("maps.google.com").ToLowerInvariant(), text.ToLowerInvariant());
    }

    [Fact]
    public void BuildFreeText_MissingAddress_DispatchLinkUsesPlaceholder()
    {
        var prospect = new Prospect { BusinessName = "Taller Juan", City = "Moreno", Category = ProspectCategory.Workshop };

        var text = LeadHandoffMessageBuilder.BuildFreeText(prospect, "hola", "5491112345678");

        Assert.Contains(Uri.EscapeDataString("Dirección: -"), text);
    }

    [Fact]
    public void BuildTemplateParameters_ReturnsFiveParametersInOrder()
    {
        var parameters = LeadHandoffMessageBuilder.BuildTemplateParameters(FullProspect(), "¿Me pasás catálogo?");

        Assert.Equal(5, parameters.Count);
        Assert.Equal("Repuestos Oeste", parameters[0]);
        Assert.Equal("Moreno", parameters[1]);
        Assert.Equal("Casa de repuestos", parameters[2]);
        Assert.Equal("92", parameters[3]);
        Assert.Equal("¿Me pasás catálogo?", parameters[4]);
    }

    [Fact]
    public void BuildTemplateParameters_MultilineProspectMessage_HasNoNewlinesTabsOrLongSpaceRuns()
    {
        // Guarda contra el error #132000 de Meta: un parámetro de plantilla con salto de línea,
        // tab o 4+ espacios seguidos hace que rechace el envío entero.
        var messyMessage = "Hola\n\nquiero\tinfo    del catalogo\r\ncompleto";

        var parameters = LeadHandoffMessageBuilder.BuildTemplateParameters(FullProspect(), messyMessage);
        var sanitizedMessage = parameters[4];

        Assert.DoesNotContain("\n", sanitizedMessage);
        Assert.DoesNotContain("\r", sanitizedMessage);
        Assert.DoesNotContain("\t", sanitizedMessage);
        Assert.DoesNotContain("    ", sanitizedMessage);
    }

    [Fact]
    public void BuildTemplateParameters_MissingCityAndScore_UsesPlaceholder()
    {
        var prospect = new Prospect { BusinessName = "Taller Juan", Category = ProspectCategory.Workshop };

        var parameters = LeadHandoffMessageBuilder.BuildTemplateParameters(prospect, "hola");

        Assert.Equal("-", parameters[1]);
        Assert.Equal("-", parameters[3]);
    }
}
