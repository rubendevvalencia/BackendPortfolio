using System.Collections;
using System.Reflection;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Command.Application.Mapping;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;

namespace Ticketing.Command.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                });

            services.AddSingleton<IMapper, Mapper>(); 
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>()); //Documentacion Automapper: https://docs.automapper.io/en/latest/Dependency-injection.html
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}


