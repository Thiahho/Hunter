using Hunter.Application.Common;

namespace Hunter.Tests.TestSupport;

public class FakeCurrentUserService : ICurrentUserService
{
    public int? UserId { get; set; }
    public int? OrganizationId { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
