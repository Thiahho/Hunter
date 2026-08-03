using Hunter.Domain.Campaigning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class MessageResponseConfiguration : IEntityTypeConfiguration<MessageResponse>
{
    public void Configure(EntityTypeBuilder<MessageResponse> builder)
    {
        builder.ToTable("message_responses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Classification).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Confidence).HasColumnType("numeric(4,2)");
        builder.Property(x => x.AiModel).HasMaxLength(100);
        builder.Property(x => x.AiPromptVersion).HasMaxLength(50);
        builder.Property(x => x.ExternalInboundId).HasMaxLength(200);
        builder.Property(x => x.ButtonPayload).HasMaxLength(100);

        builder.HasIndex(x => new { x.OrganizationId, x.ProspectId });
        builder.HasIndex(x => x.CampaignId);
        builder.HasIndex(x => new { x.OrganizationId, x.ExternalInboundId }).IsUnique().HasFilter("external_inbound_id IS NOT NULL");

        builder.HasOne(x => x.Prospect)
            .WithMany()
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
