// מימוש IPasswordHasher - עוטף את PasswordHasher<User> של ASP.NET Core Identity (PBKDF2 + Salt).
using BabyLink.Domain.Entities;
using IPasswordHasher = BabyLink.Application.Interfaces.Security.IPasswordHasher;
using IdentityPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<BabyLink.Domain.Entities.User>;
using PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace BabyLink.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private readonly IdentityPasswordHasher _hasher = new();

    // ה-User לא משמש באלגוריתם של Identity, ולכן מועבר מופע ריק.
    private static readonly User NoUser = new();

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(NoUser, passwordHash, password) != PasswordVerificationResult.Failed;
}
