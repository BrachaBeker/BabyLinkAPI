// GetUserId: שליפת ה-UserId מה-Claim "sub" של ה-JWT. GetCurrentUser: המזהה והתפקיד יחד.
// ה-UserId והתפקיד תמיד מגיעים מה-token ולא מה-Body, כדי שאי אפשר יהיה להתחזות.
using System.Security.Claims;
using BabyLink.Application.DTOs.Common;
using BabyLink.Domain.Enums;
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

    // IsInRole משתמש באותו Role Claim ש-[Authorize(Roles = ...)] בודק.
    public static CurrentUser GetCurrentUser(this ClaimsPrincipal user) =>
        new(user.GetUserId(), user.IsInRole(nameof(UserRole.Parent)) ? UserRole.Parent : UserRole.Babysitter);
}
