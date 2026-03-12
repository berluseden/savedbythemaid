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
/// Tests for business logic fixes:
/// - BufferMinutes in scheduling
/// - MaxDailyHours/MaxDailyServices enforcement
/// - TransferSlots conflict check
/// - SoftReserve extension cap
/// - SoftReserve soft-delete filter
/// </summary>
public class BusinessLogicFixTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SchedulingService _sut;

    public BusinessLogicFixTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"BusinessLogicTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var logger = Mock.Of<ILogger<SchedulingService>>();
        _sut = new SchedulingService(_context, logger);

        SeedData();
    }

    private void SeedData()
    {
        _context.Employees.Add(new Employee
        {
            Id = 1,
            FirstName = "Maria",
            LastName = "Garcia",
            Email = "maria@test.com",
            IsActive = true,
            PrimaryServiceAreaId = 1,
            MaxDailyHours = 8,
            MaxDailyServices = 4
        });

        _context.Employees.Add(new Employee
        {
            Id = 2,
            FirstName = "Ana",
            LastName = "Lopez",
            Email = "ana@test.com",
            IsActive = true,
            PrimaryServiceAreaId = 1,
            MaxDailyHours = 6,
            MaxDailyServices = 3
        });

        // Add schedule for employee 1: Mon-Fri 8am-6pm with 30min buffer
        for (int day = 1; day <= 5; day++)
        {
            _context.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = 1,
                DayOfWeek = (DayOfWeek)day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BufferMinutes = 30,
                IsAvailable = true
            });
        }

        // Schedule for employee 2: Mon-Fri 9am-5pm with 15min buffer
        for (int day = 1; day <= 5; day++)
        {
            _context.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = 2,
                DayOfWeek = (DayOfWeek)day,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                BufferMinutes = 15,
                IsAvailable = true
            });
        }

        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    #region BufferMinutes Tests

    [Fact]
    public async Task CheckConflicts_BufferMinutes_BlocksAdjacentSlots()
    {
        // Employee 1 has 30min buffer. Meeting at 10:00-11:00.
        // A new meeting at 11:00-12:00 should conflict because of the buffer.
        var date = GetNextWeekday(DayOfWeek.Monday);
        var existingStart = date.AddHours(10);
        var existingEnd = existingStart.AddHours(1);

        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = existingStart,
            SlotEnd = existingStart.AddMinutes(30),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 100,
            CreatedAt = DateTime.UtcNow
        });
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = existingStart.AddMinutes(30),
            SlotEnd = existingEnd,
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 100,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Try to book immediately after (11:00-12:00) - within 30min buffer
        var result = await _sut.CheckConflictsAsync(1, existingEnd, existingEnd.AddHours(1));
        result.Should().NotBeNull("30-minute buffer should prevent back-to-back bookings");
        result!.Type.Should().Be(ConflictType.ExistingBooking);
    }

    [Fact]
    public async Task CheckConflicts_BufferMinutes_AllowsAfterBuffer()
    {
        // Employee 1 has 30min buffer. Meeting at 10:00-11:00.
        // A new meeting at 11:30-12:30 should be allowed (after buffer).
        var date = GetNextWeekday(DayOfWeek.Tuesday);
        var existingStart = date.AddHours(10);
        var existingEnd = existingStart.AddHours(1);

        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = existingStart,
            SlotEnd = existingStart.AddMinutes(30),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 200,
            CreatedAt = DateTime.UtcNow
        });
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = existingStart.AddMinutes(30),
            SlotEnd = existingEnd,
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 200,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Book at 11:30 (30min after end = exactly past buffer)
        var result = await _sut.CheckConflictsAsync(1, existingEnd.AddMinutes(30), existingEnd.AddMinutes(90));
        result.Should().BeNull("booking after buffer period should be allowed");
    }

    #endregion

    #region MaxDailyServices Tests

    [Fact]
    public async Task CheckConflicts_MaxDailyServices_RejectsWhenLimitReached()
    {
        // Employee 1 has MaxDailyServices = 4
        var date = GetNextWeekday(DayOfWeek.Wednesday);

        // Create 4 existing meetings (max daily limit)
        for (int i = 0; i < 4; i++)
        {
            var meetStart = date.AddHours(8 + i * 2);
            _context.ServiceMeets.Add(new ServiceMeet
            {
                ServiceOrderId = 1,
                AssignedEmployeeId = 1,
                ServiceAreaId = 1,
                ScheduledStart = meetStart,
                ScheduledEnd = meetStart.AddMinutes(90),
                EstimatedDurationMinutes = 90,
                Status = MeetStatus.Scheduled
            });
        }
        await _context.SaveChangesAsync();

        // Try to add a 5th meeting
        var newStart = date.AddHours(17);
        var result = await _sut.CheckConflictsAsync(1, newStart, newStart.AddHours(1));

        result.Should().NotBeNull("should reject when max daily services reached");
        result!.Type.Should().Be(ConflictType.EmployeeUnavailable);
        result.Message.Should().Contain("máximo de 4 servicios");
    }

    [Fact]
    public async Task CheckConflicts_MaxDailyServices_AllowsUnderLimit()
    {
        // Employee 1 has MaxDailyServices = 4, only 2 existing
        var date = GetNextWeekday(DayOfWeek.Thursday);

        for (int i = 0; i < 2; i++)
        {
            var meetStart = date.AddHours(8 + i * 3);
            _context.ServiceMeets.Add(new ServiceMeet
            {
                ServiceOrderId = 1,
                AssignedEmployeeId = 1,
                ServiceAreaId = 1,
                ScheduledStart = meetStart,
                ScheduledEnd = meetStart.AddMinutes(90),
                EstimatedDurationMinutes = 90,
                Status = MeetStatus.Scheduled
            });
        }
        await _context.SaveChangesAsync();

        var newStart = date.AddHours(15);
        var result = await _sut.CheckConflictsAsync(1, newStart, newStart.AddHours(1));

        result.Should().BeNull("should allow when under daily service limit");
    }

    [Fact]
    public async Task CheckConflicts_MaxDailyServices_ExcludesCancelledMeetings()
    {
        var date = GetNextWeekday(DayOfWeek.Friday);

        // 4 meetings, but 2 are cancelled
        for (int i = 0; i < 4; i++)
        {
            var meetStart = date.AddHours(8 + i * 2);
            _context.ServiceMeets.Add(new ServiceMeet
            {
                ServiceOrderId = 1,
                AssignedEmployeeId = 1,
                ServiceAreaId = 1,
                ScheduledStart = meetStart,
                ScheduledEnd = meetStart.AddMinutes(90),
                EstimatedDurationMinutes = 90,
                Status = i < 2 ? MeetStatus.Scheduled : MeetStatus.Cancelled
            });
        }
        await _context.SaveChangesAsync();

        var newStart = date.AddHours(17);
        var result = await _sut.CheckConflictsAsync(1, newStart, newStart.AddHours(1));

        result.Should().BeNull("cancelled meetings should not count toward daily limit");
    }

    #endregion

    #region MaxDailyHours Tests

    [Fact]
    public async Task CheckConflicts_MaxDailyHours_RejectsWhenExceeded()
    {
        // Employee 2 has MaxDailyHours = 6
        var date = GetNextWeekday(DayOfWeek.Monday);

        // 5 hours of existing meetings
        var meetStart = date.AddHours(9);
        _context.ServiceMeets.Add(new ServiceMeet
        {
            ServiceOrderId = 1,
            AssignedEmployeeId = 2,
            ServiceAreaId = 1,
            ScheduledStart = meetStart,
            ScheduledEnd = meetStart.AddHours(5),
            EstimatedDurationMinutes = 300,
            Status = MeetStatus.Scheduled
        });
        await _context.SaveChangesAsync();

        // Try to add 2 more hours (5 + 2 = 7 > 6 max)
        var newStart = date.AddHours(15);
        var result = await _sut.CheckConflictsAsync(2, newStart, newStart.AddHours(2));

        result.Should().NotBeNull("should reject when max daily hours would be exceeded");
        result!.Type.Should().Be(ConflictType.EmployeeUnavailable);
        result.Message.Should().Contain("máximo de 6 horas");
    }

    #endregion

    #region TransferSlots Conflict Check

    [Fact]
    public async Task TransferSlots_WithConflict_ReturnsConflictInsteadOfTransferring()
    {
        var date = GetNextWeekday(DayOfWeek.Tuesday);
        var start = date.AddHours(10);

        // Employee 1 has slots for meeting 50
        await _sut.AcquireSlotsAsync(1, start, start.AddHours(1), OccupancyType.Meeting, 50);
        await _context.SaveChangesAsync();

        // Employee 2 has a conflicting slot at the same time
        _context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 2,
            SlotStart = start,
            SlotEnd = start.AddMinutes(30),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 60,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Try to transfer slots from employee 1 to employee 2
        var conflict = await _sut.TransferSlotsAsync(50, OccupancyType.Meeting, 2);

        conflict.Should().NotBeNull("transfer should be blocked when new employee has conflict");
        conflict!.Type.Should().Be(ConflictType.ExistingBooking);

        // Original slots should still belong to employee 1
        var slots = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == 50)
            .ToListAsync();
        slots.Should().AllSatisfy(s => s.EmployeeId.Should().Be(1));
    }

    [Fact]
    public async Task TransferSlots_WithoutConflict_TransfersSuccessfully()
    {
        var date = GetNextWeekday(DayOfWeek.Wednesday);
        var start = date.AddHours(14);

        await _sut.AcquireSlotsAsync(1, start, start.AddHours(1), OccupancyType.Meeting, 70);
        await _context.SaveChangesAsync();

        var conflict = await _sut.TransferSlotsAsync(70, OccupancyType.Meeting, 2);

        conflict.Should().BeNull("transfer should succeed when no conflict");

        var slots = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == 70)
            .ToListAsync();
        slots.Should().AllSatisfy(s => s.EmployeeId.Should().Be(2));
    }

    #endregion

    #region SoftReserve Extension Cap

    [Fact]
    public void SoftReserve_ExtensionCount_DefaultsToZero()
    {
        var reserve = new SoftReserve
        {
            SessionId = "test",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = DateTime.UtcNow,
            ScheduledEnd = DateTime.UtcNow.AddHours(1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        reserve.ExtensionCount.Should().Be(0);
    }

    [Fact]
    public async Task SoftReserve_ExtensionCount_PersistsCorrectly()
    {
        var reserve = new SoftReserve
        {
            SessionId = "test-ext",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = DateTime.UtcNow,
            ScheduledEnd = DateTime.UtcNow.AddHours(1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            ExtensionCount = 2
        };

        _context.SoftReserves.Add(reserve);
        await _context.SaveChangesAsync();

        var loaded = await _context.SoftReserves.FirstAsync(s => s.SessionId == "test-ext");
        loaded.ExtensionCount.Should().Be(2);
    }

    #endregion

    #region SoftReserve Soft-Delete Filter

    [Fact]
    public async Task SoftReserve_SoftDeleteFilter_ExcludesDeleted()
    {
        _context.SoftReserves.Add(new SoftReserve
        {
            SessionId = "active-reserve",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = DateTime.UtcNow,
            ScheduledEnd = DateTime.UtcNow.AddHours(1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Status = SoftReserveStatus.Active
        });

        _context.SoftReserves.Add(new SoftReserve
        {
            SessionId = "deleted-reserve",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = DateTime.UtcNow,
            ScheduledEnd = DateTime.UtcNow.AddHours(1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Status = SoftReserveStatus.Active,
            IsDeleted = true
        });

        await _context.SaveChangesAsync();

        var reserves = await _context.SoftReserves.ToListAsync();
        reserves.Should().HaveCount(1, "soft-deleted reserves should be filtered out");
        reserves[0].SessionId.Should().Be("active-reserve");
    }

    #endregion

    private static DateTime GetNextWeekday(DayOfWeek day)
    {
        var date = DateTime.UtcNow.AddDays(7).Date;
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
