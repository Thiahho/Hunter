using Hunter.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(30).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new Role { Id = RoleIds.Owner, Name = RoleNames.Owner },
            new Role { Id = RoleIds.Admin, Name = RoleNames.Admin },
            new Role { Id = RoleIds.Manager, Name = RoleNames.Manager },
            new Role { Id = RoleIds.Seller, Name = RoleNames.Seller });
    }
}
