using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Api.Tests.Helpers;
using SavedByTheMaid.Infrastructure.Data;
using Serilog;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Factory with full seeded master data (service areas, employees, orders, etc.)
/// for tests that need a realistic data set.
/// </summary>
public class SeededWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<Serilog.ILogger>();
            services.RemoveAll<Serilog.IDiagnosticContext>();
            services.AddLogging(lb => lb.ClearProviders().AddConsole());

            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                           d.ServiceType == typeof(ApplicationDbContext))
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            var dbName = $"SeededTestDb_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            TestDataSeeder.SeedAll(db);
        });
    }
}
