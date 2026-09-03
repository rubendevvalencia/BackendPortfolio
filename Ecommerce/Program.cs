using Ecommerce.Infrastructure;
using Ecommerce.Api.Models.Auth;
using Ecommerce.Api.Models.Swagger;
using Ecommerce.Application;
using Ecommerce.Api.Models.Cors;
using Microsoft.AspNetCore.HttpOverrides;
using Ecommerce.Api.Models.Auth;
using Ecommerce.Transversal;
using Serilog; //Necesario para ForwardedHeadersOptions / ForwardedHeaders.

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Add services to the container.
    
    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddTransversalServices(builder.Configuration); //Registra los servicios transversal
    builder.Services.AddAuth(builder.Configuration); // Registra la autenticación JWT usando la configuración de Jwt.
    builder.Services.AddCorsPolicy(builder.Configuration); //Registra (define) la politica CORS leyendo los origenes de "Config:OrinCors".
                                                           //OJO: registrar la politica NO la aplica. Aplicarla es tarea de app.UseCors() mas abajo.
    builder.Services.AddSwagger();
    builder.Host.UseSerilog(); //Remplaza el logger por defecto de .NET por Serilog, que ya se ha configurado en AddTransversalServices().
    
    var app = builder.Build();
    
    // Configure the HTTP request pipeline.
    
    //IMPORTANTE: cada app.UseXxx() se ejecuta UNA SOLA VEZ al arrancar, para ir construyendo en orden
    //la cadena de middlewares por la que pasara despues cada peticion HTTP. No se ejecutan por peticion.
    //Consecuencia: un UseXxx() dentro de un "if" que sea falso al arrancar NO se registra nunca y ese
    //middleware sencillamente no existe en la aplicacion.
    
    //Va de los primeros porque el resto del pipeline depende de el.
    //En Azure (App Service, Container Apps...) el TLS termina en el balanceador: nuestra app recibe la
    //peticion como HTTP plano y el esquema original viaja en la cabecera X-Forwarded-Proto. Sin procesarla,
    //UseHttpsRedirection() creeria que TODA peticion es insegura y respondera 307 incluso a los preflight
    //OPTIONS de CORS, que muchos navegadores no siguen -> CORS roto en produccion sin motivo aparente.
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    
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
        //app.MapOpenApi(): No está del todo maduro y es recomendable seguir usando UseSwagger() y UseSwaggerUI() para tener un control más completo sobre la configuración de Swagger en el entorno de desarrollo.
    }
    
    app.UseSerilogRequestLogging(); //Registra en Serilog cada peticion HTTP, con su metodo, ruta, codigo de respuesta y tiempo de respuesta. Se ejecuta ANTES de la autenticacion y autorizacion para que registre tambien los intentos fallidos.
    app.UseHttpsRedirection();
    
    //UseCors() esta FUERA del if(IsDevelopment()) a proposito: si estuviera dentro, en produccion no se
    //registraria y las respuestas saldrian sin la cabecera Access-Control-Allow-Origin. El fallo es dificil
    //de diagnosticar porque CORS lo aplica el NAVEGADOR, no el servidor: la API responde 200, los logs se ven
    //perfectos y desde Postman o curl funciona (no son navegadores), pero el navegador oculta la respuesta
    //al JavaScript con el clasico "blocked by CORS policy".
    //Solo sobraria si en produccion el front y la API compartieran origen (SPA servida desde wwwroot de la
    //propia API, Static Web Apps con linked backend, o un mismo ingress/reverse proxy). Si delante hay API
    //Management o Front Door, lo habitual es definir CORS alli y no aqui, para no duplicar cabeceras.
    //Su posicion es la canonica: despues de UseHttpsRedirection() y ANTES de UseAuthorization(), para que el
    //preflight OPTIONS se responda antes de que nadie exija autenticacion.
    //En Azure App Service: deja VACIO el CORS del portal (Settings -> CORS). Si se configura en los dos sitios,
    //App Service responde el tambien y llegan DOS cabeceras Access-Control-Allow-Origin, que el navegador rechaza.
    //Los origenes por entorno se cambian sin tocar codigo, en Application settings: Config__OrinCors=https://...
    //(doble guion bajo = el ":" de las claves anidadas). Sin barra final en el origen.
    app.UseCors(CorsExtension.myPolicy);
    
    app.UseAuthentication(); // Valida el token JWT y procesa el usuario autenticado antes de que llegue a los controladores.
                             // Siempre debe ir antes de UseAuthorization() para que la autorización tenga un usuario válido.
    app.UseAuthorization();
    
    app.MapControllers();
    
    
    Log.Information("Starting ecommerce API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");

}
finally
{
    Log.CloseAndFlush();
}
