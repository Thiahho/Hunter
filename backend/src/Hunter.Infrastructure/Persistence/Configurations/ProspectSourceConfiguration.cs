using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class ProspectSourceConfiguration : IEntityTypeConfiguration<ProspectSource>
{
    public void Configure(EntityTypeBuilder<ProspectSource> builder)
    {
        builder.ToTable("prospect_sources");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.SourceUrl).HasMaxLength(500);

        builder.HasIndex(x => new { x.SourceType, x.ExternalId });
    }
}
