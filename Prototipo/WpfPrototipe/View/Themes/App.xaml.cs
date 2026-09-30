using Microsoft.Extensions.Configuration;
using RegistroPerf.Model;
using System;
using System.IO;
using System.Windows;

namespace RegistroPerf
{
    public partial class App : Application
    {
        public static ApiOptions ApiConf { get; private set; } = new ApiOptions();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Mismo criterio que la API: appsettings.json, luego appsettings.{Entorno}.json (opcional)
            // y por último variables de entorno (Api__BaseUrl), que pisan a los archivos.
            string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            ApiOptions? api = configuration
                .GetSection(ApiOptions.SectionName)
                .Get<ApiOptions>();

            if (api == null
                || string.IsNullOrWhiteSpace(api.BaseUrl)
                || string.IsNullOrWhiteSpace(api.SignUpPath)
                || string.IsNullOrWhiteSpace(api.SignInPath))
            {
                MessageBox.Show("Missing configuration: ApiConf:BaseUrl, ApiConf:SignUpPath and ApiConf:SignInPath in appsettings.json", "Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            ApiConf = api;

            MainWindow window = new MainWindow();
            window.Show();
        }
    }
}
