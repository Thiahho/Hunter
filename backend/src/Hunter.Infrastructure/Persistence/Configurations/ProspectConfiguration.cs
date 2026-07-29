using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class ProspectConfiguration : IEntityTypeConfiguration<Prospect>
{
    public void Configure(EntityTypeBuilder<Prospect> builder)
    {
        builder.ToTable("prospects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactName).HasMaxLength(200);

        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.BusinessSize).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.RecurrencePotential).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DistanceCategory).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DataQuality).HasConversion<string>().HasMaxLength(5);
        builder.Property(x => x.OperationalPriority).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.City).HasMaxLength(120);
        builder.Property(x => x.Province).HasMaxLength(120);
        builder.Property(x => x.Country).HasMaxLength(120);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.Website).HasMaxLength(300);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.City });
        builder.HasIndex(x => new { x.OrganizationId, x.Category });
        builder.HasIndex(x => new { x.OrganizationId, x.OperationalPriority });

        builder.HasMany(x => x.Contacts)
            .WithOne(x => x.Prospect)
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Sources)
            .WithOne(x => x.Prospect)
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ProspectTags)
            .WithOne(x => x.Prospect)
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
