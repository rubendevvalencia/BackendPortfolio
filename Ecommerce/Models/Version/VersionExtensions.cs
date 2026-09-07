using Asp.Versioning;

namespace Ecommerce.Api.Models.Version
{
    public static class VersionExtensions
    {
        public static IServiceCollection AddVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;     //Sino especifica la versión, se asume la predeterminada
                options.ReportApiVersions = true;                       //Permite mostrar las versiones disponibles en la respuesta
                options.ApiVersionReader = ApiVersionReader.Combine(    //Lee la versión de la API desde diferentes fuentes (query string, encabezado, tipo de medio)
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("X-Version"),
                    new MediaTypeApiVersionReader("ver"));
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";                     //Versionando semántico
                options.SubstituteApiVersionInUrl = true;               //Permite sustituir la versión de la API en la URL
            });
            return services;
        }
    }
}
