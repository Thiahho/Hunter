using Hunter.Domain.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.ToTable("follow_ups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.OrganizationId, x.LeadId });
        builder.HasIndex(x => x.ScheduledAt);
    }
}
