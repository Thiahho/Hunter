namespace Hunter.Application.Common;

public interface ICurrentUserService
{
    int? UserId { get; }
    int? OrganizationId { get; }
    IReadOnlyCollection<string> Roles { get; }
}
