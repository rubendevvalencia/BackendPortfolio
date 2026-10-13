namespace Ecommerce.Api.Models.Cors
{
    public static class CorsExtension
    {
        public const string myPolicy = "policyApiEcommerce";

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            //Se admite tanto un unico origen ("Config:OriginCors": "https://localhost:3000")
            //como una lista de origenes ("Config:OriginCors": [ "...", "..." ]).
            var section = configuration.GetSection("Config:OriginCors");
            var origins = section.GetChildren().Any()
                ? section.Get<string[]>() ?? []
                : (section.Value ?? string.Empty)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (origins.Length == 0)
                throw new InvalidOperationException("No se ha configurado ningun origen en 'Config:OriginCors'.");

            services.AddCors(options =>
            {
                options.AddPolicy(myPolicy, builder =>
                {
                    builder.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            return services;
        }
    }
}
