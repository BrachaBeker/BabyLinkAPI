// AutoMapper Profile: Create/Update DTO -> ServiceRequest, ServiceRequest -> List/Details DTO.
// שדות שה-Service קובע (בעלות, שיבוץ, Version, תאריכים, תגיות לפי TagIds) לא נלקחים מה-DTO.
using AutoMapper;
using BabyLink.Application.DTOs.ServiceRequests;
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Mapping;

public class ServiceRequestProfile : Profile
{
    public ServiceRequestProfile()
    {
        CreateMap<CreateServiceRequestDto, ServiceRequest>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ParentId, o => o.Ignore())
            .ForMember(d => d.Parent, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.AssignedBabysitterId, o => o.Ignore())
            .ForMember(d => d.AssignedBabysitter, o => o.Ignore())
            .ForMember(d => d.AssignedAtUtc, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.Ignore());

        CreateMap<UpdateServiceRequestDto, ServiceRequest>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ParentId, o => o.Ignore())
            .ForMember(d => d.Parent, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.AssignedBabysitterId, o => o.Ignore())
            .ForMember(d => d.AssignedBabysitter, o => o.Ignore())
            .ForMember(d => d.AssignedAtUtc, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.Ignore());

        CreateMap<ServiceRequest, ServiceRequestListItemDto>()
            .ForMember(d => d.TagNames, o => o.MapFrom(s => s.Tags.OrderBy(t => t.Name).Select(t => t.Name)))
            .ForMember(d => d.IsAssigned, o => o.MapFrom(s => s.AssignedBabysitterId != null))
            .ForMember(d => d.IsExpired, o => o.MapFrom(s => s.StartTimeUtc <= DateTime.UtcNow));

        // ParentFullName / AssignedBabysitterPhone וכו' ממופים אוטומטית (Flattening).
        CreateMap<ServiceRequest, ServiceRequestDetailsDto>()
            .ForMember(d => d.IsAssigned, o => o.MapFrom(s => s.AssignedBabysitterId != null))
            .ForMember(d => d.IsExpired, o => o.MapFrom(s => s.StartTimeUtc <= DateTime.UtcNow));
    }
}
