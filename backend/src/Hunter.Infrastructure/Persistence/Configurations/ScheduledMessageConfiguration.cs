using Hunter.Domain.Campaigning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class ScheduledMessageConfiguration : IEntityTypeConfiguration<ScheduledMessage>
{
    public void Configure(EntityTypeBuilder<ScheduledMessage> builder)
    {
        builder.ToTable("scheduled_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FailureReason).HasMaxLength(500);

        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.ScheduledAt });
        builder.HasIndex(x => x.ProspectId);

        builder.HasOne(x => x.Prospect)
            .WithMany()
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MessageTemplate)
            .WithMany()
            .HasForeignKey(x => x.MessageTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
