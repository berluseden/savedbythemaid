using JempSoft.Core.Models;
using JempSoft.Core.Models.Administration;
using JempSoft.Core.Models.Invent;
using JempSoft.Core.Models.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace JempSoft.Core.Data
{
    public class JempSoftDbContext : IdentityDbContext<ApplicationUser>
    {
        //public JempSoftDbContext()
        //{

        //}

        public JempSoftDbContext(DbContextOptions<JempSoftDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
        public DbSet<ApplicationUser> ApplicationUser { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.Branch> Branch { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.Warehouse> Warehouse { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.Product> Product { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.Vendor> Vendor { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.VendorLine> VendorLine { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.PurchaseOrder> PurchaseOrder { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.PurchaseOrderLine> PurchaseOrderLine { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.Customer> Customer { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.CustomerLine> CustomerLine { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.SalesOrder> SalesOrder { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.SalesOrderLine> SalesOrderLine { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.Shipment> Shipment { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.ShipmentLine> ShipmentLine { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.Receiving> Receiving { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.ReceivingLine> ReceivingLine { get; set; }

        public DbSet<JempSoft.Core.Models.Invent.TransferOrder> TransferOrder { get; set; }

        public DbSet<TransferOrderLine> TransferOrderLine { get; set; }

        public DbSet<TransferOut> TransferOut { get; set; }

        public DbSet<TransferOutLine> TransferOutLine { get; set; }

        public DbSet<TransferIn> TransferIn { get; set; }

        public DbSet<TransferInLine> TransferInLine { get; set; }




        public DbSet<ServiceType> ServiceTypes { get; set; }

        public DbSet<AdditionalServiceType> AdditionalServiceTypes { get; set; }

        public DbSet<CleaningPlace> CleaningPlaces { get; set; }

        public DbSet<CleaningPlaceRoom> CleaningPlaceRooms { get; set; }

        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<CleaningPlaceCleaningPlaceRoom> CleaningPlaceCleaningPlaceRooms { get; set; }
        public DbSet<CleaningPlaceRoomServiceType> CleaningPlaceRoomServiceTypes { get; set; }

        public DbSet<ServiceOrderContactInfo> ServiceContactsInfo { get; set; }

        public DbSet<ServiceOrder> ServiceOrders { get; set; }

        public DbSet<ServiceOrderAdditionalService> ServiceOrderAdditionalServices { get; set; }


        public DbSet<Schedule> Schedule { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }

        public DbSet<ServiceMeet> ServiceMeeting { get; set; }

        public DbSet<EmployeeMeetService> EmployeesMeetingServices { get; set; }

        public DbSet<AvaliableMaid> AvaliableMaids { get; set; }

        public DbSet<AvaliableMaidHour> AvaliableMaidHours { get; set; }


    }
}
