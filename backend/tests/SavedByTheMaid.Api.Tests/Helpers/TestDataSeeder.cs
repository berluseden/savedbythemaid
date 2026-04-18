using Microsoft.AspNetCore.Identity;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests.Helpers;

/// <summary>
/// Seeds master data required for integration tests.
/// </summary>
public static class TestDataSeeder
{
    public const string AdminUserId = "admin-test-user-id";
    public const string AdminEmail = "admin@test.com";
    public const string CustomerUserId = "customer-test-user-id";
    public const string CustomerEmail = "customer@test.com";
    public const string TestPassword = "TestPassword123!";

    public static void SeedAll(ApplicationDbContext context)
    {
        SeedRoles(context);
        SeedUsers(context);
        SeedServiceAreas(context);
        SeedCleaningPlaces(context);
        SeedServiceTypes(context);
        SeedEmployees(context);
        SeedOrders(context);
        context.SaveChanges();
    }

    private static void SeedRoles(ApplicationDbContext context)
    {
        if (context.Roles.Any()) return;

        context.Roles.AddRange(
            new IdentityRole { Id = "role-admin", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = Guid.NewGuid().ToString() },
            new IdentityRole { Id = "role-employee", Name = "Employee", NormalizedName = "EMPLOYEE", ConcurrencyStamp = Guid.NewGuid().ToString() },
            new IdentityRole { Id = "role-customer", Name = "Customer", NormalizedName = "CUSTOMER", ConcurrencyStamp = Guid.NewGuid().ToString() }
        );
    }

    private static void SeedUsers(ApplicationDbContext context)
    {
        var hasher = new PasswordHasher<ApplicationUser>();

        // Admin user
        var admin = new ApplicationUser
        {
            Id = AdminUserId,
            UserName = AdminEmail,
            NormalizedUserName = AdminEmail.ToUpperInvariant(),
            Email = AdminEmail,
            NormalizedEmail = AdminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Admin",
            LastName = "Test",
            IsActive = true
        };
        admin.PasswordHash = hasher.HashPassword(admin, TestPassword);
        context.Users.Add(admin);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = AdminUserId, RoleId = "role-admin" });

        // Customer user
        var customer = new ApplicationUser
        {
            Id = CustomerUserId,
            UserName = CustomerEmail,
            NormalizedUserName = CustomerEmail.ToUpperInvariant(),
            Email = CustomerEmail,
            NormalizedEmail = CustomerEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Customer",
            LastName = "Test",
            IsActive = true,
            Address = "123 Main St",
            City = "Orlando",
            State = "FL",
            ZipCode = "32801"
        };
        customer.PasswordHash = hasher.HashPassword(customer, TestPassword);
        context.Users.Add(customer);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = CustomerUserId, RoleId = "role-customer" });
    }

    private static void SeedServiceAreas(ApplicationDbContext context)
    {
        var area = new ServiceArea
        {
            Id = 1,
            Name = "Orlando Downtown",
            Description = "Central Orlando area",
            IsActive = true
        };
        context.ServiceAreas.Add(area);

        context.ServiceAreaZips.Add(new ServiceAreaZip
        {
            Id = 1,
            ServiceAreaId = 1,
            ZipCode = "32801",
            City = "Orlando",
            State = "FL",
            IsFullCoverage = true
        });
    }

    private static void SeedCleaningPlaces(ApplicationDbContext context)
    {
        var house = new CleaningPlace
        {
            Id = 1,
            Name = "House",
            Description = "Single family house",
            IsActive = true
        };
        context.CleaningPlaces.Add(house);

        context.CleaningPlaceRooms.AddRange(
            new CleaningPlaceRoom { Id = 1, CleaningPlaceId = 1, Name = "Bedroom", BaseMinutes = 20, BasePrice = 15, DisplayOrder = 1, IsActive = true },
            new CleaningPlaceRoom { Id = 2, CleaningPlaceId = 1, Name = "Bathroom", BaseMinutes = 25, BasePrice = 20, DisplayOrder = 2, IsActive = true }
        );
    }

    private static void SeedServiceTypes(ApplicationDbContext context)
    {
        context.ServiceTypes.Add(new ServiceType
        {
            Id = 1,
            Name = "Standard Cleaning",
            Description = "Regular house cleaning",
            Price = 80.00m,
            PricePerBedroom = 15.00m,
            PricePerBathroom = 20.00m,
            EstimatedMinutes = 60,
            MinutesPerBedroom = 20,
            MinutesPerBathroom = 15,
            DisplayOrder = 1,
            IsActive = true
        });

        context.Set<AdditionalServiceType>().Add(new AdditionalServiceType
        {
            Id = 1,
            Title = "Oven Cleaning",
            Description = "Deep clean oven",
            Price = 30.00m,
            AdditionalMinutes = 30,
            IsActive = true
        });
    }

    private static void SeedEmployees(ApplicationDbContext context)
    {
        var employee = new Employee
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
        };
        context.Employees.Add(employee);

        // Add schedule: Mon-Fri 8am-6pm
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
    }

    private static void SeedOrders(ApplicationDbContext context)
    {
        var order = new ServiceOrder
        {
            Id = 1,
            CustomerId = CustomerUserId,
            ServiceAreaId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            City = "Orlando",
            State = "FL",
            ServiceTypeId = 1,
            CleaningPlaceId = 1,
            Bedrooms = 2,
            Bathrooms = 1,
            Subtotal = 110.00m,
            Tax = 7.70m,
            Discount = 0,
            Total = 117.70m,
            OrderStatus = OrderStatus.Confirmed,
            Source = OrderSource.Website,
            RecurrenceType = RecurrenceType.None,
            EstimatedDurationMinutes = 95,
            ContactName = "Customer Test",
            ContactEmail = CustomerEmail
        };
        context.ServiceOrders.Add(order);

        var futureDate = DateTime.UtcNow.AddDays(7).Date.AddHours(10);
        context.ServiceMeets.Add(new ServiceMeet
        {
            Id = 1,
            ServiceOrderId = 1,
            AssignedEmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = futureDate,
            ScheduledEnd = futureDate.AddMinutes(95),
            Status = MeetStatus.Scheduled,
            EstimatedDurationMinutes = 95
        });

        // Completed order for stats
        context.ServiceOrders.Add(new ServiceOrder
        {
            Id = 2,
            CustomerId = CustomerUserId,
            ServiceAreaId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            City = "Orlando",
            State = "FL",
            ServiceTypeId = 1,
            CleaningPlaceId = 1,
            Bedrooms = 2,
            Bathrooms = 1,
            Subtotal = 110.00m,
            Tax = 7.70m,
            Discount = 0,
            Total = 117.70m,
            OrderStatus = OrderStatus.Completed,
            Source = OrderSource.Website,
            RecurrenceType = RecurrenceType.None,
            EstimatedDurationMinutes = 95,
            ContactName = "Customer Test",
            ContactEmail = CustomerEmail
        });
    }
}
