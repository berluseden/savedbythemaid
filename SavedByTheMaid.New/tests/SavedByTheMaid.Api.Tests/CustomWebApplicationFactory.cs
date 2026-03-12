using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Infrastructure.Data;
using Serilog;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Custom factory for integration tests.
///
/// BEST PRACTICE:
/// 1. Inherits from WebApplicationFactory&lt;Program&gt;
/// 2. Overrides ConfigureWebHost (not ConfigureServices)
/// 3. Replaces the real DbContext with InMemory
/// 4. Sets the environment to "Testing"
/// 5. Used with IClassFixture to share across tests
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Set the Testing environment BEFORE Program.cs reads the variable
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 2. Remove Serilog services to avoid "logger is already frozen"
            services.RemoveAll<Serilog.ILogger>();
            services.RemoveAll<Serilog.IDiagnosticContext>();
            services.AddLogging(lb => lb.ClearProviders().AddConsole());

            // 3. Remove the production DbContext (MySQL)
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                           d.ServiceType == typeof(ApplicationDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // 4. Add DbContext with InMemory for tests
            //    IMPORTANT: the name must be evaluated OUTSIDE the lambda,
            //    otherwise each scope/request creates a different DB.
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // 5. Ensure the DB is created and initial seed is executed
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // EnsureCreated creates the schema in memory
            db.Database.EnsureCreated();

            // Seed test data if needed
            SeedTestData(db);
        });
    }

    /// <summary>
    /// Seed minimum data required for tests to work
    /// </summary>
    private static void SeedTestData(ApplicationDbContext context)
    {
        // Create basic roles
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityRole 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Admin", 
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new Microsoft.AspNetCore.Identity.IdentityRole 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Employee", 
                    NormalizedName = "EMPLOYEE",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new Microsoft.AspNetCore.Identity.IdentityRole 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Customer", 
                    NormalizedName = "CUSTOMER",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );
            context.SaveChanges();
        }
    }
}
