// ממשק ליצירת JWT. המימוש ב-Infrastructure (שם נמצאות חבילות ה-JWT).
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Interfaces.Security;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
