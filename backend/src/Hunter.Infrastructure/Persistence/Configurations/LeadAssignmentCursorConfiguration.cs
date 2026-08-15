using Hunter.Domain.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hunter.Infrastructure.Persistence.Configurations;

public class LeadAssignmentCursorConfiguration : IEntityTypeConfiguration<LeadAssignmentCursor>
{
    public void Configure(EntityTypeBuilder<LeadAssignmentCursor> builder)
    {
        builder.ToTable("lead_assignment_cursors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Area).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Version).IsConcurrencyToken();

        // Único por organización+área para los casos con área real; Area=null (pool general) no
        // queda protegido por este índice porque Postgres trata cada NULL como distinto — ver el
        // comentario sobre el retry-on-conflict en LeadAssignment.PickNextAssigneeAsync.
        builder.HasIndex(x => new { x.OrganizationId, x.Area }).IsUnique();
    }
}
