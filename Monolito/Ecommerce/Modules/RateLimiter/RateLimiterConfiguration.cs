using System.Globalization;

namespace Ecommerce.Api.Modules.RateLimiter
{
    //Los valores de RateLimiting ya tipados y validados: quien llama accede por nombre y recibe
    //el TimeSpan hecho, sin castear ni recordar en que posicion estaba cada valor.
    public class RateLimiterSettings
    {
        public int PermitLimit { get; }
        public TimeSpan Window { get; }
        public int QueueLimit { get; }

        public RateLimiterSettings(int permitLimit, TimeSpan window, int queueLimit)
        {
            PermitLimit = permitLimit;
            Window = window;
            QueueLimit = queueLimit;
        }
        
    }

    public static class RateLimiterConfiguration
    {
        public static RateLimiterSettings Configuration(IConfiguration configuration)
        {
            string? permitLimitText = configuration["RateLimiting:PermitLimit"];
            string? windowText = configuration["RateLimiting:Window"];
            string? queueLimitText = configuration["RateLimiting:QueueLimit"];

            //InvariantCulture a proposito: el valor viene de un archivo de configuracion, no del
            //usuario, y no debe depender de la cultura de la maquina donde se despliegue.
            var permitLimitOk = int.TryParse(permitLimitText, CultureInfo.InvariantCulture, out var permitLimit);

            if (!permitLimitOk)
                throw new InvalidOperationException($"PermitLimit de '{permitLimitText}' no es un int valido (formato esperado \"0\").");

            //TryParseExact y no TryParse: TimeSpan lee un entero suelto como DIAS, asi que un
            //"30" pensado como segundos daria una ventana de 30 dias sin que nada se queje.
            //Exigiendo "hh:mm:ss" ese error se convierte en un fallo al arrancar.
            var windowOk = TimeSpan.TryParseExact(windowText, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var window);

            if (!windowOk)
                throw new InvalidOperationException($"Window de '{windowText}' no es un TimeSpan valido (formato esperado \"hh:mm:ss\").");

            var queueLimitOk = int.TryParse(queueLimitText, CultureInfo.InvariantCulture, out var queueLimit);

            if (!queueLimitOk)
                throw new InvalidOperationException($"QueueLimit de '{queueLimitText}' no es un int valido (formato esperado \"0\").");

            //Rangos validados aqui y no en AddFixedWindowLimiter: el limiter tambien los rechaza,
            //pero con un ArgumentOutOfRangeException que no dice de que clave de configuracion viene.
            if (permitLimit <= 0)
                throw new InvalidOperationException($"PermitLimit debe ser mayor que 0, pero vale {permitLimit}.");

            //TryParseExact y no TryParse: TimeSpan lee un entero suelto como DIAS, asi que un
            //"30" pensado como segundos daria una ventana de 30 dias sin que nada se queje.
            //Exigiendo "hh:mm:ss" ese error se convierte en un fallo al arrancar.
            if (window <= TimeSpan.Zero)
                throw new InvalidOperationException($"Window debe ser mayor que 0, pero vale {window}.");

            if (queueLimit < 0)
                throw new InvalidOperationException($"QueueLimit no puede ser negativo, pero vale {queueLimit}.");

            return new RateLimiterSettings(permitLimit, window, queueLimit);
        }
    }
}
