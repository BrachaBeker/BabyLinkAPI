// Unit Tests לוולידציה של ההרשמה: כתובת חובה להורה בלבד (IValidatableObject).
using System.ComponentModel.DataAnnotations;
using BabyLink.Application.DTOs.Auth;
using BabyLink.Domain.Enums;

namespace BabyLink.Tests.Unit;

public class RegisterRequestDtoTests
{
    [Fact]
    public void Validate_WhenParentWithoutAddress_ReturnsStreetAndHouseNumberErrors()
    {
        var results = Validate(CreateDto(UserRole.Parent, street: null, houseNumber: " "));

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterRequestDto.Street)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterRequestDto.HouseNumber)));
    }

    [Fact]
    public void Validate_WhenParentWithAddress_IsValid()
    {
        Assert.Empty(Validate(CreateDto(UserRole.Parent, street: "הרצל", houseNumber: "12")));
    }

    [Fact]
    public void Validate_WhenBabysitterWithoutAddress_IsValid()
    {
        Assert.Empty(Validate(CreateDto(UserRole.Babysitter, street: null, houseNumber: null)));
    }

    private static List<ValidationResult> Validate(RegisterRequestDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    private static RegisterRequestDto CreateDto(UserRole role, string? street, string? houseNumber) => new()
    {
        Email = "user@babylink.dev",
        Password = "Demo123!",
        FullName = "משתמש בדיקה",
        Phone = "050-1234567",
        City = "ירושלים",
        Street = street,
        HouseNumber = houseNumber,
        Role = role
    };
}
