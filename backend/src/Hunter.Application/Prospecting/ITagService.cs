using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

public interface ITagService
{
    Task<IReadOnlyCollection<TagDto>> ListAsync(CancellationToken ct = default);
    Task<Result<TagDto>> CreateAsync(CreateTagRequest request, CancellationToken ct = default);
}
