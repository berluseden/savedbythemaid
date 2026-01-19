using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Servicios
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<AdditionalServiceType> AdditionalServiceTypes => Set<AdditionalServiceType>();
    public DbSet<CleaningPlace> CleaningPlaces => Set<CleaningPlace>();
    public DbSet<CleaningPlaceRoom> CleaningPlaceRooms => Set<CleaningPlaceRoom>();
    public DbSet<RoomServiceType> RoomServiceTypes => Set<RoomServiceType>();

    // Equipamiento
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<ServiceTypeEquipment> ServiceTypeEquipment => Set<ServiceTypeEquipment>();
    public DbSet<EmployeeEquipment> EmployeeEquipment => Set<EmployeeEquipment>();

    // Precios y multiplicadores
    public DbSet<PriceMultiplier> PriceMultipliers => Set<PriceMultiplier>();
    public DbSet<RecurrenceDiscount> RecurrenceDiscounts => Set<RecurrenceDiscount>();

    // Empleados
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSchedule> EmployeeSchedules => Set<EmployeeSchedule>();
    public DbSet<EmployeeTimeOff> EmployeeTimeOffs => Set<EmployeeTimeOff>();

    // Zonas de Servicio
    public DbSet<ServiceArea> ServiceAreas => Set<ServiceArea>();
    public DbSet<ServiceAreaZip> ServiceAreaZips => Set<ServiceAreaZip>();
    public DbSet<EmployeeServiceArea> EmployeeServiceAreas => Set<EmployeeServiceArea>();

    // Órdenes y Citas
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceOrderItem> ServiceOrderItems => Set<ServiceOrderItem>();
    public DbSet<ServiceOrderRoom> ServiceOrderRooms => Set<ServiceOrderRoom>();
    public DbSet<ServiceMeet> ServiceMeets => Set<ServiceMeet>();

    // Reservas Temporales
    public DbSet<SoftReserve> SoftReserves => Set<SoftReserve>();

    // Modelo de Ocupación Anti-Colisión
    public DbSet<SlotOccupancy> SlotOccupancies => Set<SlotOccupancy>();

    // Historial de Estados (Auditoría)
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<MeetStatusHistory> MeetStatusHistories => Set<MeetStatusHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ========== GLOBAL QUERY FILTERS ==========
        
        // Soft delete global filter - excluye automáticamente entidades eliminadas
        builder.Entity<ServiceType>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AdditionalServiceType>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CleaningPlace>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CleaningPlaceRoom>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Equipment>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PriceMultiplier>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<RecurrenceDiscount>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<EmployeeSchedule>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<EmployeeTimeOff>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ServiceArea>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ServiceAreaZip>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<EmployeeServiceArea>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ServiceOrder>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ServiceOrderItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ServiceOrderRoom>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ServiceMeet>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SlotOccupancy>().HasQueryFilter(e => !e.IsDeleted);

        // ========== ÍNDICES ==========

        // ServiceAreaZip - ZipCode único
        builder.Entity<ServiceAreaZip>()
            .HasIndex(z => z.ZipCode)
            .IsUnique();

        // EmployeeServiceArea - índice compuesto único
        builder.Entity<EmployeeServiceArea>()
            .HasIndex(e => new { e.EmployeeId, e.ServiceAreaId })
            .IsUnique();

        // RoomServiceType - índice compuesto único
        builder.Entity<RoomServiceType>()
            .HasIndex(r => new { r.CleaningPlaceRoomId, r.ServiceTypeId })
            .IsUnique();

        // ServiceTypeEquipment - índice compuesto único
        builder.Entity<ServiceTypeEquipment>()
            .HasIndex(s => new { s.ServiceTypeId, s.EquipmentId })
            .IsUnique();

        // EmployeeEquipment - índice compuesto único
        builder.Entity<EmployeeEquipment>()
            .HasIndex(e => new { e.EmployeeId, e.EquipmentId })
            .IsUnique();

        // SoftReserve - índices para performance y anti-colisión
        builder.Entity<SoftReserve>()
            .HasIndex(s => new { s.EmployeeId, s.ScheduledStart, s.ScheduledEnd });

        builder.Entity<SoftReserve>()
            .HasIndex(s => new { s.ExpiresAt, s.Status });

        builder.Entity<SoftReserve>()
            .HasIndex(s => s.SessionId);

        // SlotOccupancy - índice UNIQUE compuesto para anti-colisión (elimina double-booking a nivel DB)
        builder.Entity<SlotOccupancy>()
            .HasIndex(so => new { so.EmployeeId, so.SlotStart })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false"); // Permite reutilizar slots de ocupaciones eliminadas

        // SlotOccupancy - índice para limpieza de expirados
        builder.Entity<SlotOccupancy>()
            .HasIndex(so => new { so.ExpiresAt, so.OccupancyType });

        // SlotOccupancy - índice para búsqueda por referencia
        builder.Entity<SlotOccupancy>()
            .HasIndex(so => new { so.OccupancyType, so.ReferenceId });

        // EmployeeSchedule - índice para búsqueda de disponibilidad
        builder.Entity<EmployeeSchedule>()
            .HasIndex(es => new { es.EmployeeId, es.DayOfWeek });

        // EmployeeTimeOff - índice para búsqueda de bloqueos
        builder.Entity<EmployeeTimeOff>()
            .HasIndex(t => new { t.EmployeeId, t.StartDateTime, t.EndDateTime });

        // ServiceMeet - índices para búsqueda de citas
        builder.Entity<ServiceMeet>()
            .HasIndex(sm => new { sm.AssignedEmployeeId, sm.ScheduledStart });

        builder.Entity<ServiceMeet>()
            .HasIndex(sm => sm.Status);

        builder.Entity<ServiceMeet>()
            .HasIndex(sm => new { sm.ServiceAreaId, sm.ScheduledStart });

        // ServiceOrder - índice para listados admin ordenados por fecha
        builder.Entity<ServiceOrder>()
            .HasIndex(so => so.CreatedAt)
            .IsDescending();

        builder.Entity<ServiceOrder>()
            .HasIndex(so => new { so.OrderStatus, so.CreatedAt })
            .IsDescending();

        // OrderStatusHistory - índices para auditoría
        builder.Entity<OrderStatusHistory>()
            .HasIndex(h => h.ServiceOrderId);

        builder.Entity<OrderStatusHistory>()
            .HasIndex(h => h.ChangedAt)
            .IsDescending();

        // MeetStatusHistory - índices para auditoría
        builder.Entity<MeetStatusHistory>()
            .HasIndex(h => h.ServiceMeetId);

        builder.Entity<MeetStatusHistory>()
            .HasIndex(h => h.ChangedAt)
            .IsDescending();

        // ========== RELACIONES ==========

        // Employee -> Meetings (muchos)
        builder.Entity<Employee>()
            .HasMany(e => e.Meetings)
            .WithOne(m => m.AssignedEmployee)
            .HasForeignKey(m => m.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Employee -> TimeOffs
        builder.Entity<Employee>()
            .HasMany(e => e.TimeOffs)
            .WithOne(t => t.Employee)
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Employee -> Equipment
        builder.Entity<Employee>()
            .HasMany(e => e.Equipment)
            .WithOne(eq => eq.Employee)
            .HasForeignKey(eq => eq.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Employee -> SlotOccupancies (modelo anti-colisión)
        builder.Entity<SlotOccupancy>()
            .HasOne(so => so.Employee)
            .WithMany()
            .HasForeignKey(so => so.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ServiceOrder -> Customer
        builder.Entity<ServiceOrder>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // ServiceOrder -> Rooms
        builder.Entity<ServiceOrder>()
            .HasMany(o => o.Rooms)
            .WithOne(r => r.ServiceOrder)
            .HasForeignKey(r => r.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ServiceMeet -> ServiceArea
        builder.Entity<ServiceMeet>()
            .HasOne(m => m.ServiceArea)
            .WithMany()
            .HasForeignKey(m => m.ServiceAreaId)
            .OnDelete(DeleteBehavior.SetNull);

        // OrderStatusHistory -> ServiceOrder
        builder.Entity<OrderStatusHistory>()
            .HasOne(h => h.ServiceOrder)
            .WithMany()
            .HasForeignKey(h => h.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderStatusHistory -> ChangedBy
        builder.Entity<OrderStatusHistory>()
            .HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.SetNull);

        // MeetStatusHistory -> ServiceMeet
        builder.Entity<MeetStatusHistory>()
            .HasOne(h => h.ServiceMeet)
            .WithMany()
            .HasForeignKey(h => h.ServiceMeetId)
            .OnDelete(DeleteBehavior.Cascade);

        // MeetStatusHistory -> ChangedBy
        builder.Entity<MeetStatusHistory>()
            .HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.SetNull);

        // ========== PRECISIÓN DECIMAL ==========

        builder.Entity<ServiceType>()
            .Property(s => s.Price).HasPrecision(18, 2);
        builder.Entity<ServiceType>()
            .Property(s => s.PricePerBedroom).HasPrecision(18, 2);
        builder.Entity<ServiceType>()
            .Property(s => s.PricePerBathroom).HasPrecision(18, 2);

        builder.Entity<AdditionalServiceType>()
            .Property(s => s.Price).HasPrecision(18, 2);

        builder.Entity<CleaningPlaceRoom>()
            .Property(r => r.BasePrice).HasPrecision(18, 2);

        builder.Entity<RoomServiceType>()
            .Property(r => r.BasePriceOverride).HasPrecision(18, 2);

        builder.Entity<PriceMultiplier>()
            .Property(p => p.Factor).HasPrecision(10, 4);
        builder.Entity<PriceMultiplier>()
            .Property(p => p.MinValue).HasPrecision(18, 2);
        builder.Entity<PriceMultiplier>()
            .Property(p => p.MaxValue).HasPrecision(18, 2);

        builder.Entity<RecurrenceDiscount>()
            .Property(r => r.DiscountPercent).HasPrecision(5, 4);

        builder.Entity<ServiceOrder>()
            .Property(o => o.Subtotal).HasPrecision(18, 2);
        builder.Entity<ServiceOrder>()
            .Property(o => o.Tax).HasPrecision(18, 2);
        builder.Entity<ServiceOrder>()
            .Property(o => o.Discount).HasPrecision(18, 2);
        builder.Entity<ServiceOrder>()
            .Property(o => o.Total).HasPrecision(18, 2);

        builder.Entity<ServiceOrderItem>()
            .Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Entity<ServiceOrderItem>()
            .Property(i => i.Total).HasPrecision(18, 2);

        builder.Entity<ServiceOrderRoom>()
            .Property(r => r.CalculatedPrice).HasPrecision(18, 2);

        builder.Entity<ServiceMeet>()
            .Property(m => m.AdjustmentAmount).HasPrecision(18, 2);
        builder.Entity<ServiceMeet>()
            .Property(m => m.CheckInLatitude).HasPrecision(10, 7);
        builder.Entity<ServiceMeet>()
            .Property(m => m.CheckInLongitude).HasPrecision(10, 7);
        builder.Entity<ServiceMeet>()
            .Property(m => m.CheckOutLatitude).HasPrecision(10, 7);
        builder.Entity<ServiceMeet>()
            .Property(m => m.CheckOutLongitude).HasPrecision(10, 7);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Common.BaseEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Domain.Common.BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
