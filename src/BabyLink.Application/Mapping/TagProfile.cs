// AutoMapper Profile: Tag -> TagDto.
using AutoMapper;
using BabyLink.Application.DTOs.Tags;
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Mapping;

public class TagProfile : Profile
{
    public TagProfile()
    {
        CreateMap<Tag, TagDto>();
    }
}
