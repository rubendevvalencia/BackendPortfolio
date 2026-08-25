using Ecommerce.Application.Main;
using Ecommerce.Infrastructure;
using Ecommerce.Domain.Main;
using Ecommerce.Api.Models.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDomainServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSwagger();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); //Esto habilita la generación de la documentación Swagger en el entorno de desarrollo. Genera el json de la API y lo sirve en la ruta /swagger/v1/swagger.json.
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v0/swagger.json", "Ecommerce Api v0"); //Esto habilita la interfaz de usuario de Swagger en el entorno de desarrollo.
                                                                        //Permite a los desarrolladores explorar y probar los endpoints de la API a través de una interfaz web interactiva.
                                                                        //La ruta /swagger/v0/swagger.json es donde se encuentra el archivo JSON generado por UseSwagger() que describe la API.
        c.RoutePrefix = "swagger"; //Esto establece la ruta base para acceder a la interfaz de usuario de Swagger. En este caso, la interfaz estará disponible en /swagger.
        c.DisplayRequestDuration(); //Esto habilita la visualización de la duración de las solicitudes en la interfaz de usuario de Swagger. Muestra cuánto tiempo tarda cada solicitud en completarse, lo que puede ser útil para el rendimiento y la depuración.
        c.EnableDeepLinking();
        c.ShowExtensions();
    });
    //app.MapOpenApi(); No está del todo maduro y es recomendable seguir usando UseSwagger() y UseSwaggerUI() para tener un control más completo sobre la configuración de Swagger en el entorno de desarrollo.
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
