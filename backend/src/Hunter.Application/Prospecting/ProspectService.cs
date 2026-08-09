using Hunter.Application.Common;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

public class ProspectService(IHunterDbContext db, ICurrentUserService currentUser, IProspectDuplicateFinder duplicateFinder) : IProspectService
{
    public async Task<Result<ProspectDto>> CreateAsync(CreateProspectRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName))
            return Result<ProspectDto>.Failure("El nombre del negocio es obligatorio.");

        if (request.Contacts.Count == 0)
            return Result<ProspectDto>.Failure("El prospecto debe tener al menos un contacto.");

        var organizationId = currentUser.OrganizationId!.Value;

        var normalizedContacts = request.Contacts
            .Select(c => new ContactInput(c.Channel, ContactValueNormalizer.Normalize(c.Channel, c.Value), c.IsPrimary))
            .ToList();

        if (await duplicateFinder.FindDuplicateProspectIdAsync(organizationId, normalizedContacts, request.BusinessName, request.City, ct) is { } existingId)
            return Result<ProspectDto>.Failure($"El prospecto ya existe (contacto duplicado). ProspectId: {existingId}");

        var prospect = new Prospect
        {
            OrganizationId = organizationId,
            BusinessName = request.BusinessName.Trim(),
            ContactName = request.ContactName?.Trim(),
            Category = request.Category,
            BusinessSize = request.BusinessSize,
            RecurrencePotential = request.RecurrencePotential,
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            Province = request.Province?.Trim(),
            Country = request.Country?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Website = request.Website?.Trim()
        };

        foreach (var contact in normalizedContacts)
        {
            prospect.Contacts.Add(new ProspectContact
            {
                OrganizationId = organizationId,
                ProspectId = prospect.Id,
                Prospect = prospect,
                Channel = contact.Channel,
                Value = contact.Value,
                IsPrimary = contact.IsPrimary
            });
        }

        prospect.Sources.Add(new ProspectSource
        {
            OrganizationId = organizationId,
            ProspectId = prospect.Id,
            Prospect = prospect,
            SourceType = request.SourceType,
            ExternalId = request.SourceExternalId,
            SourceUrl = request.SourceUrl
        });

        if (request.Tags is { Count: > 0 })
            await AttachTagsAsync(prospect, request.Tags, organizationId, ct);

        db.Prospects.Add(prospect);
        await db.SaveChangesAsync(ct);

        return Result<ProspectDto>.Success(await LoadDtoAsync(prospect.Id, ct) ?? throw new InvalidOperationException());
    }

    public async Task<Result<ProspectDto>> UpdateAsync(int id, UpdateProspectRequest request, CancellationToken ct = default)
    {
        var prospect = await db.Prospects.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (prospect is null)
            return Result<ProspectDto>.Failure("Prospecto no encontrado.");

        prospect.BusinessName = request.BusinessName.Trim();
        prospect.ContactName = request.ContactName?.Trim();
        prospect.Category = request.Category;
        prospect.BusinessSize = request.BusinessSize;
        prospect.RecurrencePotential = request.RecurrencePotential;
        prospect.Address = request.Address?.Trim();
        prospect.City = request.City?.Trim();
        prospect.Province = request.Province?.Trim();
        prospect.Country = request.Country?.Trim();
        prospect.PostalCode = request.PostalCode?.Trim();
        prospect.Website = request.Website?.Trim();
        prospect.Status = request.Status;
        prospect.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<ProspectDto>.Success(await LoadDtoAsync(id, ct) ?? throw new InvalidOperationException());
    }

    public async Task<Result<ProspectDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dto = await LoadDtoAsync(id, ct);
        return dto is null
            ? Result<ProspectDto>.Failure("Prospecto no encontrado.")
            : Result<ProspectDto>.Success(dto);
    }

    public async Task<PagedResult<ProspectListItemDto>> SearchAsync(ProspectQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var prospects = db.Prospects.Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            prospects = prospects.Where(p =>
                p.BusinessName.ToLower().Contains(search)
                || (p.ContactName != null && p.ContactName.ToLower().Contains(search))
                || (p.Address != null && p.Address.ToLower().Contains(search))
                || (p.City != null && p.City.ToLower().Contains(search))
                || (p.Province != null && p.Province.ToLower().Contains(search))
                || p.Contacts.Any(c => c.Value.ToLower().Contains(search)));
        }

        if (query.Category is not null)
            prospects = prospects.Where(p => p.Category == query.Category);

        if (!string.IsNullOrWhiteSpace(query.City))
            prospects = prospects.Where(p => p.City == query.City);

        if (!string.IsNullOrWhiteSpace(query.Province))
            prospects = prospects.Where(p => p.Province == query.Province);

        if (query.Status is not null)
            prospects = prospects.Where(p => p.Status == query.Status);

        if (query.BusinessSize is not null)
            prospects = prospects.Where(p => p.BusinessSize == query.BusinessSize);

        if (!string.IsNullOrWhiteSpace(query.Tag))
            prospects = prospects.Where(p => p.ProspectTags.Any(pt => pt.Tag.Name == query.Tag));

        if (query.Source is not null)
            prospects = prospects.Where(p => p.Sources.Any(s => s.SourceType == query.Source));

        var totalItems = await prospects.CountAsync(ct);

        var items = await prospects
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProspectListItemDto(
                p.Id,
                p.BusinessName,
                p.Category,
                p.Address,
                p.City,
                p.Province,
                p.Latitude,
                p.Longitude,
                p.Status,
                p.CommercialScore,
                p.OperationalPriority,
                p.Contacts.Where(c => c.IsPrimary).Select(c => c.Value).FirstOrDefault()
                    ?? p.Contacts.Select(c => c.Value).FirstOrDefault(),
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProspectListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var prospect = await db.Prospects.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (prospect is null)
            return Result<bool>.Failure("Prospecto no encontrado.");

        prospect.IsDeleted = true;
        prospect.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AddTagAsync(int prospectId, string tagName, CancellationToken ct = default)
    {
        var prospect = await db.Prospects
            .Include(p => p.ProspectTags)
            .FirstOrDefaultAsync(p => p.Id == prospectId && !p.IsDeleted, ct);

        if (prospect is null)
            return Result<bool>.Failure("Prospecto no encontrado.");

        await AttachTagsAsync(prospect, [tagName], currentUser.OrganizationId!.Value, ct);
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RemoveTagAsync(int prospectId, int tagId, CancellationToken ct = default)
    {
        var link = await db.ProspectTags.FirstOrDefaultAsync(pt => pt.ProspectId == prospectId && pt.TagId == tagId, ct);
        if (link is null)
            return Result<bool>.Failure("La etiqueta no está asociada a este prospecto.");

        db.ProspectTags.Remove(link);
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<ProspectContactDto>> AddContactAsync(int prospectId, AddProspectContactRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return Result<ProspectContactDto>.Failure("El valor de contacto es obligatorio.");

        var prospect = await db.Prospects
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == prospectId && !p.IsDeleted, ct);
        if (prospect is null)
            return Result<ProspectContactDto>.Failure("Prospecto no encontrado.");

        var normalizedValue = ContactValueNormalizer.Normalize(request.Channel, request.Value);

        if (request.IsPrimary)
            foreach (var existing in prospect.Contacts)
                existing.IsPrimary = false;

        var contact = new ProspectContact
        {
            OrganizationId = prospect.OrganizationId,
            ProspectId = prospect.Id,
            Prospect = prospect,
            Channel = request.Channel,
            Value = normalizedValue,
            IsPrimary = request.IsPrimary
        };

        db.ProspectContacts.Add(contact);
        await db.SaveChangesAsync(ct);

        return Result<ProspectContactDto>.Success(new ProspectContactDto(contact.Id, contact.Channel, contact.Value, contact.IsPrimary, contact.IsVerified));
    }

    public async Task<Result<ProspectContactDto>> UpdateContactAsync(int prospectId, int contactId, UpdateProspectContactRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return Result<ProspectContactDto>.Failure("El valor de contacto es obligatorio.");

        var prospect = await db.Prospects
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == prospectId && !p.IsDeleted, ct);
        if (prospect is null)
            return Result<ProspectContactDto>.Failure("Prospecto no encontrado.");

        var contact = prospect.Contacts.FirstOrDefault(c => c.Id == contactId);
        if (contact is null)
            return Result<ProspectContactDto>.Failure("Contacto no encontrado.");

        if (request.IsPrimary)
            foreach (var other in prospect.Contacts.Where(c => c.Id != contactId))
                other.IsPrimary = false;

        contact.Channel = request.Channel;
        contact.Value = ContactValueNormalizer.Normalize(request.Channel, request.Value);
        contact.IsPrimary = request.IsPrimary;

        await db.SaveChangesAsync(ct);

        return Result<ProspectContactDto>.Success(new ProspectContactDto(contact.Id, contact.Channel, contact.Value, contact.IsPrimary, contact.IsVerified));
    }

    public async Task<Result<bool>> RemoveContactAsync(int prospectId, int contactId, CancellationToken ct = default)
    {
        var prospect = await db.Prospects
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == prospectId && !p.IsDeleted, ct);
        if (prospect is null)
            return Result<bool>.Failure("Prospecto no encontrado.");

        var contact = prospect.Contacts.FirstOrDefault(c => c.Id == contactId);
        if (contact is null)
            return Result<bool>.Failure("Contacto no encontrado.");

        if (prospect.Contacts.Count <= 1)
            return Result<bool>.Failure("El prospecto debe tener al menos un contacto.");

        db.ProspectContacts.Remove(contact);

        if (contact.IsPrimary)
        {
            var next = prospect.Contacts.FirstOrDefault(c => c.Id != contactId);
            if (next is not null)
                next.IsPrimary = true;
        }

        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private async Task AttachTagsAsync(Prospect prospect, IReadOnlyCollection<string> tagNames, int organizationId, CancellationToken ct)
    {
        foreach (var rawName in tagNames)
        {
            var name = rawName.Trim();
            if (name.Length == 0)
                continue;

            var tag = await db.Tags.FirstOrDefaultAsync(t => t.OrganizationId == organizationId && t.Name == name, ct);
            if (tag is null)
            {
                tag = new Tag { OrganizationId = organizationId, Name = name };
                db.Tags.Add(tag);
            }

            if (prospect.ProspectTags.All(pt => pt.TagId != tag.Id))
                prospect.ProspectTags.Add(new ProspectTag { ProspectId = prospect.Id, Prospect = prospect, TagId = tag.Id, Tag = tag });
        }
    }

    private async Task<ProspectDto?> LoadDtoAsync(int id, CancellationToken ct)
    {
        return await db.Prospects
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new ProspectDto(
                p.Id,
                p.BusinessName,
                p.ContactName,
                p.Category,
                p.BusinessSize,
                p.RecurrencePotential,
                p.Address,
                p.City,
                p.Province,
                p.Country,
                p.PostalCode,
                p.Latitude,
                p.Longitude,
                p.Website,
                p.CommercialScore,
                p.OperationalPriority,
                p.Status,
                p.CreatedAt,
                p.LastContactedAt,
                p.Contacts.Select(c => new ProspectContactDto(c.Id, c.Channel, c.Value, c.IsPrimary, c.IsVerified)).ToList(),
                p.Sources.Select(s => new ProspectSourceDto(s.Id, s.SourceType, s.ExternalId, s.SourceUrl, s.CollectedAt)).ToList(),
                p.ProspectTags.Select(pt => pt.Tag.Name).ToList()))
            .FirstOrDefaultAsync(ct);
    }
}
