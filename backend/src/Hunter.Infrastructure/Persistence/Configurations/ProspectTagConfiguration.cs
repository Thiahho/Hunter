using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class ProspectTagConfiguration : IEntityTypeConfiguration<ProspectTag>
{
    public void Configure(EntityTypeBuilder<ProspectTag> builder)
    {
        builder.ToTable("prospect_tags");

        builder.HasKey(x => new { x.ProspectId, x.TagId });

        builder.HasOne(x => x.Tag)
            .WithMany(x => x.ProspectTags)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
