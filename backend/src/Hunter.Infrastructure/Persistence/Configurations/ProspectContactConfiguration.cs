using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class ProspectContactConfiguration : IEntityTypeConfiguration<ProspectContact>
{
    public void Configure(EntityTypeBuilder<ProspectContact> builder)
    {
        builder.ToTable("prospect_contacts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.Channel, x.Value }).IsUnique();
    }
}
