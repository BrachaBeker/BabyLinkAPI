// מימוש IAuthService: הרשמה (אימייל כפול -> 409, Hash לסיסמה), התחברות (פרטים שגויים -> 401, אחרת JWT),
// ופרטי הפרופיל - צפייה ועדכון, רק של המשתמש עצמו.
// האימייל נשמר באותיות קטנות. בלוגים אין סיסמאות ואין tokens.
using AutoMapper;
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.DTOs.Users;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Security;
using BabyLink.Application.Interfaces.Services;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BabyLink.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IMapper mapper,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(dto.Email);
        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("כבר קיים משתמש עם האימייל הזה.");
        }

        var user = mapper.Map<User>(dto);
        user.Email = email;
        user.PasswordHash = passwordHasher.Hash(dto.Password);
        user.CreatedAtUtc = DateTime.UtcNow;
        SetAddress(user, dto.Street, dto.HouseNumber);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} registered as {Role}", user.Id, user.Role);
        return mapper.Map<UserDto>(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(dto.Email), cancellationToken);
        if (user is null || !passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            logger.LogInformation("Failed login attempt");
            throw new InvalidCredentialsException();
        }

        var (token, expiresAtUtc) = jwtTokenGenerator.GenerateToken(user);
        logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role
        };
    }

    public async Task<UserDto> GetUserAsync(int id, int currentUserId, CancellationToken cancellationToken)
    {
        if (id != currentUserId)
        {
            throw new ForbiddenException("אפשר לצפות רק בפרטים של המשתמש שלך.");
        }

        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"משתמש {id} לא נמצא.");

        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateUserAsync(int id, int currentUserId, UpdateUserDto dto, CancellationToken cancellationToken)
    {
        if (id != currentUserId)
        {
            throw new ForbiddenException("אפשר לעדכן רק את הפרטים של המשתמש שלך.");
        }

        var user = await userRepository.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException($"משתמש {id} לא נמצא.");

        if (user.Role == UserRole.Parent && (string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.HouseNumber)))
        {
            throw new BusinessRuleException("להורה חובה למלא כתובת מלאה: רחוב ומספר בית.");
        }

        user.FullName = dto.FullName.Trim();
        user.Phone = dto.Phone.Trim();
        user.City = dto.City.Trim();
        SetAddress(user, dto.Street, dto.HouseNumber);

        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated the profile", id);
        return mapper.Map<UserDto>(user);
    }

    // הכתובת נשמרת רק להורה (ממנה ממולאת מראש בקשה חדשה). לשמרטפית אין בה צורך - לא שומרים מידע מיותר.
    private static void SetAddress(User user, string? street, string? houseNumber)
    {
        var isParent = user.Role == UserRole.Parent;
        user.Street = isParent ? street?.Trim() : null;
        user.HouseNumber = isParent ? houseNumber?.Trim() : null;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
