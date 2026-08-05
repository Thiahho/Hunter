using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hunter.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Hunter.Infrastructure.Security;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    // Fallback para trabajos en background (ej. CampaignQueueBackgroundService) que no corren
    // dentro de un request HTTP y por lo tanto no tienen claims de qué organización son: fluye
    // por AsyncLocal a través de los await del scope que arma UseOrganization.
    private static readonly AsyncLocal<int?> AmbientOrganizationId = new();

    public static IDisposable UseOrganization(int organizationId)
    {
        var previous = AmbientOrganizationId.Value;
        AmbientOrganizationId.Value = organizationId;
        return new OrganizationScope(previous);
    }

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public int? OrganizationId =>
        (int.TryParse(Principal?.FindFirstValue("org_id"), out var id) ? id : (int?)null)
        ?? AmbientOrganizationId.Value;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    private sealed class OrganizationScope(int? previous) : IDisposable
    {
        public void Dispose() => AmbientOrganizationId.Value = previous;
    }
}
