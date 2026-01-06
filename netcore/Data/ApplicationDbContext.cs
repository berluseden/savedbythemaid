using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using netcore.Models;

namespace netcore.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Claves compuestas
            builder.Entity<CleaningPlaceCleaningPlaceRoom>()
                .HasKey(c => new { c.Id });

            builder.Entity<CleaningPlaceRoomServiceType>()
                .HasKey(c => new { c.CleaningPlaceRoomServiceTypeId });

            // Índice único para ZipCode en ServiceAreaZips
            builder.Entity<ServiceAreaZip>()
                .HasIndex(z => z.ZipCode)
                .IsUnique();

            // Índice compuesto para EmployeeServiceArea
            builder.Entity<EmployeeServiceArea>()
                .HasIndex(e => new { e.EmployeeId, e.ServiceAreaId })
                .IsUnique();

            // Índices para SoftReserves (performance)
            builder.Entity<SoftReserve>()
                .HasIndex(s => new { s.EmployeeId, s.ScheduledStart, s.ScheduledEnd });

            builder.Entity<SoftReserve>()
                .HasIndex(s => new { s.ExpiresAt, s.Status });
        }

        // Identity
        public DbSet<ApplicationUser> ApplicationUser { get; set; }

        // Servicios de Limpieza
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<AdditionalServiceType> AdditionalServiceTypes { get; set; }
        public DbSet<CleaningPlace> CleaningPlaces { get; set; }
        public DbSet<CleaningPlaceRoom> CleaningPlaceRooms { get; set; }
        public DbSet<CleaningPlaceCleaningPlaceRoom> CleaningPlaceCleaningPlaceRooms { get; set; }
        public DbSet<CleaningPlaceRoomServiceType> CleaningPlaceRoomServiceTypes { get; set; }

        // Empleados
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }
        public DbSet<Schedule> Schedule { get; set; }
        public DbSet<User> Users { get; set; }

        // Zonas de Servicio (MVP)
        public DbSet<ServiceArea> ServiceAreas { get; set; }
        public DbSet<ServiceAreaZip> ServiceAreaZips { get; set; }
        public DbSet<EmployeeServiceArea> EmployeeServiceAreas { get; set; }

        // Ordenes y Citas
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<ServiceOrderContactInfo> ServiceContactsInfo { get; set; }
        public DbSet<ServiceOrderAdditionalService> ServiceOrderAdditionalServices { get; set; }
        public DbSet<ServiceMeet> ServiceMeeting { get; set; }
        public DbSet<EmployeeMeetService> EmployeesMeetingServices { get; set; }

        // Reservas Temporales (MVP)
        public DbSet<SoftReserve> SoftReserves { get; set; }

        // Carrito
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Disponibilidad
        public DbSet<AvaliableMaid> AvaliableMaids { get; set; }
        public DbSet<AvaliableMaidHour> AvaliableMaidHours { get; set; }
    }
}
