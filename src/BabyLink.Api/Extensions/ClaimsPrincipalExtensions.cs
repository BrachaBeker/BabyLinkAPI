// GetUserId: שליפת ה-UserId מה-Claim "sub" של ה-JWT.
// ה-UserId תמיד מגיע מה-token ולא מה-Body, כדי שאי אפשר יהיה להתחזות.
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BabyLink.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The authenticated user has no valid 'sub' claim.");
    }
}
