using AutoMapper;
using Ecommerce.Application.Interface;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Service;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Ecommerce.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICustomerApplication, CustomerApplication>();
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>()); //Documentacion Automapper: https://docs.automapper.io/en/latest/Dependency-injection.html
            return services;
        }
    }
}
