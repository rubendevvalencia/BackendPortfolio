namespace Ecommerce.Api.Models.Cors
{
    public static class CorsExtension
    {
        public static string myPolicy = "policyApiEcommerce";
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(myPolicy, builder =>
                {
                    builder.WithOrigins(["Config:OrinCors"])
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            return services;
        }
    }
}
