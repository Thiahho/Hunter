using Hunter.Domain.Campaigning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => new { x.OrganizationId, x.Status });

        builder.HasOne(x => x.MessageTemplate)
            .WithMany()
            .HasForeignKey(x => x.MessageTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Recipients)
            .WithOne(x => x.Campaign)
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
