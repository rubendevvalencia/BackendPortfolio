using Ecommerce.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Infrastructure.Data
{
    /// <summary>
    /// Fábrica usada únicamente por las herramientas de EF Core (dotnet ef / Add-Migration).
    /// Permite crear el DbContext sin arrancar el host de la Api, algo necesario cuando el
    /// proveedor de servicios de la aplicación no está disponible en tiempo de diseño.
    /// No se utiliza en ejecución: ahí el contexto lo construye la inyección de dependencias.
    /// </summary>
    public class DbContextEFDesignTimeFactory : IDesignTimeDbContextFactory<DbContextEF>
    {
        public DbContextEF CreateDbContext(string[] args)
        {
            //El orden importa: cada fuente sobrescribe la clave que repita la anterior.
            //appsettings.json define la FORMA de la configuracion (con la cadena vacia);
            //User Secrets aporta el valor real en desarrollo y las variables de entorno en despliegue.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(ResolveSettingsPath())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<DbContextEFDesignTimeFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("EcommerceDb");

            //Fail-fast: sin cadena de conexion, "dotnet ef" fallaria mas tarde con un error opaco.
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No hay cadena de conexion 'EcommerceDb'. Configurala con: " +
                    "dotnet user-secrets set \"ConnectionStrings:EcommerceDb\" \"<cadena>\" --project Ecommerce");
            }

            //No activamos EnableRetryOnFailure aquí: la estrategia de reintentos no admite
            //las transacciones que abren las migraciones al aplicarse.
            var options = new DbContextOptionsBuilder<DbContextEF>()
                .UseSqlServer(connectionString, builder => builder.MigrationsAssembly(typeof(DbContextEF).Assembly.FullName))
                .Options;

            return new DbContextEF(options, configuration, new AuditableEntitySaveChangesInterceptor());
        }

        /// <summary>
        /// Localiza la carpeta que contiene appsettings.json. Las herramientas de EF ejecutan
        /// desde el proyecto de arranque, pero contemplamos también el caso contrario.
        /// </summary>
        private static string ResolveSettingsPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
            {
                return currentDirectory;
            }

            var apiDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "..", "Ecommerce"));
            if (File.Exists(Path.Combine(apiDirectory, "appsettings.json")))
            {
                return apiDirectory;
            }

            throw new InvalidOperationException($"No se encontró appsettings.json partiendo de '{currentDirectory}'.");
        }
    }
}
