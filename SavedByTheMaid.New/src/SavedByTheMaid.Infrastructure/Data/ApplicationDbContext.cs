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

    // Services
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<AdditionalServiceType> AdditionalServiceTypes => Set<AdditionalServiceType>();
    public DbSet<CleaningPlace> CleaningPlaces => Set<CleaningPlace>();
    public DbSet<CleaningPlaceRoom> CleaningPlaceRooms => Set<CleaningPlaceRoom>();
    public DbSet<RoomServiceType> RoomServiceTypes => Set<RoomServiceType>();

    // Equipment
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<ServiceTypeEquipment> ServiceTypeEquipment => Set<ServiceTypeEquipment>();
    public DbSet<EmployeeEquipment> EmployeeEquipment => Set<EmployeeEquipment>();

    // Prices and multipliers
    public DbSet<PriceMultiplier> PriceMultipliers => Set<PriceMultiplier>();
    public DbSet<RecurrenceDiscount> RecurrenceDiscounts => Set<RecurrenceDiscount>();

    // Employees
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSchedule> EmployeeSchedules => Set<EmployeeSchedule>();
    public DbSet<EmployeeTimeOff> EmployeeTimeOffs => Set<EmployeeTimeOff>();

    // Service Areas
    public DbSet<ServiceArea> ServiceAreas => Set<ServiceArea>();
    public DbSet<ServiceAreaZip> ServiceAreaZips => Set<ServiceAreaZip>();
    public DbSet<EmployeeServiceArea> EmployeeServiceAreas => Set<EmployeeServiceArea>();

    // Orders and Appointments
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceOrderItem> ServiceOrderItems => Set<ServiceOrderItem>();
    public DbSet<ServiceOrderRoom> ServiceOrderRooms => Set<ServiceOrderRoom>();
    public DbSet<ServiceMeet> ServiceMeets => Set<ServiceMeet>();

    // Temporary Reservations
    public DbSet<SoftReserve> SoftReserves => Set<SoftReserve>();

    // Anti-Collision Occupancy Model
    public DbSet<SlotOccupancy> SlotOccupancies => Set<SlotOccupancy>();

    // Status History (Audit)
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<MeetStatusHistory> MeetStatusHistories => Set<MeetStatusHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ========== GLOBAL QUERY FILTERS ==========
        
        // Soft delete global filter - automatically excludes deleted entities
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
        builder.Entity<SoftReserve>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SlotOccupancy>().HasQueryFilter(e => !e.IsDeleted);

        // ========== INDEXES ==========

        // ServiceAreaZip - unique ZipCode
        builder.Entity<ServiceAreaZip>()
            .HasIndex(z => z.ZipCode)
            .IsUnique();

        // EmployeeServiceArea - unique composite index
        builder.Entity<EmployeeServiceArea>()
            .HasIndex(e => new { e.EmployeeId, e.ServiceAreaId })
            .IsUnique();

        // RoomServiceType - unique composite index
        builder.Entity<RoomServiceType>()
            .HasIndex(r => new { r.CleaningPlaceRoomId, r.ServiceTypeId })
            .IsUnique();

        // ServiceTypeEquipment - unique composite index
        builder.Entity<ServiceTypeEquipment>()
            .HasIndex(s => new { s.ServiceTypeId, s.EquipmentId })
            .IsUnique();

        // EmployeeEquipment - unique composite index
        builder.Entity<EmployeeEquipment>()
            .HasIndex(e => new { e.EmployeeId, e.EquipmentId })
            .IsUnique();

        // SoftReserve - indexes for performance and anti-collision
        builder.Entity<SoftReserve>()
            .HasIndex(s => new { s.EmployeeId, s.ScheduledStart, s.ScheduledEnd });

        builder.Entity<SoftReserve>()
            .HasIndex(s => new { s.ExpiresAt, s.Status });

        builder.Entity<SoftReserve>()
            .HasIndex(s => s.SessionId);

        // SlotOccupancy - UNIQUE composite index for anti-collision (prevents double-booking at the DB level)
        builder.Entity<SlotOccupancy>()
            .HasIndex(so => new { so.EmployeeId, so.SlotStart })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false"); // Allows reusing slots from deleted occupancies

        // SlotOccupancy - index for expired entry cleanup
        builder.Entity<SlotOccupancy>()
            .HasIndex(so => new { so.ExpiresAt, so.OccupancyType });

        // SlotOccupancy - index for reference lookup
        builder.Entity<SlotOccupancy>()
            .HasIndex(so => new { so.OccupancyType, so.ReferenceId });

        // EmployeeSchedule - index for availability lookup
        builder.Entity<EmployeeSchedule>()
            .HasIndex(es => new { es.EmployeeId, es.DayOfWeek });

        // EmployeeTimeOff - index for time-off lookup
        builder.Entity<EmployeeTimeOff>()
            .HasIndex(t => new { t.EmployeeId, t.StartDateTime, t.EndDateTime });

        // ServiceMeet - indexes for appointment lookup
        builder.Entity<ServiceMeet>()
            .HasIndex(sm => new { sm.AssignedEmployeeId, sm.ScheduledStart });

        builder.Entity<ServiceMeet>()
            .HasIndex(sm => sm.Status);

        builder.Entity<ServiceMeet>()
            .HasIndex(sm => new { sm.ServiceAreaId, sm.ScheduledStart });

        // ServiceOrder - index for admin listings sorted by date
        builder.Entity<ServiceOrder>()
            .HasIndex(so => so.CreatedAt)
            .IsDescending();

        builder.Entity<ServiceOrder>()
            .HasIndex(so => new { so.OrderStatus, so.CreatedAt })
            .IsDescending();

        // OrderStatusHistory - indexes for audit
        builder.Entity<OrderStatusHistory>()
            .HasIndex(h => h.ServiceOrderId);

        builder.Entity<OrderStatusHistory>()
            .HasIndex(h => h.ChangedAt)
            .IsDescending();

        // MeetStatusHistory - indexes for audit
        builder.Entity<MeetStatusHistory>()
            .HasIndex(h => h.ServiceMeetId);

        builder.Entity<MeetStatusHistory>()
            .HasIndex(h => h.ChangedAt)
            .IsDescending();

        // ========== RELATIONSHIPS ==========

        // Employee -> Meetings (many)
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

        // Employee -> SlotOccupancies (anti-collision model)
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

        // ========== DECIMAL PRECISION ==========

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
