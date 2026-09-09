using AutoMapper;
using Ecommerce.Application.Feature.Customers;
using Ecommerce.Application.Feature.Users;
using Ecommerce.Application.Interface;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Application.MainService.Jwt;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Interface.IRepository;
using FluentValidation;
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
            services.AddScoped<ICustomerApplicationUoW, CustomerApplicationUoW>();
            services.AddScoped<IUserAuthApplication, UserAuthApplication>();
            services.AddScoped<IJwtApplication, JwtApplication>();
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>()); //Documentacion Automapper: https://docs.automapper.io/en/latest/Dependency-injection.html

            //Registra automaticamente todos los AbstractValidator<T> de este ensamblado como IValidator<T>.
            //Añadimos el ensamblado genérico de IFluentValidation, con esto no es necesario registrar cada validador de manera individual, 
            //se registran todos los que hereden de AbstractValidator<T> en el ensamblado.
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            
            return services;
        }
    }
}
