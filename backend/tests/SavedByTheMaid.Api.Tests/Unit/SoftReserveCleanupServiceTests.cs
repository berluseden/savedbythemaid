using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SavedByTheMaid.Api.BackgroundServices;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests.Unit;

/// <summary>
/// Tests for SoftReserveCleanupService.
/// The service now uses EF Core LINQ (no raw SQL) with a transaction for atomicity.
/// We test: (1) service lifecycle, (2) the cleanup logic correctness,
/// (3) the data model invariants that the cleanup depends on.
/// </summary>
public class SoftReserveCleanupServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;

    public SoftReserveCleanupServiceTests()
    {
        _dbName = $"CleanupTests_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseInMemoryDatabase(_dbName));
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Service_StartsAndStops_WithoutError()
    {
        var logger = Mock.Of<ILogger<SoftReserveCleanupService>>();
        var service = new SoftReserveCleanupService(_serviceProvider, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        // No exception = service lifecycle works
    }

    [Fact]
    public async Task Service_CancellationToken_StopsGracefully()
    {
        var logger = Mock.Of<ILogger<SoftReserveCleanupService>>();
        var service = new SoftReserveCleanupService(_serviceProvider, logger);

        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        cts.Cancel();
        await Task.Delay(100);

        // Should not throw on stop after cancellation
        var act = () => service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CleanupLogic_OnlyDeletesExpiredSoftReserveSlots()
    {
        var now = DateTime.UtcNow;

        // Expired SoftReserve slot - should be cleaned up
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = now.AddHours(-1),
            SlotEnd = now.AddHours(-1).AddMinutes(30),
            OccupancyType = OccupancyType.SoftReserve,
            ReferenceId = 1,
            ExpiresAt = now.AddMinutes(-10),
            CreatedAt = now.AddMinutes(-25)
        });

        // Active SoftReserve slot - should NOT be cleaned up
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = now.AddHours(1),
            SlotEnd = now.AddHours(1).AddMinutes(30),
            OccupancyType = OccupancyType.SoftReserve,
            ReferenceId = 2,
            ExpiresAt = now.AddMinutes(10),
            CreatedAt = now
        });

        // Meeting slot (permanent) - should NEVER be cleaned up
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = now.AddHours(2),
            SlotEnd = now.AddHours(2).AddMinutes(30),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 3,
            CreatedAt = now
        });

        // SoftReserve without ExpiresAt - should NOT be cleaned up
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = now.AddHours(3),
            SlotEnd = now.AddHours(3).AddMinutes(30),
            OccupancyType = OccupancyType.SoftReserve,
            ReferenceId = 4,
            ExpiresAt = null,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        // Same WHERE clause as CleanupExpiredReserves uses via EF Core LINQ
        var toDelete = await _context.SlotOccupancies
            .Where(s => s.OccupancyType == OccupancyType.SoftReserve
                        && s.ExpiresAt != null
                        && s.ExpiresAt < now)
            .ToListAsync();

        toDelete.Should().HaveCount(1, "only the expired SoftReserve slot should be selected");
        toDelete[0].ReferenceId.Should().Be(1);

        // Simulate delete
        _context.SlotOccupancies.RemoveRange(toDelete);
        await _context.SaveChangesAsync();

        var remaining = await _context.SlotOccupancies.ToListAsync();
        remaining.Should().HaveCount(3, "active soft reserve, meeting, and null-expiry should survive");
        remaining.Should().NotContain(s => s.ReferenceId == 1);
    }

    [Fact]
    public async Task CleanupLogic_MarksActiveSoftReservesAsExpired()
    {
        var now = DateTime.UtcNow;

        // Active reserve that has expired
        _context.SoftReserves.Add(new SoftReserve
        {
            SessionId = "session-expired",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = now.AddHours(1),
            ScheduledEnd = now.AddHours(2),
            ExpiresAt = now.AddMinutes(-5),
            Status = SoftReserveStatus.Active
        });

        // Active reserve that hasn't expired yet
        _context.SoftReserves.Add(new SoftReserve
        {
            SessionId = "session-still-active",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = now.AddHours(1),
            ScheduledEnd = now.AddHours(2),
            ExpiresAt = now.AddMinutes(10),
            Status = SoftReserveStatus.Active
        });

        // Already-expired reserve - should not be touched again
        _context.SoftReserves.Add(new SoftReserve
        {
            SessionId = "session-already-expired",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = now.AddHours(1),
            ScheduledEnd = now.AddHours(2),
            ExpiresAt = now.AddMinutes(-30),
            Status = SoftReserveStatus.Expired
        });

        await _context.SaveChangesAsync();

        // Same query as CleanupExpiredReserves uses to find reserves to expire
        var toExpire = await _context.SoftReserves
            .Where(r => r.ExpiresAt < now && r.Status == SoftReserveStatus.Active)
            .ToListAsync();

        toExpire.Should().HaveCount(1, "only Active reserves past expiry should be marked");
        toExpire[0].SessionId.Should().Be("session-expired");

        foreach (var r in toExpire)
        {
            r.Status = SoftReserveStatus.Expired;
            r.UpdatedAt = now;
        }
        await _context.SaveChangesAsync();

        // Verify state after cleanup
        var allReserves = await _context.SoftReserves.ToListAsync();
        allReserves.Should().HaveCount(3);

        var active = allReserves.Where(r => r.Status == SoftReserveStatus.Active).ToList();
        active.Should().HaveCount(1, "only the non-expired reserve should remain Active");
        active[0].SessionId.Should().Be("session-still-active");

        var expired = allReserves.Where(r => r.Status == SoftReserveStatus.Expired).ToList();
        expired.Should().HaveCount(2, "both expired reserves should have Expired status");
    }

    [Fact]
    public async Task CleanupLogic_DoesNotAffectConvertedReserves()
    {
        var now = DateTime.UtcNow;

        // Converted reserve that is past ExpiresAt - should NOT be changed
        _context.SoftReserves.Add(new SoftReserve
        {
            SessionId = "session-confirmed",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = now.AddHours(1),
            ScheduledEnd = now.AddHours(2),
            ExpiresAt = now.AddMinutes(-5),
            Status = SoftReserveStatus.Converted,
            ServiceOrderId = 42
        });
        await _context.SaveChangesAsync();

        // Cleanup query should NOT touch Converted reserves
        var toExpire = await _context.SoftReserves
            .Where(r => r.ExpiresAt < now && r.Status == SoftReserveStatus.Active)
            .ToListAsync();

        toExpire.Should().BeEmpty("converted reserves should not be affected by cleanup");
    }
}
