using System.Collections;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
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
             

            return services;
        }
    }
}


