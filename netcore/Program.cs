using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using netcore.Data;
using netcore.Models;
using netcore.Services;
using netcore.Services.Administration.Page;
using netcore.Services.Book;
using netcore.Services.Services;
using netcore.Services.ServiceMeet;
using netcore.UnitOfWork;
using netcore.POCOs;

var builder = WebApplication.CreateBuilder(args);

// Get MySQL connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// Get Identity Default Options
IConfigurationSection identityDefaultOptionsConfigurationSection = builder.Configuration.GetSection("IdentityDefaultOptions");

builder.Services.Configure<IdentityDefaultOptions>(identityDefaultOptionsConfigurationSection);

var identityDefaultOptions = identityDefaultOptionsConfigurationSection.Get<IdentityDefaultOptions>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    if (identityDefaultOptions != null)
    {
        // Password settings
        options.Password.RequireDigit = identityDefaultOptions.PasswordRequireDigit;
        options.Password.RequiredLength = identityDefaultOptions.PasswordRequiredLength;
        options.Password.RequireNonAlphanumeric = identityDefaultOptions.PasswordRequireNonAlphanumeric;
        options.Password.RequireUppercase = identityDefaultOptions.PasswordRequireUppercase;
        options.Password.RequireLowercase = identityDefaultOptions.PasswordRequireLowercase;
        options.Password.RequiredUniqueChars = identityDefaultOptions.PasswordRequiredUniqueChars;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identityDefaultOptions.LockoutDefaultLockoutTimeSpanInMinutes);
        options.Lockout.MaxFailedAccessAttempts = identityDefaultOptions.LockoutMaxFailedAccessAttempts;
        options.Lockout.AllowedForNewUsers = identityDefaultOptions.LockoutAllowedForNewUsers;

        // User settings
        options.User.RequireUniqueEmail = identityDefaultOptions.UserRequireUniqueEmail;

        // email confirmation require
        options.SignIn.RequireConfirmedEmail = identityDefaultOptions.SignInRequireConfirmedEmail;
    }
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Cookie settings - SHARED between netcore and EService.Web for SSO
var sharedCookieSection = builder.Configuration.GetSection("SharedCookie");
var applicationName = sharedCookieSection["ApplicationName"] ?? "SavedByTheMaid";
var cookieName = sharedCookieSection["CookieName"] ?? ".SavedByTheMaid.Auth";

if (identityDefaultOptions != null)
{
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = identityDefaultOptions.CookieHttpOnly;
        options.ExpireTimeSpan = TimeSpan.FromDays(identityDefaultOptions.CookieExpiration);
        options.LoginPath = identityDefaultOptions.LoginPath;
        options.LogoutPath = identityDefaultOptions.LogoutPath;
        options.AccessDeniedPath = identityDefaultOptions.AccessDeniedPath;
        options.SlidingExpiration = identityDefaultOptions.SlidingExpiration;
    });
}

// Add Data Protection with shared application name for SSO
builder.Services.AddDataProtection()
    .SetApplicationName(applicationName);

// Custom Services
builder.Services.AddScoped<ICleaningPlaceServices, CleaningPlaceServices>();
builder.Services.AddScoped<ICleaningPlaceRoomServices, CleaningPlaceRoomServices>();
builder.Services.AddScoped<IServiceTypeServices, ServiceTypeServices>();
builder.Services.AddScoped<IAdditionalServiceTypeServices, AdditionalServiceTypeServices>();
builder.Services.AddScoped<IEmployeeServices, EmployeeServices>();
builder.Services.AddScoped<IEmployeeScheduleServices, EmployeeScheduleServices>();
builder.Services.AddScoped<IServiceMeetServices, ServiceMeetServices>();

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPageCookieService, PageCookieService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add email services.
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Add custom role services
builder.Services.AddTransient<IRoles, Roles>();

// Add DI for Dotnetdesk
builder.Services.AddTransient<INetcoreService, NetcoreService>();

// Get SendGrid configuration options
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("SendGridOptions"));

// Get SMTP configuration options
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("SmtpOptions"));

// Get Super Admin Default options
builder.Services.Configure<SuperAdminDefaultOptions>(builder.Configuration.GetSection("SuperAdminDefaultOptions"));

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        var jempSoftContext = services.GetRequiredService<ApplicationDbContext>();
        jempSoftContext.Database.EnsureCreated();
        
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var netcoreService = services.GetRequiredService<INetcoreService>();
        await netcore.Data.DbInitializer.Initialize(context, userManager, roleManager, netcoreService);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Admin routes (attribute-based routing handles /admin prefix)
app.MapControllers();

// Area routing with explicit area prefix
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Client portal at root path (using Client area)
app.MapControllerRoute(
    name: "client",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Client" });

app.Run();
