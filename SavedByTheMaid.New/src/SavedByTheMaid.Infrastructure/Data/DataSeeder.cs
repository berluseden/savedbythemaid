using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Infrastructure.Data;

/// <summary>
/// Seeds all master/reference data required by the SavedByTheMaid platform.
/// <para>
/// <strong>Idempotent:</strong> Every method uses a check-before-insert pattern,
/// so <see cref="SeedAllAsync"/> is safe to call on every application start-up
/// without duplicating rows.
/// </para>
/// <para>Seeded data categories (in order):</para>
/// <list type="number">
///   <item>Additional database tables (SlotOccupancies, OrderStatusHistories, MeetStatusHistories)</item>
///   <item>Duplicate cleanup for legacy data</item>
///   <item>Service types (Regular, Deep, Move In/Out, Post-Construction)</item>
///   <item>Cleaning place types and their rooms (House/Apartment, Commercial Office, Airbnb)</item>
///   <item>Additional/add-on service types (Oven, Refrigerator, Windows, etc.)</item>
///   <item>Service areas with ZIP codes (Miami-Dade County)</item>
///   <item>Equipment catalog</item>
///   <item>Sample employees (development/testing only)</item>
///   <item>Employee schedules and service area assignments</item>
///   <item>Recurrence discount tiers (Weekly, BiWeekly, Monthly)</item>
/// </list>
/// <para>
/// <strong>Note:</strong> Admin user seeding is handled separately in Program.cs
/// via <c>SeedAdminUserAsync</c>. Ensure the <c>AdminSeed:Password</c> configuration
/// value is set to a strong password in production environments.
/// </para>
/// </summary>
public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DataSeeder"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    /// <param name="logger">Logger for reporting seed progress and errors.</param>
    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Executes all seed operations in the correct dependency order.
    /// Each step is idempotent -- existing records are skipped, and only
    /// missing data is inserted. A summary of created vs. skipped items
    /// is logged at the end.
    /// </summary>
    public async Task SeedAllAsync()
    {
        _logger.LogInformation("Starting master data seed...");

        var created = 0;
        var skipped = 0;

        try
        {
            // ── 0. Schema: ensure additional tables exist ────────────────
            await EnsureAdditionalTablesAsync();

            // ── 1. Cleanup: remove duplicate/obsolete legacy data ────────
            await CleanDuplicatesAsync();

            // ── 2. Service Types ─────────────────────────────────────────
            var (c, s) = await SeedServiceTypesAsync();
            created += c; skipped += s;

            // ── 3. Cleaning Places and Rooms ─────────────────────────────
            (c, s) = await SeedCleaningPlacesAsync();
            created += c; skipped += s;

            // ── 4. Additional (add-on) Services ──────────────────────────
            (c, s) = await SeedAdditionalServicesAsync();
            created += c; skipped += s;

            // ── 5. Service Areas and ZIP Codes ───────────────────────────
            (c, s) = await SeedServiceAreasAsync();
            created += c; skipped += s;

            // ── 6. Equipment ─────────────────────────────────────────────
            (c, s) = await SeedEquipmentAsync();
            created += c; skipped += s;

            // ── 7. Sample Employees ──────────────────────────────────────
            (c, s) = await SeedEmployeesAsync();
            created += c; skipped += s;

            // ── 8. Employee Schedules and Service Area assignments ───────
            await SeedEmployeeSchedulesAsync();

            // ── 9. Recurrence Discounts ──────────────────────────────────
            (c, s) = await SeedRecurrenceDiscountsAsync();
            created += c; skipped += s;

            _logger.LogInformation(
                "Master data seed completed successfully. Created: {Created}, Skipped (already existed): {Skipped}",
                created, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seed");
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Schema helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates additional MySQL tables that are not managed by EF Core migrations.
    /// Uses <c>CREATE TABLE IF NOT EXISTS</c> so the operation is fully idempotent.
    /// Tables created:
    /// <list type="bullet">
    ///   <item><c>SlotOccupancies</c> -- anti-collision tracking for employee time slots</item>
    ///   <item><c>OrderStatusHistories</c> -- audit trail for service order status changes</item>
    ///   <item><c>MeetStatusHistories</c> -- audit trail for appointment status changes</item>
    /// </list>
    /// </summary>
    private async Task EnsureAdditionalTablesAsync()
    {
        _logger.LogInformation("Checking additional tables...");

        // SlotOccupancies -- Anti-collision tracking for employee time slots
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

        // OrderStatusHistories -- Audit trail for service order status changes
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

        // MeetStatusHistories -- Audit trail for appointment status changes
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

        // Ensure new columns exist on SoftReserves (added after initial schema)
        try
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE SoftReserves ADD COLUMN ExtensionCount INT NOT NULL DEFAULT 0;
            ");
            _logger.LogInformation("Added ExtensionCount column to SoftReserves");
        }
        catch (MySql.Data.MySqlClient.MySqlException ex) when (ex.Message.Contains("Duplicate column"))
        {
            // Column already exists — nothing to do
        }

        _logger.LogInformation("Additional tables verified/created");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Legacy data cleanup
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes legacy duplicate data that may have been introduced before
    /// idempotency guards were added. Specifically:
    /// <list type="bullet">
    ///   <item>Deletes obsolete admin users that lack a <c>FirstName</c></item>
    ///   <item>Deduplicates <see cref="ServiceArea"/> records by name (keeps the first)</item>
    /// </list>
    /// </summary>
    private async Task CleanDuplicatesAsync()
    {
        _logger.LogInformation("Cleaning duplicates...");

        // Remove obsolete admin user without FirstName (legacy artifact)
        var obsoleteAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "admin@savedbythemaid.com" && u.FirstName == null);
        if (obsoleteAdmin != null)
        {
            _context.Users.Remove(obsoleteAdmin);
            _logger.LogInformation("Obsolete admin user deleted");
        }

        // Deduplicate ServiceAreas by name -- keep the first occurrence, remove duplicates
        var allAreas = await _context.ServiceAreas.ToListAsync();
        var duplicateGroups = allAreas
            .GroupBy(sa => sa.Name)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var toRemove = group.Skip(1).ToList();
            _context.ServiceAreas.RemoveRange(toRemove);
            _logger.LogInformation("Deleted {Count} duplicate areas for '{Name}'", toRemove.Count, group.Key);
        }

        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Service Types
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the four core service types offered by the platform:
    /// Regular Cleaning, Deep Cleaning, Move In/Out, and Post-Construction.
    /// Each type includes base pricing, per-room pricing, and time estimates.
    /// </summary>
    /// <returns>Tuple of (created count, skipped count).</returns>
    private async Task<(int created, int skipped)> SeedServiceTypesAsync()
    {
        int created = 0, skipped = 0;

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
                created++;
            }
            else
            {
                skipped++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Service types: {Created} created, {Skipped} already existed", created, skipped);
        return (created, skipped);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Cleaning Places and Rooms
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds property types (House/Apartment, Commercial Office, Airbnb/Vacation Rental)
    /// along with the room types specific to each property type. Room definitions include
    /// base cleaning time and base price for scheduling and pricing calculations.
    /// </summary>
    /// <returns>Tuple of (created count, skipped count).</returns>
    private async Task<(int created, int skipped)> SeedCleaningPlacesAsync()
    {
        int created = 0, skipped = 0;

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
                await _context.SaveChangesAsync(); // Flush to get the auto-generated Id
                _logger.LogInformation("Property type created: {Name}", place.Name);
                created++;
            }
            else
            {
                skipped++;
            }

            // Add rooms that don't already exist for this place
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
                    created++;
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleaning places seeded: {Created} created, {Skipped} already existed", created, skipped);
        return (created, skipped);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Additional (add-on) Services
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds optional add-on services that customers can select during booking
    /// (e.g., Oven Cleaning, Window Cleaning, Laundry). Each includes pricing
    /// and additional time estimates.
    /// </summary>
    /// <returns>Tuple of (created count, skipped count).</returns>
    private async Task<(int created, int skipped)> SeedAdditionalServicesAsync()
    {
        int created = 0, skipped = 0;

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
                created++;
            }
            else
            {
                skipped++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Additional services: {Created} created, {Skipped} already existed", created, skipped);
        return (created, skipped);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Service Areas and ZIP Codes
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds service areas covering Miami-Dade County with their associated ZIP codes.
    /// Areas include: Miami Beach, Downtown Miami, Brickell, Coral Gables, Coconut Grove,
    /// Wynwood/Midtown, Little Havana, Aventura, North Miami, Kendall, Doral, and Homestead.
    /// <para>
    /// ZIP codes are checked globally to prevent the same ZIP from being assigned to
    /// multiple areas (first area wins).
    /// </para>
    /// </summary>
    /// <returns>Tuple of (created count, skipped count).</returns>
    private async Task<(int created, int skipped)> SeedServiceAreasAsync()
    {
        int created = 0, skipped = 0;

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
                await _context.SaveChangesAsync(); // Flush to get the auto-generated Id
                _logger.LogInformation("Service area created: {Name}", area.Name);
                created++;
            }
            else
            {
                skipped++;
            }

            // Add ZIP codes only if not already globally assigned (prevents duplicates across areas)
            foreach (var zipCode in areaData.ZipCodes)
            {
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
                    created++;
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Service areas and ZIP codes: {Created} created, {Skipped} already existed", created, skipped);
        return (created, skipped);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Equipment
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the equipment catalog used for tracking specialized cleaning tools
    /// (e.g., Industrial Vacuum, Steam Mop, Window Cleaning Kit).
    /// </summary>
    /// <returns>Tuple of (created count, skipped count).</returns>
    private async Task<(int created, int skipped)> SeedEquipmentAsync()
    {
        int created = 0, skipped = 0;

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
                created++;
            }
            else
            {
                skipped++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Equipment: {Created} created, {Skipped} already existed", created, skipped);
        return (created, skipped);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Sample Employees (development/testing)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds sample employees for development and testing environments.
    /// Skipped entirely if 3 or more employees already exist.
    /// Each employee is checked by email before insertion to prevent duplicates.
    /// </summary>
    /// <returns>Tuple of (created count, skipped count).</returns>
    private async Task<(int created, int skipped)> SeedEmployeesAsync()
    {
        int created = 0, skipped = 0;

        // Only create sample employees if fewer than 3 exist
        var employeeCount = await _context.Employees.CountAsync();
        if (employeeCount >= 3)
        {
            _logger.LogInformation("Employees already exist ({Count}), skipping sample employee seed", employeeCount);
            return (0, 3);
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
                created++;
            }
            else
            {
                skipped++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Employees: {Created} created, {Skipped} already existed", created, skipped);
        return (created, skipped);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Employee Schedules and Service Area Assignments
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns default Mon-Fri 8:00 AM - 5:00 PM schedules to all employees
    /// and links each employee to all active service areas. Existing schedule
    /// entries and area assignments are preserved (only missing ones are added).
    /// </summary>
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

        // Get all active service areas for assignment
        var serviceAreas = await _context.ServiceAreas
            .Where(sa => !sa.IsDeleted)
            .ToListAsync();

        foreach (var employee in employees)
        {
            // Create schedules for Monday (1) through Friday (5), 8 AM - 5 PM
            for (int day = 1; day <= 5; day++)
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

            // Assign all active service areas to the employee
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

    // ──────────────────────────────────────────────────────────────────────
    //  Recurrence Discounts
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds discount tiers for recurring bookings:
    /// <list type="bullet">
    ///   <item>Weekly: 15% discount</item>
    ///   <item>BiWeekly: 10% discount</item>
    ///   <item>Monthly: 5% discount</item>
    /// </list>
    /// </summary>
    /// <returns>Tuple of (created count, skipped count).</returns>
    private async Task<(int created, int skipped)> SeedRecurrenceDiscountsAsync()
    {
        int created = 0, skipped = 0;

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
                    discount.RecurrenceType, discount.DiscountPercent * 100);
                created++;
            }
            else
            {
                skipped++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Recurrence discounts: {Created} created, {Skipped} already existed", created, skipped);
        return (created, skipped);
    }
}
