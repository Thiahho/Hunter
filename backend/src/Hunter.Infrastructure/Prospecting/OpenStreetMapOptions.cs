namespace Hunter.Infrastructure.Prospecting;

public class OpenStreetMapOptions
{
    public const string SectionName = "OpenStreetMap";

    public string Endpoint { get; set; } = "https://overpass-api.de/api/interpreter";

    // La política de uso de Overpass/OSM exige un User-Agent que identifique la app de verdad
    // (bloquea User-Agents genéricos o falsificados): conviene incluir un contacto real acá.
    public string UserAgent { get; set; } = "HunterCRM/1.0 (contact: thiagobrizuelav@gmail.com)";

    // Tiempo que se le pide a Overpass que use como límite interno de la query ([timeout:N]);
    // el timeout real del HttpClient se arma sumándole margen (ver DependencyInjection).
    public int TimeoutSeconds { get; set; } = 60;
}
