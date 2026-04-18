using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySQL(connectionString));

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
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }
}
