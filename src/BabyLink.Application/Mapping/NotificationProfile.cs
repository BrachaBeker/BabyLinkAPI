// AutoMapper Profile: Notification -> NotificationDto (שמות זהים).
using AutoMapper;
using BabyLink.Application.DTOs.Notifications;
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Mapping;

public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, NotificationDto>();
    }
}
