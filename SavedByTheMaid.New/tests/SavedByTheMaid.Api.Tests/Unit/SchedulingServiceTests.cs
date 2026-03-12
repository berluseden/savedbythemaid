using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests.Unit;

/// <summary>
/// Unit tests for SchedulingService - conflict detection and slot management
/// </summary>
public class SchedulingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SchedulingService _sut;

    public SchedulingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SchedulingTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var logger = Mock.Of<ILogger<SchedulingService>>();
        _sut = new SchedulingService(_context, logger);

        SeedEmployee();
    }

    private void SeedEmployee()
    {
        _context.Employees.Add(new Employee
        {
            Id = 1,
            FirstName = "Maria",
            LastName = "Garcia",
            Email = "maria@test.com",
            IsActive = true,
            PrimaryServiceAreaId = 1
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CheckConflictsAsync

    [Fact]
    public async Task CheckConflicts_NoConflicts_ReturnsNull()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var end = start.AddHours(2);

        var result = await _sut.CheckConflictsAsync(1, start, end);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckConflicts_NonExistentEmployee_ReturnsConflict()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(2);

        var result = await _sut.CheckConflictsAsync(999, start, end);

        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.EmployeeUnavailable);
    }

    [Fact]
    public async Task CheckConflicts_InactiveEmployee_ReturnsConflict()
    {
        _context.Employees.Add(new Employee
        {
            Id = 2,
            FirstName = "Inactive",
            LastName = "Worker",
            Email = "inactive@test.com",
            IsActive = false
        });
        await _context.SaveChangesAsync();

        var result = await _sut.CheckConflictsAsync(2, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2));

        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.EmployeeUnavailable);
        result.Message.Should().Contain("inactivo");
    }

    [Fact]
    public async Task CheckConflicts_ExistingSlotOccupancy_ReturnsConflict()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Add existing slot
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = start,
            SlotEnd = start.AddMinutes(30),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 100,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.CheckConflictsAsync(1, start, start.AddHours(1));

        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.ExistingBooking);
    }

    [Fact]
    public async Task CheckConflicts_ExcludedMeeting_IgnoresConflict()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = start,
            SlotEnd = start.AddMinutes(30),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 100,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Exclude the same meeting (reschedule scenario)
        var result = await _sut.CheckConflictsAsync(1, start, start.AddHours(1), excludeMeetingId: 100);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckConflicts_ApprovedTimeOff_ReturnsConflict()
    {
        var start = DateTime.UtcNow.AddDays(5).Date.AddHours(10);

        _context.EmployeeTimeOffs.Add(new EmployeeTimeOff
        {
            EmployeeId = 1,
            StartDateTime = start.AddHours(-1),
            EndDateTime = start.AddHours(8),
            Type = TimeOffType.Vacation,
            Status = TimeOffStatus.Approved,
            Reason = "Family vacation"
        });
        await _context.SaveChangesAsync();

        var result = await _sut.CheckConflictsAsync(1, start, start.AddHours(2));

        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.TimeOff);
        result.Message.Should().Contain("vacaciones");
    }

    [Fact]
    public async Task CheckConflicts_PendingTimeOff_NoConflict()
    {
        var start = DateTime.UtcNow.AddDays(5).Date.AddHours(10);

        _context.EmployeeTimeOffs.Add(new EmployeeTimeOff
        {
            EmployeeId = 1,
            StartDateTime = start.AddHours(-1),
            EndDateTime = start.AddHours(8),
            Type = TimeOffType.Personal,
            Status = TimeOffStatus.Pending,
            Reason = "Pending request"
        });
        await _context.SaveChangesAsync();

        var result = await _sut.CheckConflictsAsync(1, start, start.AddHours(2));

        // Pending time off should NOT cause a conflict
        result.Should().BeNull();
    }

    #endregion

    #region AcquireSlotsAsync

    [Fact]
    public async Task AcquireSlots_CreatesCorrectNumberOfSlots()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10); // 10:00
        var end = start.AddHours(2); // 12:00

        await _sut.AcquireSlotsAsync(1, start, end, OccupancyType.Meeting, 42);
        await _context.SaveChangesAsync();

        var slots = await _context.SlotOccupancies.Where(s => s.ReferenceId == 42).ToListAsync();
        slots.Should().HaveCount(4); // 10:00, 10:30, 11:00, 11:30 = 4 slots
        slots.Should().AllSatisfy(s =>
        {
            s.EmployeeId.Should().Be(1);
            s.OccupancyType.Should().Be(OccupancyType.Meeting);
        });
    }

    [Fact]
    public async Task AcquireSlots_SoftReserve_SetsExpiresAt()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var end = start.AddMinutes(60);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        await _sut.AcquireSlotsAsync(1, start, end, OccupancyType.SoftReserve, 99, expiresAt);
        await _context.SaveChangesAsync();

        var slots = await _context.SlotOccupancies.Where(s => s.ReferenceId == 99).ToListAsync();
        slots.Should().AllSatisfy(s =>
        {
            s.OccupancyType.Should().Be(OccupancyType.SoftReserve);
            s.ExpiresAt.Should().Be(expiresAt);
        });
    }

    [Fact]
    public async Task AcquireSlots_NormalizesStartTime()
    {
        // Start at 10:17 should normalize to 10:00
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10).AddMinutes(17);
        var end = start.AddHours(1);

        await _sut.AcquireSlotsAsync(1, start, end, OccupancyType.Meeting, 50);
        await _context.SaveChangesAsync();

        var slots = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == 50)
            .OrderBy(s => s.SlotStart)
            .ToListAsync();

        slots.First().SlotStart.Minute.Should().Be(0); // Normalized from 10:17 to 10:00
    }

    #endregion

    #region ReleaseSlotsAsync

    [Fact]
    public async Task ReleaseSlots_RemovesAllSlotsForReference()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        await _sut.AcquireSlotsAsync(1, start, start.AddHours(1), OccupancyType.SoftReserve, 77);
        await _context.SaveChangesAsync();

        var countBefore = await _context.SlotOccupancies.CountAsync(s => s.ReferenceId == 77);
        countBefore.Should().BeGreaterThan(0);

        await _sut.ReleaseSlotsAsync(77, OccupancyType.SoftReserve);
        await _context.SaveChangesAsync();

        var countAfter = await _context.SlotOccupancies.CountAsync(s => s.ReferenceId == 77);
        countAfter.Should().Be(0);
    }

    #endregion

    #region TransferSlotsAsync

    [Fact]
    public async Task TransferSlots_ChangesEmployeeId()
    {
        // Add a second employee
        _context.Employees.Add(new Employee
        {
            Id = 3,
            FirstName = "Ana",
            LastName = "Lopez",
            Email = "ana@test.com",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(14);
        await _sut.AcquireSlotsAsync(1, start, start.AddHours(1), OccupancyType.Meeting, 88);
        await _context.SaveChangesAsync();

        await _sut.TransferSlotsAsync(88, OccupancyType.Meeting, 3);
        await _context.SaveChangesAsync();

        var slots = await _context.SlotOccupancies.Where(s => s.ReferenceId == 88).ToListAsync();
        slots.Should().AllSatisfy(s => s.EmployeeId.Should().Be(3));
    }

    #endregion
}
