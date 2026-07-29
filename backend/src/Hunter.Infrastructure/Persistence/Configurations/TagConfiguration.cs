using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(20);

        builder.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
    }
}
