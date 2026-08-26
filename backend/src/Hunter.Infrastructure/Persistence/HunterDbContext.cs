using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Compliance;
using Hunter.Domain.Crm;
using Hunter.Domain.Finance;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Infrastructure.Persistence;

public class HunterDbContext : DbContext, IHunterDbContext
{
    public int? CurrentOrganizationId { get; }

    public HunterDbContext(DbContextOptions<HunterDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        CurrentOrganizationId = currentUserService.OrganizationId;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<ProspectContact> ProspectContacts => Set<ProspectContact>();
    public DbSet<ProspectSource> ProspectSources => Set<ProspectSource>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProspectTag> ProspectTags => Set<ProspectTag>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportBatchRecord> ImportBatchRecords => Set<ImportBatchRecord>();
    public DbSet<ScheduledProspectAutomation> ScheduledProspectAutomations => Set<ScheduledProspectAutomation>();

    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageResponse> MessageResponses => Set<MessageResponse>();
    public DbSet<Suppression> Suppressions => Set<Suppression>();
    public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<LeadAssignmentCursor> LeadAssignmentCursors => Set<LeadAssignmentCursor>();
    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<Cost> Costs => Set<Cost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HunterDbContext).Assembly);

        // Nota: se compara directamente contra CurrentOrganizationId (int?) en vez de
        // "HasValue && ... .Value" — con ese patrón, EF Core puede evaluar .Value durante
        // la extracción de parámetros antes de aplicar el cortocircuito de HasValue, lo que
        // lanza InvalidOperationException cuando no hay organización autenticada en vez de
        // simplemente devolver cero filas. La comparación directa es null-safe (int == null
        // siempre es false) y preserva el mismo comportamiento "deny by default".
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(o => o.Id == CurrentOrganizationId);

        modelBuilder.Entity<OrganizationSettings>()
            .HasQueryFilter(s => s.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Prospect>()
            .HasQueryFilter(p => p.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<ProspectContact>()
            .HasQueryFilter(c => c.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<ProspectSource>()
            .HasQueryFilter(s => s.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Tag>()
            .HasQueryFilter(t => t.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<ImportBatch>()
            .HasQueryFilter(b => b.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<ScheduledProspectAutomation>()
            .HasQueryFilter(a => a.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<MessageTemplate>()
            .HasQueryFilter(t => t.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Campaign>()
            .HasQueryFilter(c => c.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<CampaignRecipient>()
            .HasQueryFilter(r => r.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Message>()
            .HasQueryFilter(m => m.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<MessageResponse>()
            .HasQueryFilter(m => m.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Suppression>()
            .HasQueryFilter(s => s.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<ScheduledMessage>()
            .HasQueryFilter(s => s.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Lead>()
            .HasQueryFilter(l => l.OrganizationId == CurrentOrganizationId);

        // FollowUp y LeadActivity no tenían OrganizationId ni filtro propio: un lookup directo
        // por Id (ej. CompleteFollowUpAsync) no pasaba por el Lead dueño y dejaba completar/ver
        // seguimientos de otra organización con solo adivinar el Id (auditoria.md, hallazgo #1).
        modelBuilder.Entity<LeadActivity>()
            .HasQueryFilter(a => a.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<FollowUp>()
            .HasQueryFilter(f => f.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<LeadAssignmentCursor>()
            .HasQueryFilter(c => c.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Sale>()
            .HasQueryFilter(s => s.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<Cost>()
            .HasQueryFilter(c => c.OrganizationId == CurrentOrganizationId);
    }
}
