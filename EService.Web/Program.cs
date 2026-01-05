using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EService.Web.Data;
using EService.Web.Models;
using EService.Web.Services;
using JempSoft.Applications.Services;
using JempSoft.Core.Data;
using JempSoft.Applications;
using JempSoft.Applications.Book;
using JempSoft.Core.Repository;
using JempSoft.Infraestructure.Extensions;
using JempSoft.Core.UnitOfWork;
using JempSoft.Applications.Administration.Page;

var builder = WebApplication.CreateBuilder(args);

// Get MySQL connection strings
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var coreConnection = builder.Configuration.GetConnectionString("CoreEntitiesConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(defaultConnection, serverVersion));

builder.Services.AddDbContext<JempSoftDbContext>(options =>
    options.UseMySql(coreConnection, serverVersion));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add application services with proper logging injection
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ICleaningPlaceServices, CleaningPlaceServices>();
builder.Services.AddScoped<ICleaningPlaceRoomServices, CleaningPlaceRoomServices>();
builder.Services.AddScoped<IServiceTypeServices, ServiceTypeServices>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPageCookieService, PageCookieService>();
builder.Services.ConfigureRepository();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddControllersWithViews(options =>
{
    options.CacheProfiles.Add("Hourly", new Microsoft.AspNetCore.Mvc.CacheProfile()
    {
        Duration = 60 * 60 // 1 hour
    });
    options.CacheProfiles.Add("Weekly", new Microsoft.AspNetCore.Mvc.CacheProfile()
    {
        Duration = 60 * 60 * 24 * 7 // 7 days
    });
});

builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// Ensure database is created and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        
        var jempSoftContext = services.GetRequiredService<JempSoftDbContext>();
        jempSoftContext.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
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

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
