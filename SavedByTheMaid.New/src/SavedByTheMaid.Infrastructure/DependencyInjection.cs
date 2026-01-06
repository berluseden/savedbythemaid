using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de infraestructura.
    /// 
    /// BUENA PRÁCTICA:
    /// - Verificar el ambiente antes de configurar DB real
    /// - Permitir que tests usen InMemory sin conexión MySQL
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration,
        string? environmentName = null)
    {
        // En ambiente Testing, NO registrar MySQL - los tests configuran InMemory
        if (environmentName == "Testing")
        {
            return services;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        return services;
    }
}
