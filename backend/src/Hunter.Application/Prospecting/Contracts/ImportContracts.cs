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
