using Hunter.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.TelegramChatId).HasMaxLength(30);
        builder.Property(x => x.TelegramLinkCode).HasMaxLength(64);
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Area).HasConversion<string>().HasMaxLength(20).HasDefaultValue(UserArea.Unassigned);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.Email }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Area });
    }
}
