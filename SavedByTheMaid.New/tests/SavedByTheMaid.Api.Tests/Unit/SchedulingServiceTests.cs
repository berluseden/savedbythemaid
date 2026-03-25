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
        result.Message.Should().Contain("inactive");
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
        result.Message.Should().Contain("vacation");
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

    [Fact]
    public async Task CheckConflicts_WithMaxDailyHoursExceeded_ReturnsConflict()
    {
        // Arrange - set max daily hours to 4
        var employee = await _context.Employees.FindAsync(1);
        employee!.MaxDailyHours = 4;
        await _context.SaveChangesAsync();

        // Use a specific weekday and add schedule for it
        var testDate = DateTime.UtcNow.AddDays(3).Date;

        // Add an existing 3-hour meeting on that day
        _context.ServiceMeets.Add(new ServiceMeet
        {
            ServiceOrderId = 1,
            AssignedEmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = testDate.AddHours(9),
            ScheduledEnd = testDate.AddHours(12),
            Status = MeetStatus.Scheduled,
            EstimatedDurationMinutes = 180
        });
        await _context.SaveChangesAsync();

        // Try to add a 2-hour meeting (3 + 2 = 5 > 4 max)
        var result = await _sut.CheckConflictsAsync(1, testDate.AddHours(14), testDate.AddHours(16));

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.EmployeeUnavailable);
        result.Message.Should().Contain("daily hours");
    }

    [Fact]
    public async Task CheckConflicts_WithMaxDailyServicesExceeded_ReturnsConflict()
    {
        // Arrange - set max daily services to 2
        var employee = await _context.Employees.FindAsync(1);
        employee!.MaxDailyServices = 2;
        employee.MaxDailyHours = 10; // high limit so hours don't interfere
        await _context.SaveChangesAsync();

        var testDate = DateTime.UtcNow.AddDays(3).Date;

        // Add 2 existing meetings on that day
        _context.ServiceMeets.AddRange(
            new ServiceMeet
            {
                ServiceOrderId = 1,
                AssignedEmployeeId = 1,
                ServiceAreaId = 1,
                ScheduledStart = testDate.AddHours(8),
                ScheduledEnd = testDate.AddHours(9),
                Status = MeetStatus.Scheduled,
                EstimatedDurationMinutes = 60
            },
            new ServiceMeet
            {
                ServiceOrderId = 2,
                AssignedEmployeeId = 1,
                ServiceAreaId = 1,
                ScheduledStart = testDate.AddHours(10),
                ScheduledEnd = testDate.AddHours(11),
                Status = MeetStatus.Scheduled,
                EstimatedDurationMinutes = 60
            }
        );
        await _context.SaveChangesAsync();

        // Try to add a 3rd meeting
        var result = await _sut.CheckConflictsAsync(1, testDate.AddHours(14), testDate.AddHours(15));

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.EmployeeUnavailable);
        result.Message.Should().Contain("daily services");
    }

    [Fact]
    public async Task CheckConflicts_CancelledMeetingsNotCountedForDailyLimits()
    {
        // Arrange - max 2 services
        var employee = await _context.Employees.FindAsync(1);
        employee!.MaxDailyServices = 2;
        employee.MaxDailyHours = 10;
        await _context.SaveChangesAsync();

        var testDate = DateTime.UtcNow.AddDays(3).Date;

        // Add 2 meetings but one is cancelled
        _context.ServiceMeets.AddRange(
            new ServiceMeet
            {
                ServiceOrderId = 1,
                AssignedEmployeeId = 1,
                ServiceAreaId = 1,
                ScheduledStart = testDate.AddHours(8),
                ScheduledEnd = testDate.AddHours(9),
                Status = MeetStatus.Scheduled,
                EstimatedDurationMinutes = 60
            },
            new ServiceMeet
            {
                ServiceOrderId = 2,
                AssignedEmployeeId = 1,
                ServiceAreaId = 1,
                ScheduledStart = testDate.AddHours(10),
                ScheduledEnd = testDate.AddHours(11),
                Status = MeetStatus.Cancelled, // cancelled - should not count
                EstimatedDurationMinutes = 60
            }
        );
        await _context.SaveChangesAsync();

        // Try to add a 2nd active meeting
        var result = await _sut.CheckConflictsAsync(1, testDate.AddHours(14), testDate.AddHours(15));

        // Assert - should be OK since only 1 active meeting exists
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckConflicts_WithExcludedMeetingId_IgnoresExcludedMeeting()
    {
        // This test verifies that when rescheduling, the current meeting is excluded
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Add a SoftReserve slot (not a meeting)
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = start,
            SlotEnd = start.AddMinutes(30),
            OccupancyType = OccupancyType.SoftReserve,
            ReferenceId = 200,
            CreatedAt = DateTime.UtcNow
        });

        // Add a Meeting slot
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = start.AddMinutes(30),
            SlotEnd = start.AddMinutes(60),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 300,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Exclude meeting 300 (rescheduling) - but SoftReserve 200 should still conflict
        var result = await _sut.CheckConflictsAsync(1, start, start.AddHours(1), excludeMeetingId: 300);

        // Assert - should still find conflict with the SoftReserve
        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.ExistingBooking);
    }

    [Fact]
    public async Task CheckConflicts_WithSickLeaveTimeOff_ReturnsCorrectMessage()
    {
        var start = DateTime.UtcNow.AddDays(5).Date.AddHours(10);

        _context.EmployeeTimeOffs.Add(new EmployeeTimeOff
        {
            EmployeeId = 1,
            StartDateTime = start.AddHours(-1),
            EndDateTime = start.AddHours(8),
            Type = TimeOffType.Sick,
            Status = TimeOffStatus.Approved,
            Reason = "Feeling unwell"
        });
        await _context.SaveChangesAsync();

        var result = await _sut.CheckConflictsAsync(1, start, start.AddHours(2));

        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.TimeOff);
        result.Message.Should().Contain("sick leave");
        result.Details.Should().Be("Feeling unwell");
    }

    [Fact]
    public async Task CheckConflicts_WithManualBlockTimeOff_ReturnsCorrectMessage()
    {
        var start = DateTime.UtcNow.AddDays(5).Date.AddHours(10);

        _context.EmployeeTimeOffs.Add(new EmployeeTimeOff
        {
            EmployeeId = 1,
            StartDateTime = start.Date,
            EndDateTime = start.Date.AddDays(1),
            Type = TimeOffType.ManualBlock,
            Status = TimeOffStatus.Approved,
            Reason = "Admin blocked"
        });
        await _context.SaveChangesAsync();

        var result = await _sut.CheckConflictsAsync(1, start, start.AddHours(2));

        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.TimeOff);
        result.Message.Should().Contain("manual block");
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

    [Fact]
    public async Task AcquireSlots_WithExpiry_SetsExpirationTime()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var end = start.AddMinutes(60);
        var expiresAt = DateTime.UtcNow.AddMinutes(20);

        await _sut.AcquireSlotsAsync(1, start, end, OccupancyType.SoftReserve, 55, expiresAt);
        await _context.SaveChangesAsync();

        var slots = await _context.SlotOccupancies.Where(s => s.ReferenceId == 55).ToListAsync();
        slots.Should().HaveCount(2); // 10:00, 10:30
        slots.Should().AllSatisfy(s =>
        {
            s.ExpiresAt.Should().Be(expiresAt);
            s.OccupancyType.Should().Be(OccupancyType.SoftReserve);
        });
    }

    [Fact]
    public async Task AcquireSlots_MeetingType_NoExpiration()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var end = start.AddMinutes(60);

        await _sut.AcquireSlotsAsync(1, start, end, OccupancyType.Meeting, 60);
        await _context.SaveChangesAsync();

        var slots = await _context.SlotOccupancies.Where(s => s.ReferenceId == 60).ToListAsync();
        slots.Should().AllSatisfy(s =>
        {
            s.ExpiresAt.Should().BeNull();
            s.OccupancyType.Should().Be(OccupancyType.Meeting);
        });
    }

    [Fact]
    public async Task AcquireSlots_30MinuteGranularity_CorrectBoundaries()
    {
        // 10:00 to 11:30 should create exactly 3 slots: 10:00, 10:30, 11:00
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var end = start.AddMinutes(90);

        await _sut.AcquireSlotsAsync(1, start, end, OccupancyType.Meeting, 70);
        await _context.SaveChangesAsync();

        var slots = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == 70)
            .OrderBy(s => s.SlotStart)
            .ToListAsync();

        slots.Should().HaveCount(3);
        slots[0].SlotStart.Should().Be(start);
        slots[0].SlotEnd.Should().Be(start.AddMinutes(30));
        slots[1].SlotStart.Should().Be(start.AddMinutes(30));
        slots[1].SlotEnd.Should().Be(start.AddMinutes(60));
        slots[2].SlotStart.Should().Be(start.AddMinutes(60));
        slots[2].SlotEnd.Should().Be(start.AddMinutes(90));
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

    [Fact]
    public async Task ReleaseSlots_WithNoSlots_DoesNothing()
    {
        // Arrange - no slots exist for this reference
        var countBefore = await _context.SlotOccupancies.CountAsync();

        // Act - should not throw
        await _sut.ReleaseSlotsAsync(99999, OccupancyType.Meeting);
        await _context.SaveChangesAsync();

        // Assert - count unchanged
        var countAfter = await _context.SlotOccupancies.CountAsync();
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task ReleaseSlots_OnlyRemovesMatchingTypeAndReference()
    {
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Add slots for two different references
        await _sut.AcquireSlotsAsync(1, start, start.AddHours(1), OccupancyType.SoftReserve, 101);
        await _sut.AcquireSlotsAsync(1, start.AddHours(2), start.AddHours(3), OccupancyType.Meeting, 102);
        await _context.SaveChangesAsync();

        var countRef101Before = await _context.SlotOccupancies.CountAsync(s => s.ReferenceId == 101);
        var countRef102Before = await _context.SlotOccupancies.CountAsync(s => s.ReferenceId == 102);
        countRef101Before.Should().BeGreaterThan(0);
        countRef102Before.Should().BeGreaterThan(0);

        // Release only reference 101
        await _sut.ReleaseSlotsAsync(101, OccupancyType.SoftReserve);
        await _context.SaveChangesAsync();

        var countRef101After = await _context.SlotOccupancies.CountAsync(s => s.ReferenceId == 101);
        var countRef102After = await _context.SlotOccupancies.CountAsync(s => s.ReferenceId == 102);
        countRef101After.Should().Be(0);
        countRef102After.Should().Be(countRef102Before); // untouched
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

    [Fact]
    public async Task TransferSlots_WithConflict_ReturnsConflict()
    {
        // Arrange - add two employees
        _context.Employees.Add(new Employee
        {
            Id = 4,
            FirstName = "Carlos",
            LastName = "Rivera",
            Email = "carlos@test.com",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Employee 1 has slots for reference 111
        await _sut.AcquireSlotsAsync(1, start, start.AddHours(1), OccupancyType.Meeting, 111);
        // Employee 4 already has slots at the same time
        await _sut.AcquireSlotsAsync(4, start, start.AddHours(1), OccupancyType.Meeting, 222);
        await _context.SaveChangesAsync();

        // Act - try to transfer employee 1's slots to employee 4 (conflict)
        var result = await _sut.TransferSlotsAsync(111, OccupancyType.Meeting, 4);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(ConflictType.ExistingBooking);
    }

    [Fact]
    public async Task TransferSlots_UpdatesEmployeeId()
    {
        // Arrange
        _context.Employees.Add(new Employee
        {
            Id = 5,
            FirstName = "Sofia",
            LastName = "Martinez",
            Email = "sofia@test.com",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var start = DateTime.UtcNow.AddDays(2).Date.AddHours(14);
        await _sut.AcquireSlotsAsync(1, start, start.AddHours(2), OccupancyType.Meeting, 333);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.TransferSlotsAsync(333, OccupancyType.Meeting, 5);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().BeNull(); // no conflict
        var slots = await _context.SlotOccupancies.Where(s => s.ReferenceId == 333).ToListAsync();
        slots.Should().HaveCount(4); // 2 hours = 4 slots
        slots.Should().AllSatisfy(s =>
        {
            s.EmployeeId.Should().Be(5);
            s.UpdatedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task TransferSlots_WithNoSlots_ReturnsNull()
    {
        // Arrange - no slots exist for this reference
        _context.Employees.Add(new Employee
        {
            Id = 6,
            FirstName = "Diego",
            LastName = "Perez",
            Email = "diego@test.com",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.TransferSlotsAsync(99999, OccupancyType.Meeting, 6);

        // Assert - returns null (no conflict, but also no slots to transfer)
        result.Should().BeNull();
    }

    #endregion
}
