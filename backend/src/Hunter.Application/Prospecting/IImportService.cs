using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

public interface IImportService
{
    Task<Result<ImportPreviewDto>> ImportCsvAsync(Stream csvStream, string fileName, CancellationToken ct = default);
    Task<Result<ImportPreviewDto>> ImportFromGooglePlacesAsync(GooglePlacesImportRequest request, CancellationToken ct = default);
    Task<Result<ImportPreviewDto>> GetPreviewAsync(int batchId, CancellationToken ct = default);
    Task<Result<ImportConfirmResultDto>> ConfirmAsync(int batchId, CancellationToken ct = default);
    Task<Result<bool>> CancelAsync(int batchId, CancellationToken ct = default);
}
