using Ecommerce.Transversal.Loggin.Interface;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Transversal.Loggin.Services
{
    //Implementación sobre ILogger<T>: el genérico T fija la categoría del log, así el origen de cada entrada
    //queda registrado sin pasarlo a mano en cada llamada. Solo delega —el valor no está en lo que añade,
    //sino en que el consumidor dependa de IApiLogger y no del proveedor: Serilog se configura en un único
    //sitio y esta clase no cambia. Sin consumidores hoy; ver el comentario de IApiLogger<T>.
    public class AppLogger<T> : IApiLogger<T>
    {
        private readonly ILogger<T> _logger;
        public AppLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }
    }
}
