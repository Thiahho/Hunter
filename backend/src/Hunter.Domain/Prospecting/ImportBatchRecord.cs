namespace Hunter.Domain.Prospecting;

public class ImportBatchRecord
{
    public int Id { get; set; }

    public int ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    public int RowNumber { get; set; }

    public string RawData { get; set; } = null!;
    public string? NormalizedData { get; set; }

    public ImportBatchRecordStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    public int? ProspectId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
