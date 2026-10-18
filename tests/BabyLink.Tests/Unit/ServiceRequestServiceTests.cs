// Unit Tests (xUnit + Moq) ל-ServiceRequestService: ה-Repositories מדומים, AutoMapper אמיתי עם ה-Profiles.
// כולל את המקרה התקין ואת המקרה הנדחה של המשאב המוגבל (תפיסת בקשה).
using AutoMapper;
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
    private readonly ServiceRequestService _service;

    public ServiceRequestServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(ServiceRequestProfile).Assembly),
            NullLoggerFactory.Instance).CreateMapper();

        _service = new ServiceRequestService(_repository.Object, _tagRepository.Object, mapper,
            NullLogger<ServiceRequestService>.Instance);
    }

    [Fact]
    public async Task AssignAsync_WhenAvailable_AssignsBabysitter()
    {
        var request = CreateRequest();
        SetupGetForUpdate(request);
        _repository.Setup(r => r.GetByIdAsync(RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var result = await _service.AssignAsync(RequestId, BabysitterId, CancellationToken.None);

        Assert.Equal(BabysitterId, request.AssignedBabysitterId);
        Assert.NotNull(request.AssignedAtUtc);
        Assert.True(result.IsAssigned);
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
    public async Task GetByIdAsync_WhenOtherBabysitter_HidesAssignedBabysitter()
    {
        SetupGetById(CreateAssignedRequestWithBabysitter());

        var result = await _service.GetByIdAsync(RequestId, OtherBabysitterId, CancellationToken.None);

        Assert.True(result.IsAssigned);
        Assert.Null(result.AssignedBabysitterId);
        Assert.Null(result.AssignedBabysitterFullName);
        Assert.Null(result.AssignedBabysitterPhone);
        Assert.Null(result.AssignedAtUtc);
    }

    [Theory]
    [InlineData(ParentId)]
    [InlineData(BabysitterId)]
    public async Task GetByIdAsync_WhenParentOrAssignedBabysitter_ShowsAssignedBabysitter(int currentUserId)
    {
        SetupGetById(CreateAssignedRequestWithBabysitter());

        var result = await _service.GetByIdAsync(RequestId, currentUserId, CancellationToken.None);

        Assert.Equal(BabysitterId, result.AssignedBabysitterId);
        Assert.Equal("Sitter", result.AssignedBabysitterFullName);
        Assert.Equal("050-0000000", result.AssignedBabysitterPhone);
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
