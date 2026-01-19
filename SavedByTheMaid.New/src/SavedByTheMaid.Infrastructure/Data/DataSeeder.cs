using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Infrastructure.Data;

/// <summary>
/// Seed completo de datos maestros para MVP
/// Idempotente: puede ejecutarse múltiples veces sin duplicar
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
        _logger.LogInformation("Iniciando seed de datos maestros...");

        try
        {
            // 0. Crear tablas adicionales si no existen (SlotOccupancy, StatusHistory)
            await EnsureAdditionalTablesAsync();

            // 1. Limpiar duplicados
            await CleanDuplicatesAsync();

            // 2. Service Types
            await SeedServiceTypesAsync();

            // 3. Cleaning Places y Rooms
            await SeedCleaningPlacesAsync();

            // 4. Additional Services
            await SeedAdditionalServicesAsync();

            // 5. Service Areas y ZIP Codes
            await SeedServiceAreasAsync();

            // 6. Equipment
            await SeedEquipmentAsync();

            // 7. Employees de ejemplo
            await SeedEmployeesAsync();

            // 8. Employee Schedules y Service Areas
            await SeedEmployeeSchedulesAsync();

            // 9. Recurrence Discounts
            await SeedRecurrenceDiscountsAsync();

            _logger.LogInformation("Seed de datos maestros completado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el seed de datos");
            throw;
        }
    }

    /// <summary>
    /// Crea tablas adicionales (SlotOccupancy, StatusHistory) si no existen.
    /// Esto es idempotente y seguro para ejecutar múltiples veces.
    /// </summary>
    private async Task EnsureAdditionalTablesAsync()
    {
        _logger.LogInformation("Verificando tablas adicionales...");

        // SlotOccupancies - Anti-colisión
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

        // OrderStatusHistories - Auditoría de órdenes
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

        // MeetStatusHistories - Auditoría de citas
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

        _logger.LogInformation("Tablas adicionales verificadas/creadas");
    }

    private async Task CleanDuplicatesAsync()
    {
        _logger.LogInformation("Limpiando duplicados...");

        // Eliminar usuario admin obsoleto sin FirstName
        var obsoleteAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "admin@savedbythemaid.com" && u.FirstName == null);
        if (obsoleteAdmin != null)
        {
            _context.Users.Remove(obsoleteAdmin);
            _logger.LogInformation("Usuario admin obsoleto eliminado");
        }

        // Eliminar ServiceAreas duplicadas - cargar en memoria primero
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
            _logger.LogInformation("Eliminadas {Count} áreas duplicadas de '{Name}'", toRemove.Count, group.Key);
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedServiceTypesAsync()
    {
        var serviceTypes = new[]
        {
            new ServiceType
            {
                Name = "Limpieza Regular",
                Description = "Limpieza estándar de mantenimiento semanal o quincenal",
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
                Name = "Limpieza Profunda",
                Description = "Limpieza detallada a fondo incluyendo áreas difíciles",
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
                Name = "Mudanza (Move In/Out)",
                Description = "Limpieza completa para mudanzas o entrega de propiedad",
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
                Name = "Post-Construcción",
                Description = "Limpieza después de remodelación o construcción",
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
                _logger.LogInformation("Tipo de servicio creado: {Name}", serviceType.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedCleaningPlacesAsync()
    {
        var places = new[]
        {
            new { Name = "Casa/Apartamento", Description = "Residencia unifamiliar o apartamento", Rooms = new[] {
                ("Recámara", "Limpieza de dormitorio incluyendo cama y closet", 25, 15.00m),
                ("Baño", "Limpieza completa de sanitarios, ducha, lavabo", 20, 10.00m),
                ("Sala", "Limpieza de área de estar y muebles", 20, 12.00m),
                ("Cocina", "Limpieza de cocina, electrodomésticos, gabinetes", 30, 18.00m),
                ("Comedor", "Limpieza de área de comedor", 15, 10.00m),
                ("Oficina", "Limpieza de oficina en casa o estudio", 20, 12.00m),
                ("Garaje", "Limpieza de garaje o estacionamiento", 25, 15.00m)
            }},
            new { Name = "Oficina Comercial", Description = "Espacio de oficina o comercio", Rooms = new[] {
                ("Área de Trabajo", "Limpieza de escritorios y áreas de trabajo", 20, 12.00m),
                ("Sala de Reuniones", "Limpieza de salas de conferencias", 20, 15.00m),
                ("Baño Comercial", "Limpieza de sanitarios comerciales", 25, 12.00m),
                ("Cocina/Break Room", "Limpieza de área de descanso", 25, 15.00m),
                ("Recepción", "Limpieza de área de recepción", 15, 10.00m),
                ("Almacén", "Limpieza de área de almacenamiento", 20, 12.00m)
            }},
            new { Name = "Airbnb/Vacation Rental", Description = "Propiedad de renta vacacional", Rooms = new[] {
                ("Recámara", "Limpieza profunda post-huésped", 30, 18.00m),
                ("Baño", "Sanitización completa post-huésped", 25, 15.00m),
                ("Sala/Living", "Limpieza y reorganización", 25, 15.00m),
                ("Cocina", "Limpieza y reabastecimiento", 35, 20.00m),
                ("Área Exterior", "Limpieza de patio o balcón", 20, 12.00m)
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
                _logger.LogInformation("Tipo de inmueble creado: {Name}", place.Name);
            }

            // Agregar rooms
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
                Title = "Limpieza de Horno",
                Description = "Limpieza profunda interior del horno",
                Price = 25.00m,
                AdditionalMinutes = 30,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Limpieza de Refrigerador",
                Description = "Limpieza interior completa del refrigerador",
                Price = 30.00m,
                AdditionalMinutes = 40,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Interior de Gabinetes",
                Description = "Limpieza interior de gabinetes de cocina",
                Price = 40.00m,
                AdditionalMinutes = 45,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Limpieza de Ventanas",
                Description = "Limpieza interior y exterior de ventanas",
                Price = 50.00m,
                AdditionalMinutes = 60,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Lavandería",
                Description = "Lavado, secado y doblado de ropa",
                Price = 35.00m,
                AdditionalMinutes = 90,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Organización de Closet",
                Description = "Organización y limpieza de armarios",
                Price = 45.00m,
                AdditionalMinutes = 60,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Limpieza de Alfombras",
                Description = "Limpieza profunda de alfombras y tapetes",
                Price = 60.00m,
                AdditionalMinutes = 75,
                IsActive = true
            },
            new AdditionalServiceType
            {
                Title = "Limpieza de Paredes",
                Description = "Limpieza y manchas en paredes",
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
                _logger.LogInformation("Servicio adicional creado: {Title}", service.Title);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedServiceAreasAsync()
    {
        // Áreas de Miami con ZIPs completos
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
                    Description = $"Área de servicio en {areaData.Name}",
                    IsActive = true
                };
                _context.ServiceAreas.Add(area);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Área de servicio creada: {Name}", area.Name);
            }

            // Agregar ZIP codes (solo si no existe globalmente para evitar duplicados)
            foreach (var zipCode in areaData.ZipCodes)
            {
                // Verificar si el ZIP ya existe en CUALQUIER área
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
        _logger.LogInformation("Service Areas y ZIPs de Miami seed completado");
    }

    private async Task SeedEquipmentAsync()
    {
        var equipment = new[]
        {
            new Equipment
            {
                Name = "Aspiradora Industrial",
                Description = "Aspiradora de alta potencia para uso comercial",
                IsActive = true
            },
            new Equipment
            {
                Name = "Mopa de Vapor",
                Description = "Sistema de limpieza con vapor para pisos",
                IsActive = true
            },
            new Equipment
            {
                Name = "Kit de Limpieza de Ventanas",
                Description = "Herramientas especializadas para limpieza de ventanas",
                IsActive = true
            },
            new Equipment
            {
                Name = "Lavadora de Alfombras",
                Description = "Máquina para limpieza profunda de alfombras",
                IsActive = true
            },
            new Equipment
            {
                Name = "Kit Eco-Friendly",
                Description = "Productos de limpieza ecológicos y biodegradables",
                IsActive = true
            },
            new Equipment
            {
                Name = "Escalera Telescópica",
                Description = "Escalera profesional para áreas altas",
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
                _logger.LogInformation("Equipamiento creado: {Name}", item.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedEmployeesAsync()
    {
        // Solo crear empleados de ejemplo si no existen
        var employeeCount = await _context.Employees.CountAsync();
        if (employeeCount >= 3)
        {
            _logger.LogInformation("Ya existen empleados, saltando seed de empleados de ejemplo");
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
                _logger.LogInformation("Empleado creado: {Name}", $"{employee.FirstName} {employee.LastName}");
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedEmployeeSchedulesAsync()
    {
        _logger.LogInformation("Configurando horarios y áreas de servicio para empleados...");

        var employees = await _context.Employees
            .Include(e => e.Schedules)
            .Include(e => e.ServiceAreas)
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        if (!employees.Any())
        {
            _logger.LogWarning("No hay empleados para configurar horarios");
            return;
        }

        // Obtener todas las áreas de servicio activas
        var serviceAreas = await _context.ServiceAreas
            .Where(sa => !sa.IsDeleted)
            .ToListAsync();

        foreach (var employee in employees)
        {
            // Crear horarios para lunes a viernes (8am - 5pm)
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

            // Asignar TODAS las áreas de servicio a cada empleada
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

            _logger.LogInformation("Configurado horario y {Count} áreas para: {Name}", 
                serviceAreas.Count, $"{employee.FirstName} {employee.LastName}");
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Horarios y áreas de servicio configurados exitosamente");
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
                _logger.LogInformation("Descuento de recurrencia creado: {Type} - {Percent}%", 
                    discount.RecurrenceType, discount.DiscountPercent);
            }
        }

        await _context.SaveChangesAsync();
    }
}
