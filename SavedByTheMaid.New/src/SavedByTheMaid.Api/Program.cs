using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Middleware;
using SavedByTheMaid.Infrastructure;
using SavedByTheMaid.Infrastructure.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. Configuración de servicios
// ============================================

// Infrastructure (DbContext, etc.) - pasa el environment para permitir testing
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

// JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher<SavedByTheMaid.Domain.Entities.ApplicationUser>, PasswordHasher<SavedByTheMaid.Domain.Entities.ApplicationUser>>();

// Autenticación JWT
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() 
    ?? new JwtSettings { Secret = "DefaultSecretKeyForDevelopmentOnly123456789!", Issuer = "SavedByTheMaid", Audience = "SavedByTheMaidApp" };

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// Autorización con políticas
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.EmployeeOrAdmin, policy => policy.RequireRole(Roles.Admin, Roles.Employee));
    options.AddPolicy(Policies.Authenticated, policy => policy.RequireAuthenticatedUser());
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddFixedWindowLimiter("booking", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.AutoReplenishment = true;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new 
        { 
            message = "Demasiadas solicitudes. Por favor espera un momento." 
        }, token);
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000", "http://localhost:5173" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// YARP para proxy a Vite en desarrollo
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddReverseProxy()
        .LoadFromMemory(
            new[]
            {
                new Yarp.ReverseProxy.Configuration.RouteConfig
                {
                    RouteId = "spa",
                    ClusterId = "vite",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Path = "{**catch-all}"
                    }
                }
            },
            new[]
            {
                new Yarp.ReverseProxy.Configuration.ClusterConfig
                {
                    ClusterId = "vite",
                    Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
                    {
                        { "vite", new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5173" } }
                    }
                }
            });
}

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ============================================
// 2. Middleware Pipeline
// ============================================

// Global Exception Handler (primero para capturar todo)
app.UseGlobalExceptionHandler();

// Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Authentication & Authorization (en orden correcto)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
})).AllowAnonymous();

// SPA Hosting
if (app.Environment.IsDevelopment())
{
    // En desarrollo: proxy a Vite dev server
    app.MapReverseProxy();
}
else
{
    // En producción: servir archivos estáticos
    app.UseDefaultFiles();
    app.UseStaticFiles();
    
    // Fallback para SPA routing
    app.MapFallbackToFile("index.html");
}

// Seed de roles al iniciar
await SeedRolesAsync(app.Services);

app.Run();

// ============================================
// 3. Seed de datos iniciales
// ============================================
static async Task SeedRolesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Crear roles si no existen
        foreach (var roleName in Roles.All)
        {
            var roleExists = await context.Roles.AnyAsync(r => r.Name == roleName);
            if (!roleExists)
            {
                context.Roles.Add(new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
                logger.LogInformation("Rol creado: {Role}", roleName);
            }
        }
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al crear roles iniciales");
    }
}

// Clase parcial para WebApplicationFactory en tests
public partial class Program { }
