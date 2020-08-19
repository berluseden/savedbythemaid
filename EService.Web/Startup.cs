using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EService.Web.Data;
using EService.Web.Models;
using EService.Web.Services;
using JempSoft.Applications.Services;
using JempSoft.Core.Data;
using JempSoft.Applications;
using Microsoft.AspNetCore.Mvc;
using JempSoft.Core.Repository;
using JempSoft.Infraestructure.Extensions;
using JempSoft.Core.UnitOfWork;
using JempSoft.Applications.Administration.Page;

namespace EService.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));


            services.AddDbContext<JempSoftDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("CoreEntitiesConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();



            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddScoped<ICleaningPlaceServices, CleaningPlaceServices>();
            services.AddScoped<ICleaningPlaceRoomServices, CleaningPlaceRoomServices>();
            services.AddScoped<IServiceTypeServices, ServiceTypeServices>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICookieService, CookieService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPageCookieService, PageCookieService>();
            services.ConfigureRepository();


            services.AddMvc(options =>
            {
                options.CacheProfiles.Add("Hourly", new CacheProfile()
                {
                    Duration = 60 * 60 // 1 hour
                });
                options.CacheProfiles.Add("Weekly", new CacheProfile()
                {
                    Duration = 60 * 60 * 24 * 7 // 7 days
                });
            });

            services.AddSession();
            services.AddDistributedMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseSession();


            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
