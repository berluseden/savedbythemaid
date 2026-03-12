using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Infrastructure.Data;

/// <summary>
/// Full seed of master data for MVP
/// Idempotent: can be executed multiple times without duplicating
/// </summary>
public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAllAsync()
    {
        _logger.LogInformation("Starting master data seed...");

        try
        {
            // 0. Create additional tables if they don't exist (SlotOccupancy, StatusHistory)
            await EnsureAdditionalTablesAsync();

            // 1. Clean duplicates
            await CleanDuplicatesAsync();

            // 2. Service Types
            await SeedServiceTypesAsync();

            // 3. Cleaning Places and Rooms
            await SeedCleaningPlacesAsync();

            // 4. Additional Services
            await SeedAdditionalServicesAsync();

            // 5. Service Areas and ZIP Codes
            await SeedServiceAreasAsync();

            // 6. Equipment
            await SeedEquipmentAsync();

            // 7. Sample Employees
            await SeedEmployeesAsync();

            // 8. Employee Schedules and Service Areas
            await SeedEmployeeSchedulesAsync();

            // 9. Recurrence Discounts
            await SeedRecurrenceDiscountsAsync();

            _logger.LogInformation("Master data seed completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seed");
            throw;
        }
    }

    /// <summary>
    /// Creates additional tables (SlotOccupancy, StatusHistory) if they don't exist.
    /// This is idempotent and safe to execute multiple times.
    /// </summary>
    private async Task EnsureAdditionalTablesAsync()
    {
        _logger.LogInformation("Checking additional tables...");

        // SlotOccupancies - Anti-collision
        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS SlotOccupancies (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                EmployeeId INT NOT NULL,
                SlotStart DATETIME(6) NOT NULL,
                SlotEnd DATETIME(6) NOT NULL,
                OccupancyType INT NOT NULL DEFAULT 0,
                ReferenceId INT NOT NULL,
                ExpiresAt DATETIME(6) NULL,
                CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                UpdatedAt DATETIME(6) NULL,
                CreatedByUserId VARCHAR(255) NULL,
                UpdatedByUserId VARCHAR(255) NULL,
                IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
                CONSTRAINT FK_SlotOccupancies_Employees 
                    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
                INDEX IX_SlotOccupancies_ExpiresAt_OccupancyType (ExpiresAt, OccupancyType),
                INDEX IX_SlotOccupancies_OccupancyType_ReferenceId (OccupancyType, ReferenceId),
                UNIQUE INDEX IX_SlotOccupancies_AntiCollision (EmployeeId, SlotStart, IsDeleted)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        // OrderStatusHistories - Order auditing
        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS OrderStatusHistories (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                ServiceOrderId INT NOT NULL,
                FromStatus INT NULL,
                ToStatus INT NOT NULL,
                ChangedById VARCHAR(255) NULL,
                ChangedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                ReasonCode VARCHAR(50) NULL,
                Notes TEXT NULL,
                CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                UpdatedAt DATETIME(6) NULL,
                CreatedByUserId VARCHAR(255) NULL,
                UpdatedByUserId VARCHAR(255) NULL,
                IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
                CONSTRAINT FK_OrderStatusHistories_ServiceOrders 
                    FOREIGN KEY (ServiceOrderId) REFERENCES ServiceOrders(Id) ON DELETE CASCADE,
                INDEX IX_OrderStatusHistories_ServiceOrderId (ServiceOrderId),
                INDEX IX_OrderStatusHistories_ChangedAt (ChangedAt)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        // MeetStatusHistories - Appointment auditing
        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS MeetStatusHistories (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                ServiceMeetId INT NOT NULL,
                FromStatus INT NULL,
                ToStatus INT NOT NULL,
                ChangedById VARCHAR(255) NULL,
                ChangedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                ReasonCode VARCHAR(50) NULL,
                Notes TEXT NULL,
                CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                UpdatedAt DATETIME(6) NULL,
                CreatedByUserId VARCHAR(255) NULL,
                UpdatedByUserId VARCHAR(255) NULL,
                IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
                CONSTRAINT FK_MeetStatusHistories_ServiceMeets 
                    FOREIGN KEY (ServiceMeetId) REFERENCES ServiceMeets(Id) ON DELETE CASCADE,
                INDEX IX_MeetStatusHistories_ServiceMeetId (ServiceMeetId),
                INDEX IX_MeetStatusHistories_ChangedAt (ChangedAt)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        _logger.LogInformation("Additional tables verified/created");
    }

    private async Task CleanDuplicatesAsync()
    {
        _logger.LogInformation("Cleaning duplicates...");

        // Delete obsolete admin user without FirstName
        var obsoleteAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "admin@savedbythemaid.com" && u.FirstName == null);
        if (obsoleteAdmin != null)
        {
            _context.Users.Remove(obsoleteAdmin);
            _logger.LogInformation("Obsolete admin user deleted");
        }

        // Delete duplicate ServiceAreas - load into memory first
        var allAreas = await _context.ServiceAreas.ToListAsync();
        var duplicateGroups = allAreas
            .GroupBy(sa => sa.Name)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var toKeep = group.First();
            var toRemove = group.Skip(1).ToList();
            _context.ServiceAreas.RemoveRange(toRemove);
            _logger.LogInformation("Deleted {Count} duplicate areas for '{Name}'", toRemove.Count, group.Key);
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedServiceTypesAsync()
    {
        var serviceTypes = new[]
        {
            new ServiceType
            {
                Name = "Regular Cleaning",
                Description = "Standard weekly or biweekly maintenance cleaning",
                Cost = 45.00m,
                Price = 75.00m,
                PricePerBedroom = 15.00m,
                PricePerBathroom = 10.00m,
                EstimatedMinutes = 120,
                MinutesPerBedroom = 20,
                MinutesPerBathroom = 15,
                IsActive = true
            },
            new ServiceType
            {
                Name = "Deep Cleaning",
                Description = "Thorough detailed cleaning including hard-to-reach areas",
                Cost = 90.00m,
                Price = 150.00m,
                PricePerBedroom = 25.00m,
                PricePerBathroom = 20.00m,
                EstimatedMinutes = 180,
                MinutesPerBedroom = 30,
                MinutesPerBathroom = 25,
                IsActive = true
            },
            new ServiceType
            {
                Name = "Move In/Out",
                Description = "Complete cleaning for moves or property handover",
                Cost = 120.00m,
                Price = 200.00m,
                PricePerBedroom = 30.00m,
                PricePerBathroom = 25.00m,
                EstimatedMinutes = 240,
                MinutesPerBedroom = 40,
                MinutesPerBathroom = 30,
                IsActive = true
            },
            new ServiceType
            {
                Name = "Post-Construction",
                Description = "Cleaning after remodeling or construction",
                Cost = 150.00m,
                Price = 250.00m,
                PricePerBedroom = 35.00m,
                PricePerBathroom = 30.00m,
                EstimatedMinutes = 300,
                MinutesPerBedroom = 50,
                MinutesPerBathroom = 35,
                IsActive = true
            }
        };

        foreach (var serviceType in serviceTypes)
        {
            var exists = await _context.ServiceTypes
                .AnyAsync(st => st.Name == serviceType.Name);

            if (!exists)
            {
                _context.ServiceTypes.Add(serviceType);
                _logger.LogInformation("Service type created: {Name}", serviceType.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedCleaningPlacesAsync()
    {
        var places = new[]
        {
            new { Name = "House/Apartment", Description = "Single-family home or apartment", Rooms = new[] {
                ("Bedroom", "Bedroom cleaning including bed and closet", 25, 15.00m),
                ("Bathroom", "Full cleaning of toilet, shower, and sink", 20, 10.00m),
                ("Living Room", "Cleaning of living area and furniture", 20, 12.00m),
                ("Kitchen", "Kitchen cleaning, appliances, and cabinets", 30, 18.00m),
                ("Dining Room", "Dining area cleaning", 15, 10.00m),
                ("Office", "Home office or study cleaning", 20, 12.00m),
                ("Garage", "Garage or parking area cleaning", 25, 15.00m)
            }},
            new { Name = "Commercial Office", Description = "Office or commercial space", Rooms = new[] {
                ("Work Area", "Desk and work area cleaning", 20, 12.00m),
                ("Meeting Room", "Conference room cleaning", 20, 15.00m),
                ("Commercial Bathroom", "Commercial restroom cleaning", 25, 12.00m),
                ("Kitchen/Break Room", "Break area cleaning", 25, 15.00m),
                ("Reception", "Reception area cleaning", 15, 10.00m),
                ("Storage Room", "Storage area cleaning", 20, 12.00m)
            }},
            new { Name = "Airbnb/Vacation Rental", Description = "Vacation rental property", Rooms = new[] {
                ("Bedroom", "Deep post-guest cleaning", 30, 18.00m),
                ("Bathroom", "Full post-guest sanitization", 25, 15.00m),
                ("Living Room", "Cleaning and reorganization", 25, 15.00m),
                ("Kitchen", "Cleaning and restocking", 35, 20.00m),
                ("Outdoor Area", "Patio or balcony cleaning", 20, 12.00m)
            }}
        };

        foreach (var placeData in places)
        {
            var place = await _context.CleaningPlaces
                .Include(cp => cp.Rooms)
                .FirstOrDefaultAsync(cp => cp.Name == placeData.Name);

            if (place == null)
            {
                place = new CleaningPlace
                {
                    Name = placeData.Name,
                    Description = placeData.Description,
                    IsActive = true
                };
                _context.CleaningPlaces.Add(place);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Property type created: {Name}", place.Name);
            }

            // Add rooms
            foreach (var (name, desc, minutes, price) in placeData.Rooms)
            {
                var roomExists = place.Rooms?.Any(r => r.Name == name) ?? false;
                if (!roomExists)
                {
                    var room = new CleaningPlaceRoom
                    {
                        CleaningPlaceId = place.Id,
                        Name = name,
                        Description = desc,
                        BaseMinutes = minutes,
                        BasePrice = price,
                        IsActive = true
                    };
                    _context.CleaningPlaceRooms.Add(room);
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedAdditionalServicesAsync()
    {
        var services = new[]
        {
            new AdditionalServiceType
            {
                Title = "Oven Cleaning",
                Description = "Deep interior oven cleaning",
                Price = 25.00m,
                AdditionalMinutes = 30,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Refrigerator Cleaning",
                Description = "Complete interior refrigerator cleaning",
                Price = 30.00m,
                AdditionalMinutes = 40,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Inside Cabinets",
                Description = "Interior cleaning of kitchen cabinets",
                Price = 40.00m,
                AdditionalMinutes = 45,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Window Cleaning",
                Description = "Interior and exterior window cleaning",
                Price = 50.00m,
                AdditionalMinutes = 60,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Laundry",
                Description = "Washing, drying, and folding clothes",
                Price = 35.00m,
                AdditionalMinutes = 90,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Closet Organization",
                Description = "Closet organization and cleaning",
                Price = 45.00m,
                AdditionalMinutes = 60,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Carpet Cleaning",
                Description = "Deep cleaning of carpets and rugs",
                Price = 60.00m,
                AdditionalMinutes = 75,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Wall Cleaning",
                Description = "Wall cleaning and stain removal",
                Price = 40.00m,
                AdditionalMinutes = 50,
                IsActive = true
            }
        };

        foreach (var service in services)
        {
            var exists = await _context.AdditionalServiceTypes
                .AnyAsync(ast => ast.Title == service.Title);

            if (!exists)
            {
                _context.AdditionalServiceTypes.Add(service);
                _logger.LogInformation("Additional service created: {Title}", service.Title);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedServiceAreasAsync()
    {
        // Miami areas with full ZIP codes
        var miamiAreas = new[]
        {
            new { Name = "Miami Beach", ZipCodes = new[] {
                "33109", "33119", "33139", "33140", "33141", "33154"
            }},
            new { Name = "Downtown Miami", ZipCodes = new[] {
                "33128", "33130", "33131", "33132", "33136"
            }},
            new { Name = "Brickell", ZipCodes = new[] {
                "33129", "33130", "33131"
            }},
            new { Name = "Coral Gables", ZipCodes = new[] {
                "33114", "33133", "33134", "33143", "33146", "33156", "33158"
            }},
            new { Name = "Coconut Grove", ZipCodes = new[] {
                "33133", "33146"
            }},
            new { Name = "Wynwood/Midtown", ZipCodes = new[] {
                "33127", "33137", "33138"
            }},
            new { Name = "Little Havana", ZipCodes = new[] {
                "33125", "33135", "33145"
            }},
            new { Name = "Aventura", ZipCodes = new[] {
                "33160", "33180", "33181"
            }},
            new { Name = "North Miami", ZipCodes = new[] {
                "33161", "33162", "33167", "33168", "33169", "33181"
            }},
            new { Name = "Kendall", ZipCodes = new[] {
                "33156", "33157", "33173", "33176", "33183", "33186", "33193", "33196"
            }},
            new { Name = "Doral", ZipCodes = new[] {
                "33122", "33126", "33166", "33172", "33178", "33182", "33184"
            }},
            new { Name = "Homestead", ZipCodes = new[] {
                "33030", "33031", "33032", "33033", "33034", "33035", "33039"
            }}
        };

        foreach (var areaData in miamiAreas)
        {
            var area = await _context.ServiceAreas
                .Include(sa => sa.ZipCodes)
                .FirstOrDefaultAsync(sa => sa.Name == areaData.Name);

            if (area == null)
            {
                area = new ServiceArea
                {
                    Name = areaData.Name,
                    Description = $"Service area in {areaData.Name}",
                    IsActive = true
                };
                _context.ServiceAreas.Add(area);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Service area created: {Name}", area.Name);
            }

            // Add ZIP codes (only if not already globally assigned to avoid duplicates)
            foreach (var zipCode in areaData.ZipCodes)
            {
                // Check if the ZIP already exists in ANY area
                var zipExistsGlobally = await _context.ServiceAreaZips
                    .AnyAsync(saz => saz.ZipCode == zipCode);

                if (!zipExistsGlobally)
                {
                    var zip = new ServiceAreaZip
                    {
                        ServiceAreaId = area.Id,
                        ZipCode = zipCode,
                        City = "Miami",
                        State = "FL",
                        County = "Miami-Dade"
                    };
                    _context.ServiceAreaZips.Add(zip);
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Miami Service Areas and ZIPs seed completed");
    }

    private async Task SeedEquipmentAsync()
    {
        var equipment = new[]
        {
            new Equipment
            {
                Name = "Industrial Vacuum",
                Description = "High-power vacuum for commercial use",
                IsActive = true
            },
            new Equipment
            {
                Name = "Steam Mop",
                Description = "Steam cleaning system for floors",
                IsActive = true
            },
            new Equipment
            {
                Name = "Window Cleaning Kit",
                Description = "Specialized tools for window cleaning",
                IsActive = true
            },
            new Equipment
            {
                Name = "Carpet Washer",
                Description = "Machine for deep carpet cleaning",
                IsActive = true
            },
            new Equipment
            {
                Name = "Kit Eco-Friendly",
                Description = "Eco-friendly and biodegradable cleaning products",
                IsActive = true
            },
            new Equipment
            {
                Name = "Telescopic Ladder",
                Description = "Professional ladder for high areas",
                IsActive = true
            }
        };

        foreach (var item in equipment)
        {
            var exists = await _context.Equipment
                .AnyAsync(e => e.Name == item.Name);

            if (!exists)
            {
                _context.Equipment.Add(item);
                _logger.LogInformation("Equipment created: {Name}", item.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedEmployeesAsync()
    {
        // Only create sample employees if none exist
        var employeeCount = await _context.Employees.CountAsync();
        if (employeeCount >= 3)
        {
            _logger.LogInformation("Employees already exist, skipping sample employee seed");
            return;
        }

        var employees = new[]
        {
            new Employee
            {
                FirstName = "Maria",
                LastName = "Garcia",
                Email = "maria.garcia@savedbytemaid.com",
                Phone = "305-555-0101",
                IsActive = true,
                MaxDailyHours = 8,
                MaxDailyServices = 4
            },
            new Employee
            {
                FirstName = "Carmen",
                LastName = "Rodriguez",
                Email = "carmen.rodriguez@savedbytemaid.com",
                Phone = "305-555-0102",
                IsActive = true,
                MaxDailyHours = 8,
                MaxDailyServices = 4
            },
            new Employee
            {
                FirstName = "Sofia",
                LastName = "Martinez",
                Email = "sofia.martinez@savedbytemaid.com",
                Phone = "305-555-0103",
                IsActive = true,
                MaxDailyHours = 6,
                MaxDailyServices = 3
            }
        };

        foreach (var employee in employees)
        {
            var exists = await _context.Employees
                .AnyAsync(e => e.Email == employee.Email);

            if (!exists)
            {
                _context.Employees.Add(employee);
                _logger.LogInformation("Employee created: {Name}", $"{employee.FirstName} {employee.LastName}");
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedEmployeeSchedulesAsync()
    {
        _logger.LogInformation("Configuring schedules and service areas for employees...");

        var employees = await _context.Employees
            .Include(e => e.Schedules)
            .Include(e => e.ServiceAreas)
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        if (!employees.Any())
        {
            _logger.LogWarning("No employees found to configure schedules");
            return;
        }

        // Get all active service areas
        var serviceAreas = await _context.ServiceAreas
            .Where(sa => !sa.IsDeleted)
            .ToListAsync();

        foreach (var employee in employees)
        {
            // Create schedules for Monday through Friday (8am - 5pm)
            for (int day = 1; day <= 5; day++) // Monday = 1, Friday = 5
            {
                var schedule = employee.Schedules.FirstOrDefault(s => s.DayOfWeek == (DayOfWeek)day);
                if (schedule == null)
                {
                    employee.Schedules.Add(new EmployeeSchedule
                    {
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(8, 0, 0),  // 8:00 AM
                        EndTime = new TimeSpan(17, 0, 0),   // 5:00 PM
                        IsAvailable = true
                    });
                }
            }

            // Assign ALL service areas to each employee
            foreach (var area in serviceAreas)
            {
                if (!employee.ServiceAreas.Any(sa => sa.ServiceAreaId == area.Id))
                {
                    employee.ServiceAreas.Add(new EmployeeServiceArea
                    {
                        ServiceAreaId = area.Id,
                        IsDeleted = false
                    });
                }
            }

            _logger.LogInformation("Configured schedule and {Count} areas for: {Name}", 
                serviceAreas.Count, $"{employee.FirstName} {employee.LastName}");
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Schedules and service areas configured successfully");
    }

    private async Task SeedRecurrenceDiscountsAsync()
    {
        var discounts = new[]
        {
            new RecurrenceDiscount
            {
                RecurrenceType = RecurrenceType.Weekly,
                DiscountPercent = 0.15m, // 15%
                IsActive = true
            },
            new RecurrenceDiscount
            {
                RecurrenceType = RecurrenceType.BiWeekly,
                DiscountPercent = 0.10m, // 10%
                IsActive = true
            },
            new RecurrenceDiscount
            {
                RecurrenceType = RecurrenceType.Monthly,
                DiscountPercent = 0.05m, // 5%
                IsActive = true
            }
        };

        foreach (var discount in discounts)
        {
            var exists = await _context.RecurrenceDiscounts
                .AnyAsync(rd => rd.RecurrenceType == discount.RecurrenceType);

            if (!exists)
            {
                _context.RecurrenceDiscounts.Add(discount);
                _logger.LogInformation("Recurrence discount created: {Type} - {Percent}%", 
                    discount.RecurrenceType, discount.DiscountPercent);
            }
        }

        await _context.SaveChangesAsync();
    }
}
