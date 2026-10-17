// מימוש IAuthService: הרשמה (אימייל כפול -> 409, Hash לסיסמה) והתחברות (פרטים שגויים -> 401, אחרת JWT).
// האימייל נשמר באותיות קטנות. בלוגים אין סיסמאות ואין tokens.
using AutoMapper;
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.DTOs.Users;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Security;
using BabyLink.Application.Interfaces.Services;
using BabyLink.Domain.Entities;
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

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
