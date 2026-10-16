using System.Security.Claims;

namespace RaceDay.Extensions;

public static class ClaimsPrincipalExtensions
{

    /// Reads the UserID out of the session-derived claims for the current request.
    /// Throws if called on an unauthenticated principal — controllers using this
    /// should always be behind [Authorize].

    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim is null || !int.TryParse(idClaim, out var id))
        {
            throw new InvalidOperationException("No valid user ID claim found on the current principal.");
        }
        return id;
    }
}