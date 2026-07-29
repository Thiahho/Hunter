using Hunter.Domain.Campaigning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
{
    public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
    {
        builder.ToTable("campaign_recipients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => new { x.CampaignId, x.ProspectId }).IsUnique();
        builder.HasIndex(x => x.ProspectId);

        builder.HasOne(x => x.Prospect)
            .WithMany()
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
