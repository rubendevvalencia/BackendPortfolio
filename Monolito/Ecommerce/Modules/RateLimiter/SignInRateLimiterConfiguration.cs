using System.Globalization;

namespace Ecommerce.Api.Modules.RateLimiter
{
    //Configuracion propia de la politica de SignIn/SignUp, separada de RateLimiterConfiguration
    //a proposito: esa se deja intacta como ejemplo de partida.
    public static class SignInRateLimiterConfiguration
    {
        public static RateLimiterSettings Configuration(IConfiguration configuration)
        {
            string? permitLimitText = configuration["SignInRateLimiting:PermitLimit"];
            string? windowText = configuration["SignInRateLimiting:Window"];
            string? queueLimitText = configuration["SignInRateLimiting:QueueLimit"];

            var permitLimitOk = int.TryParse(permitLimitText, CultureInfo.InvariantCulture, out var permitLimit);

            if (!permitLimitOk)
                throw new InvalidOperationException($"SignInRateLimiting:PermitLimit de '{permitLimitText}' no es un int valido (formato esperado \"0\").");

            var windowOk = TimeSpan.TryParseExact(windowText, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var window);

            if (!windowOk)
                throw new InvalidOperationException($"SignInRateLimiting:Window de '{windowText}' no es un TimeSpan valido (formato esperado \"hh:mm:ss\").");

            var queueLimitOk = int.TryParse(queueLimitText, CultureInfo.InvariantCulture, out var queueLimit);

            if (!queueLimitOk)
                throw new InvalidOperationException($"SignInRateLimiting:QueueLimit de '{queueLimitText}' no es un int valido (formato esperado \"0\").");

            if (permitLimit <= 0)
                throw new InvalidOperationException($"SignInRateLimiting:PermitLimit debe ser mayor que 0, pero vale {permitLimit}.");

            if (window <= TimeSpan.Zero)
                throw new InvalidOperationException($"SignInRateLimiting:Window debe ser mayor que 0, pero vale {window}.");

            if (queueLimit < 0)
                throw new InvalidOperationException($"SignInRateLimiting:QueueLimit no puede ser negativo, pero vale {queueLimit}.");

            return new RateLimiterSettings(permitLimit, window, queueLimit);
        }
    }
}
