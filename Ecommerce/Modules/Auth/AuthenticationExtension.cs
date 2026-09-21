using Ecommerce.Api.Modules.Services.CurrentUser;
using Ecommerce.Domain.Interface.IAuthIdentification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Api.Models.Auth
{
    public static class AuthenticationExtension 
    {
        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt"); // Obtiene la sección de configuración compartida por la validación y la generación de tokens.
       
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // Establece JWT Bearer como esquema de autenticación predeterminado.
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true, // Comprueba que el emisor del token coincide con el emisor configurado.
                        ValidateAudience = true, // Comprueba que el token está destinado a esta API o aplicación.
                        ValidateLifetime = true, // Rechaza tokens caducados.
                        ValidateIssuerSigningKey = true, // Comprueba que la firma del token es válida.
                        ValidIssuer = jwtSettings["Issuer"], // Define el emisor válido; debe coincidir con Jwt:Issuer al generar el token.
                        ValidAudience = jwtSettings["Audience"], // Define la audiencia válida; debe coincidir con Jwt:Audience al generar el token.
                        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings["Key"]!)), // Usa Jwt:Key para verificar la firma HMAC del token.
                        ClockSkew = TimeSpan.Zero // No añade margen de tiempo a la caducidad del token.
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.Append("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddHttpContextAccessor();                     // Da acceso a HttpContext.User fuera del controller.
            services.AddScoped<ICurrentUser, CurrentUser>();       // Permite rellenar la auditoría que ya no depende de system, sino de un user para rellenar CreatedBy / LastUpdatedBy.
            return services;
        }
    }
}
