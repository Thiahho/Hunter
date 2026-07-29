using Hunter.Domain.Campaigning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.ExternalMessageId).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Cost).HasColumnType("numeric(14,2)");
        builder.Property(x => x.Currency).HasMaxLength(3);

        builder.HasIndex(x => new { x.OrganizationId, x.ProspectId });
        builder.HasIndex(x => x.CampaignId);
        builder.HasIndex(x => x.ExternalMessageId).IsUnique().HasFilter("external_message_id IS NOT NULL");

        builder.HasOne(x => x.Prospect)
            .WithMany()
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
