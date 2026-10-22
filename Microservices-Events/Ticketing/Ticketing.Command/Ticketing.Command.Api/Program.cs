using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Ticketing.Command.Api.Modules.Versioning;
using Ticketing.Command.Application;
using Ticketing.Command.Infrastructure;
using Ticketing.Command.Infrastructure.Mapping;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddVersioning();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();





var app = builder.Build();

//Crea los índices de Mongo (idempotente); sin el índice único de versión no hay control de concurrencia real
await app.Services.EnsureMongoIndexesAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger(); //Esto habilita la generación de la documentación Swagger en el entorno de desarrollo. Genera el json de la API y lo sirve en la ruta /swagger/v1/swagger.json.
    app.UseSwaggerUI(c =>
    {
        //c.SwaggerEndpoint("/swagger/v0/swagger.json", "Ecommerce Api v0"); //Esto habilita la interfaz de usuario de Swagger en el entorno de desarrollo.
                                                                            //Permite a los desarrolladores explorar y probar los endpoints de la API a través de una interfaz web interactiva.
                                                                            //La ruta /swagger/v0/swagger.json es donde se encuentra el archivo JSON generado por UseSwagger() que describe la API.
        
        
        foreach (var description in provider.ApiVersionDescriptions)
        {
            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
        c.RoutePrefix = "swagger"; //Esto establece la ruta base para acceder a la interfaz de usuario de Swagger. En este caso, la interfaz estará disponible en /swagger.
        c.DisplayRequestDuration(); //Esto habilita la visualización de la duración de las solicitudes en la interfaz de usuario de Swagger. Muestra cuánto tiempo tarda cada solicitud en completarse, lo que puede ser útil para el rendimiento y la depuración.
        c.EnableDeepLinking();
        c.ShowExtensions();
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
