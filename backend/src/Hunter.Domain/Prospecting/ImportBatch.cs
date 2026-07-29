namespace Hunter.Domain.Prospecting;

public class ImportBatch
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public string FileName { get; set; } = null!;
    public int? SourceId { get; set; }
    public ProspectSourceType SourceType { get; set; } = ProspectSourceType.CsvImport;

    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int DuplicateRecords { get; set; }
    public int InvalidRecords { get; set; }
    public int SuppressedRecords { get; set; }

    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Processing;

    public int? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ImportBatchRecord> Records { get; set; } = new List<ImportBatchRecord>();
}
