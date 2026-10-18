// ממשק שירות הרשמה והתחברות.
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.DTOs.Users;

namespace BabyLink.Application.Interfaces.Services;

public interface IAuthService
{
    // אימייל קיים -> ConflictException (409).
    Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken);

    // פרטים שגויים -> InvalidCredentialsException (401).
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken);

    // פרטי משתמש - רק המשתמש עצמו (אחרת 403). לא קיים -> 404.
    Task<UserDto> GetUserAsync(int id, int currentUserId, CancellationToken cancellationToken);

    // עדכון הפרופיל - רק המשתמש עצמו (אחרת 403). להורה חובה כתובת מלאה (אחרת 400).
    Task<UserDto> UpdateUserAsync(int id, int currentUserId, UpdateUserDto dto, CancellationToken cancellationToken);
}
