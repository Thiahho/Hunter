namespace Hunter.Application.Common;

// Links reusados en dos lugares: la derivación de leads entrantes por Telegram
// (LeadHandoffMessageBuilder) y la exportación de prospectos a Excel (ProspectExportService).
public static class ProspectLinkBuilder
{
    // https://wa.me/<número sin "+" ni espacios>?text=<mensaje pre-cargado, url-encoded>. Meta
    // no exige "+", el normalizador de contactos ya deja solo dígitos, así que alcanza con
    // interpolar directo.
    public static string BuildWhatsAppLink(string phone, string? prefilledText = null) =>
        prefilledText is null
            ? $"https://wa.me/{phone}"
            : $"https://wa.me/{phone}?text={Uri.EscapeDataString(prefilledText)}";

    // Sin lat/long confiable en todos los prospectos, se arma como búsqueda por nombre +
    // dirección en vez de depender de coordenadas.
    public static string BuildMapsLink(string businessName, string? address, string? city)
    {
        var query = string.Join(" ", new[] { businessName, address, city }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        return $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(query)}";
    }
}
