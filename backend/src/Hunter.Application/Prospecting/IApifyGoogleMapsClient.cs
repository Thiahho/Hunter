namespace Hunter.Application.Prospecting;

// CategoryName: categoryName de Google Maps tal cual (texto libre, ej. "Tienda de repuestos para
// automóviles"). SearchKeyword: el rubro seleccionado/escrito que produjo este resultado (uno de
// ApifySearchCriteria.Keywords), para poder guardar el rubro aunque Google no devuelva categoría.
// ImportService deduce ProspectCategory de ambos con ProspectCategoryNames.Resolve.
public record ApifyPlaceResult(
    string PlaceId,
    string Name,
    string? Address,
    string? City,
    string? Province,
    string? PhoneNumber,
    string? CategoryName = null,
    string? SearchKeyword = null);

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
