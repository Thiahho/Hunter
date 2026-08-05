using System.Text;
using System.Text.RegularExpressions;
using Hunter.Domain.Prospecting;

namespace Hunter.Application.Crm;

// Arma el resumen de derivación al vendedor/administrativo asignado (doc 23, sección 37):
// empresa, ubicación, rubro, score y el mensaje del prospecto. Dos formas de salida porque el
// envío por WhatsApp tiene dos caminos: texto libre (solo funciona dentro de la ventana de
// servicio de 24hs) o los parámetros de una plantilla UTILITY aprobada (funciona siempre, pero
// Meta rechaza parámetros con saltos de línea o corridas de espacios: #132000).
public static class LeadHandoffMessageBuilder
{
    private const int MaxProspectMessageLength = 300;
    private static readonly Regex WhitespaceRun = new(@"[\r\n\t]+|[ ]{2,}", RegexOptions.Compiled);

    private static readonly Dictionary<ProspectCategory, string> CategoryDisplayNames = new()
    {
        [ProspectCategory.Unknown] = "Sin clasificar",
        [ProspectCategory.Distributor] = "Mayorista/Distribuidor",
        [ProspectCategory.AutoPartsStore] = "Casa de repuestos",
        [ProspectCategory.Workshop] = "Taller",
        [ProspectCategory.Lubricentro] = "Lubricentro",
        [ProspectCategory.TireShop] = "Gomería",
        [ProspectCategory.Reseller] = "Revendedor",
        [ProspectCategory.Other] = "Otro"
    };

    // prospectWhatsApp: el vendedor lo necesita para poder responderle directo sin ir a buscarlo
    // al CRM. assigneeFirstName es opcional (nombre del usuario logueado que recibe el handoff):
    // si viene, se agrega una sugerencia de respuesta lista para copiar y pegar; si no, se omite
    // ese bloque entero en vez de dejar un saludo con nombre vacío.
    public static string BuildFreeText(Prospect prospect, string prospectMessage, string prospectWhatsApp, string? assigneeFirstName = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🔥 NUEVO LEAD");
        sb.AppendLine();
        sb.AppendLine(prospect.BusinessName);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(prospect.City))
            sb.AppendLine($"📍 {prospect.City}");

        sb.AppendLine($"🏷 {DisplayName(prospect.Category)}");

        if (prospect.CommercialScore is not null)
            sb.AppendLine($"📊 Score: {prospect.CommercialScore}");

        sb.AppendLine($"📱 {prospectWhatsApp}");

        sb.AppendLine();
        sb.AppendLine("Mensaje:");
        sb.AppendLine($"\"{Truncate(prospectMessage)}\"");

        if (string.IsNullOrWhiteSpace(assigneeFirstName))
            return sb.ToString().TrimEnd();

        var greetingName = string.IsNullOrWhiteSpace(prospect.ContactName) ? prospect.BusinessName : prospect.ContactName;
        sb.AppendLine();
        sb.AppendLine("💬 Sugerencia de respuesta:");
        sb.Append($"\"Hola {greetingName}! ¿Cómo estás? Mi nombre es {assigneeFirstName}, un gusto saludarte.\"");

        return sb.ToString();
    }

    // Parámetros para la plantilla nuevo_lead (5 params: empresa, ciudad, rubro, score, mensaje).
    public static IReadOnlyList<string> BuildTemplateParameters(Prospect prospect, string prospectMessage) =>
    [
        Sanitize(prospect.BusinessName),
        Sanitize(prospect.City ?? "-"),
        Sanitize(DisplayName(prospect.Category)),
        prospect.CommercialScore?.ToString() ?? "-",
        Sanitize(Truncate(prospectMessage))
    ];

    private static string DisplayName(ProspectCategory category) =>
        CategoryDisplayNames.GetValueOrDefault(category, category.ToString());

    private static string Truncate(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= MaxProspectMessageLength
            ? trimmed
            : trimmed[..MaxProspectMessageLength] + "…";
    }

    // Meta rechaza (#132000) parámetros de plantilla con newlines, tabs o 4+ espacios seguidos.
    private static string Sanitize(string value) => WhitespaceRun.Replace(value, " ").Trim();
}
