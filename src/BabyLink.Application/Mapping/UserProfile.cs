// AutoMapper Profile: User -> UserDto, RegisterRequestDto -> User (PasswordHash נקבע ב-Service).
using AutoMapper;
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.DTOs.Users;
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<RegisterRequestDto, User>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedRequests, o => o.Ignore())
            .ForMember(d => d.AssignedRequests, o => o.Ignore());
    }
}
