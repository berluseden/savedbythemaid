using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.Repository;
using JempSoft.Core.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace JempSoft.Infraestructure.Extensions
{
    public static class ServicesExtensions
    {
        public static void ConfigureRepository(this IServiceCollection services)
        {
            services.AddScoped<IRepository<ServiceOrder>, BaseRepository<ServiceOrder>>();
            services.AddScoped<IRepository<ServiceOrderContactInfo>, BaseRepository<ServiceOrderContactInfo>>();
            services.AddScoped<IRepository<ServiceOrderAdditionalService>, BaseRepository<ServiceOrderAdditionalService>>();
        }

        public static void ConfigureUnitOfWork(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork<JempSoftDbContext>, UnitOfWork<JempSoftDbContext>>
        }
    }
}
