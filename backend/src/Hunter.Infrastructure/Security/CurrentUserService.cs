using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hunter.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Hunter.Infrastructure.Security;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public int? OrganizationId =>
        int.TryParse(Principal?.FindFirstValue("org_id"), out var id) ? id : null;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
}
