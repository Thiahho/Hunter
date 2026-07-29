using Hunter.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.City).HasMaxLength(120);
        builder.Property(x => x.Province).HasMaxLength(120);
        builder.Property(x => x.Country).HasMaxLength(120);
        builder.Property(x => x.Timezone).HasMaxLength(60).IsRequired();

        builder.HasIndex(x => x.TaxId)
            .IsUnique()
            .HasFilter("tax_id IS NOT NULL");

        builder.HasMany(x => x.Users)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
