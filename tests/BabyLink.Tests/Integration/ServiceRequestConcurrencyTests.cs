// בדיקת אינטגרציה - ההוכחה שה-Concurrency עובד: שני AppDbContext נפרדים מול אותו DB
// (SQLite in-memory עם חיבור פתוח ומשותף). הראשון שומר בהצלחה, השני נכשל על ה-Version,
// ובסוף בודקים מה באמת נשמר ב-DB.
using BabyLink.Application.Exceptions;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using BabyLink.Infrastructure.Persistence;
using BabyLink.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Tests.Integration;

public sealed class ServiceRequestConcurrencyTests : IAsyncLifetime
{
    // ה-DB in-memory חי כל עוד החיבור פתוח, ולכן כל ה-DbContext-ים משתמשים באותו חיבור.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    private int _requestId;
    private int _babysitterAId;
    private int _babysitterBId;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        await using var db = CreateContext();
        await db.Database.EnsureCreatedAsync();

        var parent = new User { Email = "parent@test.dev", PasswordHash = "x", FullName = "Parent", Phone = "1", City = "ירושלים", Role = UserRole.Parent, CreatedAtUtc = DateTime.UtcNow };
        var babysitterA = new User { Email = "a@test.dev", PasswordHash = "x", FullName = "Sitter A", Phone = "2", City = "ירושלים", Role = UserRole.Babysitter, CreatedAtUtc = DateTime.UtcNow };
        var babysitterB = new User { Email = "b@test.dev", PasswordHash = "x", FullName = "Sitter B", Phone = "3", City = "ירושלים", Role = UserRole.Babysitter, CreatedAtUtc = DateTime.UtcNow };
        db.Users.AddRange(parent, babysitterA, babysitterB);
        await db.SaveChangesAsync();

        var request = new ServiceRequest
        {
            ParentId = parent.Id,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            EndTimeUtc = DateTime.UtcNow.AddDays(1).AddHours(3),
            City = "ירושלים",
            Street = "הרצל",
            HouseNumber = "1",
            ChildrenCount = 2,
            HourlyRate = 50m,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync();

        _requestId = request.Id;
        _babysitterAId = babysitterA.Id;
        _babysitterBId = babysitterB.Id;
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task TwoContexts_AssignSameRequest_SecondSaveThrowsDbUpdateConcurrencyException()
    {
        await using var ctx1 = CreateContext();
        await using var ctx2 = CreateContext();

        // 1. שתי השמרטפיות טוענות את אותה בקשה, כשהיא עדיין פנויה (אותו Version).
        var requestInCtx1 = await ctx1.ServiceRequests.SingleAsync(r => r.Id == _requestId);
        var requestInCtx2 = await ctx2.ServiceRequests.SingleAsync(r => r.Id == _requestId);
        Assert.Null(requestInCtx1.AssignedBabysitterId);
        Assert.Null(requestInCtx2.AssignedBabysitterId);
        Assert.Equal(requestInCtx1.Version, requestInCtx2.Version);

        // 2. ctx1 משבץ את שמרטפית A ושומר - מצליח (וה-Version מתחלף).
        requestInCtx1.AssignedBabysitterId = _babysitterAId;
        requestInCtx1.AssignedAtUtc = DateTime.UtcNow;
        await ctx1.SaveChangesAsync();

        // 3. ctx2 משבץ את שמרטפית B ושומר - ה-UPDATE עם ה-Version הישן לא מוצא שורה.
        requestInCtx2.AssignedBabysitterId = _babysitterBId;
        requestInCtx2.AssignedAtUtc = DateTime.UtcNow;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx2.SaveChangesAsync());

        // 4. ב-DB נשמרה שמרטפית A בלבד.
        await using var verifyCtx = CreateContext();
        var stored = await verifyCtx.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == _requestId);
        Assert.Equal(_babysitterAId, stored.AssignedBabysitterId);
        Assert.Equal(requestInCtx1.Version, stored.Version);
    }

    [Fact]
    public async Task TwoContexts_ThroughRepository_SecondSaveThrowsConcurrencyConflictException()
    {
        // אותו תרחיש דרך ה-Repository: חריגת EF מתורגמת לחריגה של Application (ול-409 ב-API).
        await using var ctx1 = CreateContext();
        await using var ctx2 = CreateContext();
        var repository1 = new ServiceRequestRepository(ctx1);
        var repository2 = new ServiceRequestRepository(ctx2);

        var requestInCtx1 = (await repository1.GetForUpdateAsync(_requestId, CancellationToken.None))!;
        var requestInCtx2 = (await repository2.GetForUpdateAsync(_requestId, CancellationToken.None))!;

        requestInCtx1.AssignedBabysitterId = _babysitterAId;
        await repository1.SaveChangesAsync(CancellationToken.None);

        requestInCtx2.AssignedBabysitterId = _babysitterBId;
        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => repository2.SaveChangesAsync(CancellationToken.None));

        await using var verifyCtx = CreateContext();
        var stored = await verifyCtx.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == _requestId);
        Assert.Equal(_babysitterAId, stored.AssignedBabysitterId);
    }

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
}
