using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Application.Interfaces;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Infrastructure.Data;

// AddIdentity is in this namespace
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SavedByTheMaid.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services.
    ///
    /// BEST PRACTICE:
    /// - Check the environment before configuring a real DB
    /// - Allow tests to use InMemory without a MySQL connection
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        // In Testing environment, do NOT register MySQL - tests configure InMemory
        if (environmentName == "Testing")
        {
            return services;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // NOTE: MySql.EntityFrameworkCore (Oracle provider) does not expose EnableRetryOnFailure
        // via the options builder — that is a Pomelo-only API. Connection resiliency is handled
        // at startup via the retry loop in WaitForDatabaseAsync (see below).
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySQL(connectionString));

        // Register IApplicationDbContext so Application-layer services can inject the interface
        // (used by OrderManagementService and BookingApplicationService).
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // Add Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;

            // Lockout settings — limit brute-force attempts on login
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Waits for MySQL to become reachable, retrying with exponential back-off.
    /// Call this from Program.cs after building the host, before seeding.
    /// Handles the docker-compose race condition where MySQL starts after the API container.
    /// </summary>
    public static async Task WaitForDatabaseAsync(ApplicationDbContext context, ILogger logger)
    {
        const int maxRetries = 10;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (await context.Database.CanConnectAsync())
                {
                    logger.LogInformation("Database connection established on attempt {Attempt}", attempt);
                    return;
                }
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                logger.LogWarning(
                    "DB connection attempt {Attempt}/{Max} failed ({Message}), retrying in {Delay}s",
                    attempt, maxRetries, ex.Message, delay.TotalSeconds);

                if (attempt == maxRetries)
                {
                    logger.LogError("Could not connect to database after {Max} attempts", maxRetries);
                    throw;
                }

                await Task.Delay(delay);
            }
        }
    }
}
