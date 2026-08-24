using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

public interface IProspectExportService
{
    Task<Result<ProspectExcelExportResult>> ExportAsync(ExportProspectsToExcelRequest request, CancellationToken ct = default);
    Task<Result<ProspectExcelExportResult>> ExportAllActiveAsync(CancellationToken ct = default);
}
