namespace Hunter.Application.Prospecting;

public record GooglePlaceResult(
    string PlaceId,
    string Name,
    string? FormattedAddress,
    string? City,
    string? Province,
    string? PhoneNumber);

public interface IGooglePlacesClient
{
    Task<IReadOnlyList<GooglePlaceResult>> SearchTextAsync(string query, int maxResults, CancellationToken ct = default);
}
