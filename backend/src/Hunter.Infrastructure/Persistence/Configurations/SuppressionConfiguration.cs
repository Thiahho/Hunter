using Hunter.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class SuppressionConfiguration : IEntityTypeConfiguration<Suppression>
{
    public void Configure(EntityTypeBuilder<Suppression> builder)
    {
        builder.ToTable("suppressions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Contact).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Source).HasMaxLength(200);

        builder.HasIndex(x => new { x.OrganizationId, x.Contact }).IsUnique();
    }
}
