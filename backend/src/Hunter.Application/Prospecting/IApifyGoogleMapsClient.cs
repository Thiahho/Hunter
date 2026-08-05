namespace Hunter.Application.Prospecting;

// A diferencia de OpenStreetMapPlaceResult, no incluye Category: Apify devuelve categoryName como
// texto libre de Google Maps (ej. "Peluquería", "Taller mecánico"), que no mapea 1:1 a
// ProspectCategory — mismo criterio que GooglePlaceResult, que también llega siempre sin rubro.
public record ApifyPlaceResult(
    string PlaceId,
    string Name,
    string? Address,
    string? City,
    string? Province,
    string? PhoneNumber);

// Keywords: términos de búsqueda libres (rubro escrito por el usuario, sin restricción a un
// enum) — a diferencia de OpenStreetMapSearchCriteria, acá SIEMPRE son texto libre porque Apify
// scrapea Google Maps por texto, no por tags; no hay necesidad de un mapeo rubro→tag.
public record ApifySearchCriteria(
    IReadOnlyCollection<string> Keywords,
    IReadOnlyCollection<string> Localities,
    int MaxResults);

public interface IApifyGoogleMapsClient
{
    Task<IReadOnlyList<ApifyPlaceResult>> SearchAsync(ApifySearchCriteria criteria, CancellationToken ct = default);
}
