using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record ImportPreviewDto(
    int BatchId,
    string Status,
    int TotalRecords,
    int ValidRecords,
    int DuplicateRecords,
    int InvalidRecords);

public record ImportConfirmResultDto(int BatchId, string Status, int Created);

public record GooglePlacesImportRequest(string Query, int MaxResults = 10);

// Keywords es obligatorio y siempre texto libre (a diferencia de OpenStreetMapImportRequest.Categories):
// Apify busca por texto contra Google Maps, no hay tags cerrados que mapear, así que cualquier
// rubro que el usuario escriba sirve tal cual.
public record ApifyImportRequest(
    IReadOnlyCollection<string> Localities,
    IReadOnlyCollection<string> Keywords,
    int MaxResults = 50);

// Categories vacío/null = se buscan todos los rubros soportados por OSM
// (OpenStreetMapCategories.Supported). RadiusKm null = búsqueda por límite administrativo
// (comportamiento original); con valor, se geocodifica cada localidad y se busca en ese radio.
// Keywords: rubros libres (no restringidos a Categories) que se buscan por coincidencia en el
// nombre del comercio — ver OpenStreetMapCategories.Supported y OpenStreetMapClient.
public record OpenStreetMapImportRequest(
    IReadOnlyCollection<string> Localities,
    IReadOnlyCollection<ProspectCategory>? Categories = null,
    int? RadiusKm = null,
    int MaxResults = 50,
    IReadOnlyCollection<string>? Keywords = null);

// Detalle de una fila del batch para la tabla de preview (nombre, contacto, rubro, por qué
// quedó Duplicate/Invalid si aplica). Se arma a partir de lo que ya se persiste en
// ImportBatchRecord, no requiere columnas nuevas.
public record ImportRecordDto(
    int Id,
    int RowNumber,
    string Status,
    string? BusinessName,
    string? Category,
    string? Phone,
    string? Whatsapp,
    string? Address,
    string? City,
    string? Province,
    string? ErrorMessage);

// SelectedRecordIds null = importar todos los Valid (comportamiento de siempre, lo que sigue
// usando CSV y Google Places). Con una lista, solo se importan los Valid cuyo Id esté incluido.
public record ConfirmImportRequest(IReadOnlyCollection<int>? SelectedRecordIds = null);
