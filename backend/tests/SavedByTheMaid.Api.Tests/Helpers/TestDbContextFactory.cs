using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory ApplicationDbContext instances for unit testing.
/// Each call to Create() returns a fresh database to ensure test isolation.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory ApplicationDbContext with a unique database name.
    /// </summary>
    public static ApplicationDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? $"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Creates a context and seeds it with common test data.
    /// </summary>
    public static ApplicationDbContext CreateSeeded(string? dbName = null)
    {
        var context = Create(dbName);
        SeedRoles(context);
        SeedServiceTypes(context);
        SeedCleaningPlaces(context);
        SeedServiceAreas(context);
        SeedEmployees(context);
        context.SaveChanges();
        return context;
    }

    #region Seed Methods

    public static void SeedRoles(ApplicationDbContext context)
    {
        if (context.Roles.Any()) return;

        context.Roles.AddRange(
            new IdentityRole
            {
                Id = "role-admin",
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole
            {
                Id = "role-employee",
                Name = "Employee",
                NormalizedName = "EMPLOYEE",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole
            {
                Id = "role-customer",
                Name = "Customer",
                NormalizedName = "CUSTOMER",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        );
        context.SaveChanges();
    }

    public static void SeedServiceTypes(ApplicationDbContext context)
    {
        if (context.ServiceTypes.Any()) return;

        context.ServiceTypes.Add(new ServiceType
        {
            Id = 1,
            Name = "Standard Cleaning",
            Description = "Regular house cleaning",
            Price = 80.00m,
            Cost = 40.00m,
            PricePerBedroom = 15.00m,
            PricePerBathroom = 20.00m,
            EstimatedMinutes = 60,
            MinutesPerBedroom = 20,
            MinutesPerBathroom = 15,
            DisplayOrder = 1,
            IsActive = true
        });

        context.ServiceTypes.Add(new ServiceType
        {
            Id = 2,
            Name = "Deep Cleaning",
            Description = "Intensive deep cleaning",
            Price = 150.00m,
            Cost = 75.00m,
            PricePerBedroom = 25.00m,
            PricePerBathroom = 30.00m,
            EstimatedMinutes = 120,
            MinutesPerBedroom = 30,
            MinutesPerBathroom = 25,
            DisplayOrder = 2,
            IsActive = true
        });

        context.SaveChanges();
    }

    public static void SeedAdditionalServiceTypes(ApplicationDbContext context)
    {
        if (context.AdditionalServiceTypes.Any()) return;

        context.AdditionalServiceTypes.AddRange(
            new AdditionalServiceType
            {
                Id = 1,
                Title = "Oven Cleaning",
                Description = "Deep clean oven",
                Price = 30.00m,
                AdditionalMinutes = 30,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Id = 2,
                Title = "Window Cleaning",
                Description = "Interior window cleaning",
                Price = 25.00m,
                AdditionalMinutes = 20,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Id = 3,
                Title = "Fridge Cleaning",
                Description = "Interior fridge cleaning",
                Price = 20.00m,
                AdditionalMinutes = 15,
                IsActive = true
            }
        );
        context.SaveChanges();
    }

    public static void SeedCleaningPlaces(ApplicationDbContext context)
    {
        if (context.CleaningPlaces.Any()) return;

        context.CleaningPlaces.Add(new CleaningPlace
        {
            Id = 1,
            Name = "House",
            Description = "Single family house",
            IsActive = true
        });

        context.CleaningPlaceRooms.AddRange(
            new CleaningPlaceRoom
            {
                Id = 1,
                CleaningPlaceId = 1,
                Name = "Bedroom",
                BaseMinutes = 20,
                BasePrice = 15.00m,
                DisplayOrder = 1,
                IsActive = true
            },
            new CleaningPlaceRoom
            {
                Id = 2,
                CleaningPlaceId = 1,
                Name = "Bathroom",
                BaseMinutes = 25,
                BasePrice = 20.00m,
                DisplayOrder = 2,
                IsActive = true
            },
            new CleaningPlaceRoom
            {
                Id = 3,
                CleaningPlaceId = 1,
                Name = "Kitchen",
                BaseMinutes = 30,
                BasePrice = 25.00m,
                DisplayOrder = 3,
                IsActive = true
            }
        );
        context.SaveChanges();
    }

    public static void SeedServiceAreas(ApplicationDbContext context)
    {
        if (context.ServiceAreas.Any()) return;

        context.ServiceAreas.Add(new ServiceArea
        {
            Id = 1,
            Name = "Orlando Downtown",
            Description = "Central Orlando area",
            IsActive = true
        });

        context.ServiceAreaZips.Add(new ServiceAreaZip
        {
            Id = 1,
            ServiceAreaId = 1,
            ZipCode = "32801",
            City = "Orlando",
            State = "FL",
            IsFullCoverage = true
        });
        context.SaveChanges();
    }

    public static void SeedEmployees(ApplicationDbContext context)
    {
        if (context.Employees.Any()) return;

        context.Employees.Add(new Employee
        {
            Id = 1,
            FirstName = "Maria",
            LastName = "Garcia",
            Email = "maria@test.com",
            Phone = "555-0001",
            IsActive = true,
            PrimaryServiceAreaId = 1,
            MaxDailyHours = 8,
            MaxDailyServices = 4
        });

        // Add Mon-Fri schedule: 8am-6pm
        for (int day = 1; day <= 5; day++)
        {
            context.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = 1,
                DayOfWeek = (DayOfWeek)day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BufferMinutes = 30,
                IsAvailable = true
            });
        }

        context.Set<EmployeeServiceArea>().Add(new EmployeeServiceArea
        {
            EmployeeId = 1,
            ServiceAreaId = 1,
            IsPrimary = true
        });
        context.SaveChanges();
    }

    public static void SeedPriceMultipliers(ApplicationDbContext context)
    {
        if (context.PriceMultipliers.Any()) return;

        context.PriceMultipliers.AddRange(
            new PriceMultiplier
            {
                Id = 1,
                Name = "Heavy Dirt Level",
                ConditionType = MultiplierConditionType.DirtLevel,
                Factor = 1.3m,
                MinValue = 2, // DirtLevel.Heavy = 2
                AppliesToPrice = true,
                AppliesToTime = true,
                IsActive = true
            },
            new PriceMultiplier
            {
                Id = 2,
                Name = "Has Pets",
                ConditionType = MultiplierConditionType.HasPets,
                Factor = 1.1m,
                AppliesToPrice = true,
                AppliesToTime = true,
                IsActive = true
            },
            new PriceMultiplier
            {
                Id = 3,
                Name = "First Time Bonus",
                ConditionType = MultiplierConditionType.FirstTime,
                Factor = 1.15m,
                AppliesToPrice = true,
                AppliesToTime = true,
                IsActive = true
            },
            new PriceMultiplier
            {
                Id = 4,
                Name = "No Elevator",
                ConditionType = MultiplierConditionType.NoElevator,
                Factor = 1.05m,
                AppliesToPrice = true,
                AppliesToTime = false,
                IsActive = true
            },
            new PriceMultiplier
            {
                Id = 5,
                Name = "Large Square Footage (1000-2000)",
                ConditionType = MultiplierConditionType.SquareFootage,
                Factor = 1.2m,
                MinValue = 1000,
                MaxValue = 2000,
                AppliesToPrice = true,
                AppliesToTime = true,
                IsActive = true
            }
        );
        context.SaveChanges();
    }

    public static void SeedRecurrenceDiscounts(ApplicationDbContext context)
    {
        if (context.RecurrenceDiscounts.Any()) return;

        context.RecurrenceDiscounts.AddRange(
            new RecurrenceDiscount
            {
                Id = 1,
                RecurrenceType = RecurrenceType.Weekly,
                DiscountPercent = 0.15m,
                IsActive = true
            },
            new RecurrenceDiscount
            {
                Id = 2,
                RecurrenceType = RecurrenceType.BiWeekly,
                DiscountPercent = 0.10m,
                IsActive = true
            },
            new RecurrenceDiscount
            {
                Id = 3,
                RecurrenceType = RecurrenceType.Monthly,
                DiscountPercent = 0.05m,
                IsActive = true
            }
        );
        context.SaveChanges();
    }

    /// <summary>
    /// Creates a SoftReserve for testing booking confirmation.
    /// </summary>
    public static SoftReserve SeedSoftReserve(
        ApplicationDbContext context,
        int employeeId = 1,
        int serviceAreaId = 1,
        string sessionId = "test-session-123",
        DateTime? scheduledStart = null,
        DateTime? expiresAt = null,
        SoftReserveStatus status = SoftReserveStatus.Active)
    {
        var start = scheduledStart ?? GetNextWeekday(DateTime.UtcNow.AddDays(3), DayOfWeek.Monday).AddHours(10);
        var softReserve = new SoftReserve
        {
            SessionId = sessionId,
            EmployeeId = employeeId,
            ServiceAreaId = serviceAreaId,
            ScheduledStart = start,
            ScheduledEnd = start.AddMinutes(120),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(15),
            Status = status
        };

        context.SoftReserves.Add(softReserve);
        context.SaveChanges();
        return softReserve;
    }

    /// <summary>
    /// Returns the next occurrence of a given day of the week from a starting date.
    /// </summary>
    public static DateTime GetNextWeekday(DateTime from, DayOfWeek day)
    {
        var daysUntil = ((int)day - (int)from.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7;
        return from.Date.AddDays(daysUntil);
    }

    #endregion
}
