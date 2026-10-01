using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Common.Behaviours
{
    //Cobertura automática: envuelve la ejecución del handler y deja rastro sin que el handler sepa que existe.
    //Cero código por caso de uso, a cambio de ver solo el borde —request y response, nunca el interior de la decisión.
    //Cubre únicamente lo que pasa por MediatR con Send, es decir hoy exactamente v3: v1, v2 y
    //UserAuthApplication no entran al pipeline y ahí el log se sigue llamando a mano en el call site.
    //Lo que sí ve, y no es poco: como los fallos esperados viajan dentro de Response<T> en vez de lanzarse,
    //puede leer el motivo del fallo y elegir el nivel del log según el ErrorType.
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger; //El logger gestiona toda la información
        public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            //Paso pre. 
              var requestName = typeof(TRequest).Name;
            _logger.LogDebug("Clean Architecture Request Handling {Request}", requestName);

            //Next. El next es el método delegado que lleva la solicitud al siguiente pipeline
            var response = await next();

            //Paso post. TResponse es genérico: se lee a través de la cara no genérica de Response<T>.
            var result = response as IResponse;
            if (result is null) return response; //El handler no devuelve Response<T>: no hay nada que clasificar
            var info = ErrorControlInfo(result);
           _logger.Log(info, "{Request} -> {ErrorType}: {Message}", requestName, result.ErrorType, result.Message);
            return response; //Continua la app
        }

        //El nivel decide a dónde llega cada entrada: Information va a consola y fichero, y desde Warning
        //también a la tabla SQL. Unexpected no sube a Error a propósito: las excepciones reales ya las
        //registra GlobalExceptionHandler como Error, y así no se confunde "no se guardó nada" con "se cayó la base de datos".
        private static LogLevel ErrorControlInfo(IResponse result)
        {
            if (result.IsSuccess) return LogLevel.Information;

            var level = result.ErrorType switch
            {
                ErrorType.Validation => LogLevel.Information, //Error del cliente; los detalles ya viajan en el body
                ErrorType.NotFound => LogLevel.Information,   //Tráfico normal; un escaneo llenaría la tabla SQL
                ErrorType.Duplicated => LogLevel.Warning,     //Conflicto de negocio que interesa conservar
                ErrorType.Unexpected => LogLevel.Warning,     //Hoy es SaveChangesAsync devolviendo 0 (pendiente nº 4)
                ErrorType.TimeOut => LogLevel.Warning,
                _ => LogLevel.Warning                         //Un fallo sin ErrorType no debería existir: mejor que se note
            };
            return level;
        }
    }
}
