// ממשק ל-Hash ולאימות סיסמאות. המימוש ב-Infrastructure - כך קל לבדוק את ה-Service עם Moq.
namespace BabyLink.Application.Interfaces.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
