using Hunter.Application.Common;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

public class TagService(IHunterDbContext db, ICurrentUserService currentUser) : ITagService
{
    public async Task<IReadOnlyCollection<TagDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.Tags
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(ct);
    }

    public async Task<Result<TagDto>> CreateAsync(CreateTagRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<TagDto>.Failure("El nombre de la etiqueta es obligatorio.");

        var organizationId = currentUser.OrganizationId!.Value;
        var name = request.Name.Trim();

        if (await db.Tags.AnyAsync(t => t.OrganizationId == organizationId && t.Name == name, ct))
            return Result<TagDto>.Failure("Ya existe una etiqueta con ese nombre.");

        var tag = new Tag { OrganizationId = organizationId, Name = name, Color = request.Color };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);

        return Result<TagDto>.Success(new TagDto(tag.Id, tag.Name, tag.Color));
    }
}
