// Unit Tests ל-AuthService: אימייל קיים -> ConflictException, סיסמה שגויה -> InvalidCredentialsException,
// התחברות תקינה -> מוחזר token.
using AutoMapper;
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Security;
using BabyLink.Application.Mapping;
using BabyLink.Application.Services;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BabyLink.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(UserProfile).Assembly),
            NullLoggerFactory.Instance).CreateMapper();

        _service = new AuthService(_userRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object, mapper,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ThrowsConflict()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("mom@test.dev", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var dto = new RegisterRequestDto
        {
            Email = "Mom@Test.dev",
            Password = "Secret123!",
            FullName = "Mom",
            Phone = "050-0000000",
            City = "ירושלים",
            Role = UserRole.Parent
        };

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(dto, CancellationToken.None));
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsWrong_ThrowsInvalidCredentials()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("sitter@test.dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());
        _passwordHasher.Setup(h => h.Verify("wrong", "HASH")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequestDto { Email = "sitter@test.dev", Password = "wrong" }, CancellationToken.None));

        _jwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsToken()
    {
        var user = CreateUser();
        var expires = DateTime.UtcNow.AddHours(2);
        _userRepository.Setup(r => r.GetByEmailAsync("sitter@test.dev", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Demo123!", "HASH")).Returns(true);
        _jwtTokenGenerator.Setup(g => g.GenerateToken(user)).Returns(("TOKEN", expires));

        var result = await _service.LoginAsync(
            new LoginRequestDto { Email = " Sitter@Test.dev ", Password = "Demo123!" }, CancellationToken.None);

        Assert.Equal("TOKEN", result.Token);
        Assert.Equal(expires, result.ExpiresAtUtc);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(UserRole.Babysitter, result.Role);
    }

    private static User CreateUser() => new()
    {
        Id = 7,
        Email = "sitter@test.dev",
        PasswordHash = "HASH",
        FullName = "Sitter",
        Role = UserRole.Babysitter
    };
}
