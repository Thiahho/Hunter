using Hunter.Application.Common;

namespace Hunter.Infrastructure.Persistence;

internal class DesignTimeCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
    public int? OrganizationId => null;
    public IReadOnlyCollection<string> Roles => [];
}
