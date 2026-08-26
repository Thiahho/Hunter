using Hunter.Domain.Campaigning;
using Hunter.Domain.Compliance;
using Hunter.Domain.Crm;
using Hunter.Domain.Finance;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hunter.Application.Common;

public interface IHunterDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationSettings> OrganizationSettings { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Prospect> Prospects { get; }
    DbSet<ProspectContact> ProspectContacts { get; }
    DbSet<ProspectSource> ProspectSources { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ProspectTag> ProspectTags { get; }
    DbSet<ImportBatch> ImportBatches { get; }
    DbSet<ImportBatchRecord> ImportBatchRecords { get; }
    DbSet<ScheduledProspectAutomation> ScheduledProspectAutomations { get; }

    DbSet<MessageTemplate> MessageTemplates { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<CampaignRecipient> CampaignRecipients { get; }
    DbSet<Message> Messages { get; }
    DbSet<MessageResponse> MessageResponses { get; }
    DbSet<Suppression> Suppressions { get; }
    DbSet<ScheduledMessage> ScheduledMessages { get; }

    DbSet<Lead> Leads { get; }
    DbSet<LeadActivity> LeadActivities { get; }
    DbSet<FollowUp> FollowUps { get; }
    DbSet<LeadAssignmentCursor> LeadAssignmentCursors { get; }
    DbSet<Sale> Sales { get; }

    DbSet<Cost> Costs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Necesario para LeadAssignment.ClaimNextIndexAsync: tras un choque de concurrencia
    // optimista, hay que soltar la entidad tracked-y-sucia del ChangeTracker antes de reintentar
    // con una lectura fresca (DbContext.Entry ya lo resuelve, solo hace falta exponerlo acá).
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
