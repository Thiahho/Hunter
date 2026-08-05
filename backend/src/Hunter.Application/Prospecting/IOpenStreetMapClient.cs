using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting;

// A diferencia de GooglePlaceResult, incluye Category: los tags de OSM (shop=car_parts,
// shop=car_repair, etc.) sí permiten deducirla, cosa que la ruta de Google Places no hace.
public record OpenStreetMapPlaceResult(
    string ElementId,
    string Name,
    string? Address,
    string? City,
    string? Province,
    string? PhoneNumber,
    ProspectCategory Category);

public record OpenStreetMapSearchCriteria(
    IReadOnlyCollection<string> Localities,
    IReadOnlyCollection<ProspectCategory> Categories,
    int? RadiusKm,
    int MaxResults,
    // Términos de búsqueda libre (rubros no mapeados a un tag conocido de OSM, ej.
    // "peluquería"): se buscan por coincidencia en el nombre del comercio en vez de por tag
    // exacto. Ver OpenStreetMapClient.KeywordToOsmFilter.
    IReadOnlyCollection<string>? Keywords = null);

public static class OpenStreetMapCategories
{
    // Único subconjunto de ProspectCategory con mapeo directo a un tag de OSM (shop=... o
    // service:vehicle:oil_change=yes). Unknown/Distributor/Other no tienen equivalente y no
    // son buscables acá; ImportService valida contra esta lista antes de llamar al cliente.
    public static readonly IReadOnlyCollection<ProspectCategory> Supported =
    [
        ProspectCategory.AutoPartsStore,
        ProspectCategory.Workshop,
        ProspectCategory.TireShop,
        ProspectCategory.Reseller,
        ProspectCategory.Lubricentro
    ];
}

public interface IOpenStreetMapClient
{
    Task<IReadOnlyList<OpenStreetMapPlaceResult>> SearchAsync(OpenStreetMapSearchCriteria criteria, CancellationToken ct = default);
}
