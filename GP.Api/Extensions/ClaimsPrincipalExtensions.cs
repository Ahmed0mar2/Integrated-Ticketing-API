using System.Security.Claims;

namespace GP.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public const string DomainUserIdClaim = "domain_user_id";

    public static int? GetDomainUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(DomainUserIdClaim)?.Value
                    ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id) ? id : null;
    }

    public static int? GetApplicationUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}
