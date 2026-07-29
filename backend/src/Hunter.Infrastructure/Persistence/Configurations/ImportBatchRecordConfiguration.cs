using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class ImportBatchRecordConfiguration : IEntityTypeConfiguration<ImportBatchRecord>
{
    public void Configure(EntityTypeBuilder<ImportBatchRecord> builder)
    {
        builder.ToTable("import_batch_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RawData).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.NormalizedData).HasColumnType("jsonb");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.ImportBatchId);
    }
}
