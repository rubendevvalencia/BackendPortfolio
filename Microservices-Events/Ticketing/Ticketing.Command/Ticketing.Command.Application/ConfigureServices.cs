using System.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Ticketing.Command.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            return services;
        }
    }
}


