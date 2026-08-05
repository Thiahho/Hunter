namespace Hunter.Infrastructure.Prospecting;

public class NominatimOptions
{
    public const string SectionName = "Nominatim";

    public string Endpoint { get; set; } = "https://nominatim.openstreetmap.org/search";

    // Misma exigencia que Overpass (ambos son del ecosistema OSM): un User-Agent que
    // identifique la app de verdad, no el genérico de la librería HTTP.
    public string UserAgent { get; set; } = "HunterCRM/1.0 (contact: CHANGE_ME_CONTACT_EMAIL)";

    public int TimeoutSeconds { get; set; } = 10;
}
