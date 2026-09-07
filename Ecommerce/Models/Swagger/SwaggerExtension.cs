using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ecommerce.Api.Models.Swagger
{
    public static class SwaggerExtension
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            services.AddSwaggerGen(c =>
            {
                //Al incluir todo el tema del versionado,
                //la configuración del swagger se hace a través de "ConfigureSwaggerOptions.cs"
                
                //c.SwaggerDoc("v0", new OpenApiInfo
                //{
                //    Title = "Ecommerce Api",
                //    Version = "v0",
                //    Description = "API for Ecommerce Application",
                //    Contact = new OpenApiContact
                //    {
                //        Name = "Your Name",
                //        Email = "your.email@example.com",
                //        Url = new Uri("https://yourwebsite.com")
                //    },
                //    License = new OpenApiLicense
                //    {
                //        Name = "Use under LICX",
                //        Url = new Uri("https://example.com/license")
                //    }
                //});
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                //Configuramos el esquema de seguridad para JWT Bearer en Swagger, permitiendo a los usuarios autenticarse y probar los endpoints protegidos.
                //El id "Bearer" (JwtBearerDefaults.AuthenticationScheme) es el mismo que registra AddAuth():
                //la definicion describe COMO se autentica, y el requisito de mas abajo la referencia por ese id.
                var securityScheme = new OpenApiSecurityScheme
                {
                    //Con Type = Http y Scheme = "bearer", Swagger UI antepone el prefijo "Bearer " al enviar
                    //la cabecera, por eso en el cuadro de texto solo se pega el token pelado.
                    //Name e In no se declaran: OpenApi solo los serializa para Type = ApiKey, donde hace falta
                    //decir en que cabecera viaja la credencial. Aqui lo fija el propio estandar HTTP.
                    Description = "Enter JWT Bearer token **_only_**",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };
                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

                //Microsoft.OpenApi 2.0 (el que arrastra Swashbuckle 10) separo el objeto de su referencia:
                //OpenApiSecurityScheme ya no tiene propiedad Reference, y OpenApiSecurityRequirement paso a ser
                //un diccionario de OpenApiSecuritySchemeReference. Antes se reutilizaba el propio scheme aqui.
                //Por eso AddSecurityRequirement recibe ahora una lambda: entrega el OpenApiDocument para poder
                //atar la referencia a su documento, y que "Bearer" resuelva contra la definicion de arriba.
                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    { new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document), new List<string>() }
                });
                c.EnableAnnotations(); //Anotaciones de Swagger para documentar los endpoints de la API, como descripciones, parámetros y respuestas.

            });

            return services;
        }
    }
}
