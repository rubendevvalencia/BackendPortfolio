using Ecommerce.Transversal.Loggin.Interface;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Transversal.Loggin.Services
{
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
