using Hunter.Application.Common;
using Hunter.Application.Compliance.Contracts;
using Hunter.Application.Prospecting;
using Hunter.Domain.Compliance;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Compliance;

public class SuppressionService(IHunterDbContext db, ICurrentUserService currentUser) : ISuppressionService
{
    public async Task<Result<SuppressionDto>> CreateAsync(CreateSuppressionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Contact))
            return Result<SuppressionDto>.Failure("El contacto es obligatorio.");

        var organizationId = currentUser.OrganizationId!.Value;
        var normalized = Normalize(request.ContactType, request.Contact);

        if (await db.Suppressions.AnyAsync(s => s.OrganizationId == organizationId && s.Contact == normalized, ct))
            return Result<SuppressionDto>.Failure("El contacto ya está en la lista de exclusión.");

        var suppression = new Suppression
        {
            OrganizationId = organizationId,
            Contact = normalized,
            ContactType = request.ContactType,
            Reason = request.Reason,
            Source = request.Source
        };

        db.Suppressions.Add(suppression);
        await db.SaveChangesAsync(ct);

        return Result<SuppressionDto>.Success(ToDto(suppression));
    }

    public async Task<IReadOnlyCollection<SuppressionDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.Suppressions
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SuppressionDto(s.Id, s.Contact, s.ContactType, s.Reason, s.Source, s.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<bool> IsSuppressedAsync(SuppressionContactType contactType, string rawContact, CancellationToken ct = default)
    {
        var organizationId = currentUser.OrganizationId!.Value;
        var normalized = Normalize(contactType, rawContact);

        return await db.Suppressions.AnyAsync(s => s.OrganizationId == organizationId && s.Contact == normalized, ct);
    }

    private static string Normalize(SuppressionContactType contactType, string rawContact)
    {
        var channel = contactType switch
        {
            SuppressionContactType.Phone => ProspectContactChannel.Phone,
            SuppressionContactType.Whatsapp => ProspectContactChannel.Whatsapp,
            SuppressionContactType.Email => ProspectContactChannel.Email,
            _ => ProspectContactChannel.Phone
        };

        return ContactValueNormalizer.Normalize(channel, rawContact);
    }

    private static SuppressionDto ToDto(Suppression s) => new(s.Id, s.Contact, s.ContactType, s.Reason, s.Source, s.CreatedAt);
}
