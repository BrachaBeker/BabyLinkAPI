// Unit Tests (xUnit + Moq) ל-ServiceRequestService: ה-Repositories מדומים, AutoMapper אמיתי עם ה-Profiles.
// כולל את המקרה התקין ואת המקרה הנדחה של המשאב המוגבל (תפיסת בקשה).
using AutoMapper;
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Mapping;
using BabyLink.Application.Services;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BabyLink.Tests.Unit;

public class ServiceRequestServiceTests
{
    private const int ParentId = 1;
    private const int BabysitterId = 10;
    private const int OtherBabysitterId = 20;
    private const int RequestId = 100;

    private readonly Mock<IServiceRequestRepository> _repository = new();
    private readonly Mock<ITagRepository> _tagRepository = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ServiceRequestService _service;

    public ServiceRequestServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(ServiceRequestProfile).Assembly),
            NullLoggerFactory.Instance).CreateMapper();

        _userRepository.Setup(u => u.GetByIdAsync(BabysitterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = BabysitterId, FullName = "Sitter", Role = UserRole.Babysitter });

        _service = new ServiceRequestService(_repository.Object, _tagRepository.Object, _notificationRepository.Object,
            _userRepository.Object, mapper, NullLogger<ServiceRequestService>.Instance);
    }

    [Fact]
    public async Task AssignAsync_WhenAvailable_AssignsBabysitterAndNotifiesParent()
    {
        var request = CreateRequest();
        SetupGetForUpdate(request);
        _repository.Setup(r => r.GetByIdAsync(RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var result = await _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None);

        Assert.Equal(BabysitterId, request.AssignedBabysitterId);
        Assert.NotNull(request.AssignedAtUtc);
        Assert.True(result.IsAssigned);
        // "נמצאה שמרטפית!" להורה, עם קישור לבקשה
        _notificationRepository.Verify(n => n.Add(It.Is<Notification>(x =>
            x.UserId == ParentId &&
            x.Type == NotificationType.RequestAssigned &&
            x.RequestId == RequestId &&
            x.ActorFullName == "Sitter")), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignAsync_WhenRepositoryThrowsConcurrencyConflict_PropagatesConflict()
    {
        // מישהי אחרת שמרה בין הקריאה לשמירה - ה-Repository מתרגם את חריגת ה-Version.
        SetupGetForUpdate(CreateRequest());
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None));
    }

    [Fact]
    public async Task AssignAsync_WhenAssignedToOtherBabysitter_ThrowsConflict()
    {
        SetupGetForUpdate(CreateRequest(assignedBabysitterId: OtherBabysitterId));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task AssignAsync_WhenAlreadyAssignedToSameBabysitter_Succeeds()
    {
        SetupGetForUpdate(CreateRequest(assignedBabysitterId: BabysitterId));

        var result = await _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None);

        Assert.Equal(BabysitterId, result.AssignedBabysitterId);
        VerifyNotSaved();
        // לחיצה כפולה לא שולחת להורה הודעה נוספת
        _notificationRepository.Verify(n => n.Add(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task AssignAsync_WhenRequestNotFound_ThrowsNotFound()
    {
        _repository.Setup(r => r.GetForUpdateAsync(RequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceRequest?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None));
    }

    [Fact]
    public async Task AssignAsync_WhenStartTimePassed_ThrowsBusinessRule()
    {
        SetupGetForUpdate(CreateRequest(startUtc: DateTime.UtcNow.AddHours(-1)));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task AssignAsync_WhenBabysitterHasOverlappingAssignment_ThrowsConflict()
    {
        SetupGetForUpdate(CreateRequest());
        _repository.Setup(r => r.HasOverlappingAssignmentAsync(BabysitterId, It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                RequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task UpdateAsync_WhenNotOwner_ThrowsForbidden()
    {
        SetupGetForUpdate(CreateRequest());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateAsync(RequestId, CreateUpdateDto(), parentId: 999, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task UpdateAsync_WhenAlreadyAssigned_ThrowsConflict()
    {
        SetupGetForUpdate(CreateRequest(assignedBabysitterId: BabysitterId));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(RequestId, CreateUpdateDto(), ParentId, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task GetByIdAsync_WhenOtherBabysitter_HidesIdentityButKeepsAssignedTime()
    {
        SetupGetById(CreateAssignedRequestWithBabysitter());

        var result = await _service.GetByIdAsync(RequestId, new CurrentUser(OtherBabysitterId, UserRole.Babysitter), CancellationToken.None);

        Assert.True(result.IsAssigned);
        Assert.Null(result.AssignedBabysitterId);
        Assert.Null(result.AssignedBabysitterFullName);
        Assert.Null(result.AssignedBabysitterPhone);
        Assert.NotNull(result.AssignedAtUtc);
    }

    [Theory]
    [InlineData(ParentId, UserRole.Parent)]
    [InlineData(BabysitterId, UserRole.Babysitter)]
    public async Task GetByIdAsync_WhenParentOrAssignedBabysitter_ShowsAssignedBabysitter(int currentUserId, UserRole role)
    {
        SetupGetById(CreateAssignedRequestWithBabysitter());

        var result = await _service.GetByIdAsync(RequestId, new CurrentUser(currentUserId, role), CancellationToken.None);

        Assert.Equal(BabysitterId, result.AssignedBabysitterId);
        Assert.Equal("Sitter", result.AssignedBabysitterFullName);
        Assert.Equal("050-0000000", result.AssignedBabysitterPhone);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOtherParent_ThrowsForbidden()
    {
        // הורה רואה רק בקשות שהוא פרסם.
        SetupGetById(CreateRequest());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.GetByIdAsync(RequestId, new CurrentUser(999, UserRole.Parent), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenAssigned_NotifiesBabysitter()
    {
        var request = CreateRequest(assignedBabysitterId: BabysitterId);
        SetupGetForUpdate(request);

        await _service.DeleteAsync(RequestId, ParentId, CancellationToken.None);

        _notificationRepository.Verify(n => n.Add(It.Is<Notification>(x =>
            x.UserId == BabysitterId &&
            x.Type == NotificationType.RequestCancelled &&
            x.RequestStartTimeUtc == request.StartTimeUtc &&
            x.RequestId == null &&
            x.ActorFullName == "Parent")), Times.Once);
        _repository.Verify(r => r.Remove(request), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotAssigned_DoesNotNotify()
    {
        SetupGetForUpdate(CreateRequest());

        await _service.DeleteAsync(RequestId, ParentId, CancellationToken.None);

        _notificationRepository.Verify(n => n.Add(It.IsAny<Notification>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- ביטול שיבוץ ע"י השמרטפית ----------

    [Fact]
    public async Task UnassignAsync_WhenMine_ClearsAssignmentAndNotifiesParent()
    {
        var request = CreateAssignedRequestWithBabysitter();
        SetupGetForUpdate(request);

        await _service.UnassignAsync(RequestId, BabysitterId, CancellationToken.None);

        Assert.Null(request.AssignedBabysitterId);
        Assert.Null(request.AssignedAtUtc);
        _notificationRepository.Verify(n => n.Add(It.Is<Notification>(x =>
            x.UserId == ParentId &&
            x.Type == NotificationType.AssignmentCancelled &&
            x.RequestId == RequestId &&
            x.ActorFullName == "Sitter")), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnassignAsync_WhenAssignedToOtherBabysitter_ThrowsForbidden()
    {
        SetupGetForUpdate(CreateRequest(assignedBabysitterId: OtherBabysitterId));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UnassignAsync(RequestId, BabysitterId, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task UnassignAsync_WhenNotAssigned_ThrowsConflict()
    {
        // למשל לחיצה כפולה על "ביטול השיבוץ", או שההורה ביטל בינתיים
        SetupGetForUpdate(CreateRequest());

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UnassignAsync(RequestId, BabysitterId, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task UnassignAsync_WhenStartTimePassed_ThrowsBusinessRule()
    {
        SetupGetForUpdate(CreateRequest(assignedBabysitterId: BabysitterId, startUtc: DateTime.UtcNow.AddHours(-1)));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.UnassignAsync(RequestId, BabysitterId, CancellationToken.None));

        VerifyNotSaved();
    }

    [Fact]
    public async Task UnassignAsync_WhenRepositoryThrowsConcurrencyConflict_PropagatesConflict()
    {
        // מישהו שינה את הבקשה בין הקריאה לשמירה (למשל ההורה ביטל אותה באותו רגע) - 409.
        SetupGetForUpdate(CreateAssignedRequestWithBabysitter());
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _service.UnassignAsync(RequestId, BabysitterId, CancellationToken.None));
    }

    private static ServiceRequest CreateAssignedRequestWithBabysitter()
    {
        var request = CreateRequest(assignedBabysitterId: BabysitterId);
        request.AssignedBabysitter = new User { Id = BabysitterId, FullName = "Sitter", Phone = "050-0000000", Role = UserRole.Babysitter };
        return request;
    }

    private void SetupGetById(ServiceRequest request) =>
        _repository.Setup(r => r.GetByIdAsync(RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

    private void SetupGetForUpdate(ServiceRequest request) =>
        _repository.Setup(r => r.GetForUpdateAsync(RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

    private void VerifyNotSaved() =>
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    private static ServiceRequest CreateRequest(int? assignedBabysitterId = null, DateTime? startUtc = null)
    {
        var start = startUtc ?? DateTime.UtcNow.AddDays(1);
        return new ServiceRequest
        {
            Id = RequestId,
            ParentId = ParentId,
            Parent = new User { Id = ParentId, FullName = "Parent", Role = UserRole.Parent },
            StartTimeUtc = start,
            EndTimeUtc = start.AddHours(3),
            City = "ירושלים",
            Street = "הרצל",
            HouseNumber = "1",
            ChildrenCount = 2,
            HourlyRate = 50m,
            AssignedBabysitterId = assignedBabysitterId,
            AssignedAtUtc = assignedBabysitterId is null ? null : DateTime.UtcNow,
            Version = Guid.NewGuid()
        };
    }

    private static UpdateServiceRequestDto CreateUpdateDto() => new()
    {
        StartTimeUtc = DateTime.UtcNow.AddDays(2),
        EndTimeUtc = DateTime.UtcNow.AddDays(2).AddHours(3),
        City = "ירושלים",
        Street = "יפו",
        HouseNumber = "5",
        ChildrenCount = 1,
        HourlyRate = 60m
    };
}
