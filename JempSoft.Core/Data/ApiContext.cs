using JempSoft.Core.Api.Database.Entity;
using JempSoft.Core.Api.Database.Entity.Threats;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JempSoft.Core.Api.Database
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options) { }


        public ApiContext(DbContextOptions options) : base(options) { }
        public DbSet<Threat> Threats { get; set; }
        public DbSet<ThreatType> ThreatTypes { get; set; }
        public DbSet<Status> Status { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.HasDefaultSchema(schema: "Portal");
            modelBuilder.Entity("ThreatTypes");
            modelBuilder.Entity("Threats");
            modelBuilder.Entity("Status");
            base.OnModelCreating(modelBuilder);


        }

        public override int SaveChanges()
        {
            AddAuitInfo();
            return base.SaveChanges();
        }

        public async Task SaveChangesAsync()
        {
            AddAuitInfo();
            await base.SaveChangesAsync();
        }

        private void AddAuitInfo()
        {
            var entries = ChangeTracker.Entries().Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    ((BaseEntity)entry.Entity).Created = DateTime.UtcNow;
                }
            ((BaseEntity)entry.Entity).Modified = DateTime.UtcNow;
            }
        }

    }
}