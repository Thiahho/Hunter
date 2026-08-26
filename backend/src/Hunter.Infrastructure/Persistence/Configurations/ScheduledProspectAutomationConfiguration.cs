using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class ScheduledProspectAutomationConfiguration : IEntityTypeConfiguration<ScheduledProspectAutomation>
{
    public void Configure(EntityTypeBuilder<ScheduledProspectAutomation> builder)
    {
        builder.ToTable("scheduled_prospect_automations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SearchCriteriaJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ResultSummary).HasMaxLength(2000);

        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.ScheduledAt });
    }
}
