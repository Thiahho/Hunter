using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

public class MessageTemplateService(IHunterDbContext db, ICurrentUserService currentUser) : IMessageTemplateService
{
    public async Task<IReadOnlyCollection<MessageTemplateDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.MessageTemplates
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Name)
            .Select(t => ToDto(t))
            .ToListAsync(ct);
    }

    public async Task<Result<MessageTemplateDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var template = await db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        return template is null
            ? Result<MessageTemplateDto>.Failure("Plantilla no encontrada.")
            : Result<MessageTemplateDto>.Success(ToDto(template));
    }

    public async Task<Result<MessageTemplateDto>> CreateAsync(CreateMessageTemplateRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Content))
            return Result<MessageTemplateDto>.Failure("Nombre y contenido son obligatorios.");

        var organizationId = currentUser.OrganizationId!.Value;

        var template = new MessageTemplate
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Content = request.Content.Trim(),
            Channel = request.Channel,
            Version = 1,
            IsActive = true,
            CreatedBy = currentUser.UserId
        };

        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync(ct);

        return Result<MessageTemplateDto>.Success(ToDto(template));
    }

    public async Task<Result<MessageTemplateDto>> UpdateAsync(int id, UpdateMessageTemplateRequest request, CancellationToken ct = default)
    {
        var current = await db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (current is null)
            return Result<MessageTemplateDto>.Failure("Plantilla no encontrada.");

        // Cada edición crea una nueva versión en lugar de sobrescribir el contenido
        // histórico usado por campañas ya enviadas (doc 06, sección 16).
        current.IsActive = false;

        var next = new MessageTemplate
        {
            OrganizationId = current.OrganizationId,
            Name = current.Name,
            Content = request.Content.Trim(),
            Channel = current.Channel,
            Version = current.Version + 1,
            IsActive = true,
            CreatedBy = currentUser.UserId
        };

        db.MessageTemplates.Add(next);
        await db.SaveChangesAsync(ct);

        return Result<MessageTemplateDto>.Success(ToDto(next));
    }

    public async Task<Result<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var template = await db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is null)
            return Result<bool>.Failure("Plantilla no encontrada.");

        template.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> SetCatalogAsync(int id, CancellationToken ct = default)
    {
        var target = await db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (target is null)
            return Result<bool>.Failure("Plantilla no encontrada.");

        var previous = await db.MessageTemplates
            .Where(t => t.OrganizationId == target.OrganizationId && t.IsCatalogTemplate && t.Id != id)
            .ToListAsync(ct);
        foreach (var template in previous)
            template.IsCatalogTemplate = false;

        target.IsCatalogTemplate = true;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private static MessageTemplateDto ToDto(MessageTemplate t) => new(t.Id, t.Name, t.Content, t.Channel, t.Version, t.IsActive, t.IsCatalogTemplate);
}
