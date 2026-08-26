namespace Hunter.Application.Prospecting.Contracts;

public record ExportProspectsToExcelRequest(
    IReadOnlyCollection<int> ProspectIds,
    IReadOnlyCollection<int> MessageTemplateIds);

public record ProspectExcelExportResult(byte[] Content, string FileName);
