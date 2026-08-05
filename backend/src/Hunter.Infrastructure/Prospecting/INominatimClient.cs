namespace Hunter.Infrastructure.Prospecting;

// Colaborador interno de OpenStreetMapClient (no lo consume ImportService directamente): por
// eso vive solo en Infrastructure, sin contraparte en Application. Resuelve el lat/lon que
// Overpass necesita para una búsqueda por radio (around:).
public interface INominatimClient
{
    Task<(double Lat, double Lon)?> GeocodeAsync(string query, CancellationToken ct = default);
}
