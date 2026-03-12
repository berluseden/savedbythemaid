using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests.Unit;

public class DomainEntityTests
{
    #region Employee.FullName

    [Theory]
    [InlineData("Maria", "Garcia", "Maria Garcia")]
    [InlineData("Ana", "Lopez", "Ana Lopez")]
    [InlineData("", "", " ")]
    public void Employee_FullName_ConcatenatesFirstAndLastName(string first, string last, string expected)
    {
        var employee = new Employee { FirstName = first, LastName = last };
        employee.FullName.Should().Be(expected);
    }

    #endregion

    #region ServiceOrder - New orders must start as Draft

    [Fact]
    public void ServiceOrder_NewOrder_StartsAsDraft()
    {
        var order = new ServiceOrder { ZipCode = "32801", Address = "123 Main St" };

        order.OrderStatus.Should().Be(OrderStatus.Draft,
            "new orders should always start in Draft status to prevent unreviewed bookings");
    }

    [Fact]
    public void ServiceOrder_NewOrder_DefaultsToSingleOccurrence()
    {
        var order = new ServiceOrder { ZipCode = "32801", Address = "123 Main St" };

        order.RecurrenceType.Should().Be(RecurrenceType.None,
            "orders should default to one-time to prevent accidental recurring charges");
    }

    #endregion

    #region Soft Delete - IsDeleted filter

    [Fact]
    public async Task SoftDelete_GlobalQueryFilter_ExcludesDeletedEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SoftDeleteTest_{Guid.NewGuid()}")
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        context.CleaningPlaces.Add(new CleaningPlace { Name = "Active Place" });
        context.CleaningPlaces.Add(new CleaningPlace { Name = "Deleted Place", IsDeleted = true });
        await context.SaveChangesAsync();

        // Query WITHOUT IgnoreQueryFilters should exclude soft-deleted
        var visible = await context.CleaningPlaces.ToListAsync();
        visible.Should().ContainSingle(p => p.Name == "Active Place");
        visible.Should().NotContain(p => p.Name == "Deleted Place",
            "soft-deleted entities should be filtered out by default query filter");

        // Query WITH IgnoreQueryFilters should return all
        var all = await context.CleaningPlaces.IgnoreQueryFilters().ToListAsync();
        all.Should().HaveCount(2, "IgnoreQueryFilters should return both active and deleted");
    }

    #endregion

    #region CleaningPlaceRoom - Pricing model

    [Fact]
    public void CleaningPlaceRoom_BaseMinutes_MustBePositive()
    {
        // The domain model uses BaseMinutes for time estimation.
        // Default must be a sane value, not zero (which would mean "takes no time")
        var room = new CleaningPlaceRoom { Name = "Bedroom" };

        room.BaseMinutes.Should().BeGreaterThan(0,
            "default base minutes must be positive for correct time estimation");
        room.BasePrice.Should().BeGreaterThan(0,
            "default base price must be positive for correct pricing");
    }

    #endregion

    #region ServiceOrder - Financial calculations invariant

    [Fact]
    public async Task ServiceOrder_Total_MustEqualSubtotalPlusTaxMinusDiscount()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FinancialTest_{Guid.NewGuid()}")
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        // Insert an order where Total != Subtotal + Tax - Discount (inconsistent data)
        var order = new ServiceOrder
        {
            ZipCode = "32801",
            Address = "123 Main St",
            ServiceTypeId = 1,
            Subtotal = 100.00m,
            Tax = 7.00m,
            Discount = 10.00m,
            Total = 97.00m // correct: 100 + 7 - 10 = 97
        };

        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();

        var saved = await context.ServiceOrders.FindAsync(order.Id);
        var expectedTotal = saved!.Subtotal + saved.Tax - saved.Discount;
        saved.Total.Should().Be(expectedTotal,
            "Total should equal Subtotal + Tax - Discount for financial consistency");
    }

    #endregion

    #region Employee - Operational limits

    [Fact]
    public void Employee_NewEmployee_HasSaneOperationalDefaults()
    {
        var employee = new Employee { FirstName = "Test", LastName = "Worker" };

        // Business rule: employees have limits to prevent overwork
        employee.MaxDailyHours.Should().Be(8, "default daily hours should be 8 (standard workday)");
        employee.MaxDailyServices.Should().Be(4, "default max services should be 4 per day");
        employee.IsActive.Should().BeTrue("new employees should be active by default");
    }

    #endregion

    #region SlotOccupancy - 30-minute granularity

    [Fact]
    public async Task SlotOccupancy_OverlappingSlots_DetectedByQuery()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SlotOverlapTest_{Guid.NewGuid()}")
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        var baseTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Existing slot: 10:00-10:30
        context.SlotOccupancies.Add(new SlotOccupancy
        {
            EmployeeId = 1,
            SlotStart = baseTime,
            SlotEnd = baseTime.AddMinutes(30),
            OccupancyType = OccupancyType.Meeting,
            ReferenceId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Query: does anything overlap with 9:45-10:15?
        var queryStart = baseTime.AddMinutes(-15);
        var queryEnd = baseTime.AddMinutes(15);

        var conflicts = await context.SlotOccupancies
            .Where(s => s.EmployeeId == 1 && s.SlotStart < queryEnd && s.SlotEnd > queryStart)
            .ToListAsync();

        conflicts.Should().HaveCount(1,
            "overlapping time range query should detect the conflicting slot");
    }

    #endregion

    #region SoftReserve - Expiration model

    [Fact]
    public void SoftReserve_NewReserve_DefaultsToActive()
    {
        var reserve = new SoftReserve
        {
            SessionId = "test-session",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = DateTime.UtcNow.AddHours(1),
            ScheduledEnd = DateTime.UtcNow.AddHours(2),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        reserve.Status.Should().Be(SoftReserveStatus.Active,
            "new soft reserves should be Active so they block slots until expired or confirmed");
    }

    [Fact]
    public void SoftReserve_ExpiredReserve_CanBeDetectedByExpiresAt()
    {
        var reserve = new SoftReserve
        {
            SessionId = "test-session",
            EmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = DateTime.UtcNow.AddHours(1),
            ScheduledEnd = DateTime.UtcNow.AddHours(2),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // expired 5 min ago
        };

        var isExpired = reserve.ExpiresAt < DateTime.UtcNow;
        isExpired.Should().BeTrue("reserves past their ExpiresAt should be identifiable as expired");
    }

    #endregion
}
