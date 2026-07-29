using Hunter.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class CostConfiguration : IEntityTypeConfiguration<Cost>
{
    public void Configure(EntityTypeBuilder<Cost> builder)
    {
        builder.ToTable("costs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReferenceId).HasMaxLength(200);
        builder.Property(x => x.Amount).HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.CampaignId);
    }
}
