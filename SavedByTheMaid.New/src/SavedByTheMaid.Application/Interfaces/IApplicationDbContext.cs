using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for database access.
/// Implemented by Infrastructure's ApplicationDbContext.
/// Exposes only the DbSets and operations needed by Application services.
/// </summary>
public interface IApplicationDbContext
{
    // Employees
    DbSet<Employee> Employees { get; }
    DbSet<EmployeeSchedule> EmployeeSchedules { get; }
    DbSet<EmployeeServiceArea> EmployeeServiceAreas { get; }
    DbSet<EmployeeEquipment> EmployeeEquipment { get; }

    // Service Areas
    DbSet<ServiceAreaZip> ServiceAreaZips { get; }

    // Orders and Appointments
    DbSet<ServiceOrder> ServiceOrders { get; }
    DbSet<ServiceMeet> ServiceMeets { get; }

    // Temporary Reservations
    DbSet<SoftReserve> SoftReserves { get; }

    // Database operations
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
