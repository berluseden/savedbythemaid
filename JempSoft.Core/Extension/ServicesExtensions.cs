using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using JempSoft.Infraestructure;

namespace JempSoft.Infraestructure.Extensions
{
    public static class ServicesExtensions
    {
        public static void ConfigureService(this IServiceCollection services)
        {
            // Add services types
            services.AddTransient<IServiceTypeServices, ServiceTypeServices>();
        }
    }
}
