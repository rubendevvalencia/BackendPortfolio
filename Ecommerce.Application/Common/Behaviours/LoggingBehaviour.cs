using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Application.Common.Behaviours
{
    //Cobertura automática: envuelve la ejecución del handler y deja rastro de entrada y salida sin que el
    //handler sepa que existe. Cero código por caso de uso, a cambio de ver solo el borde —request y response,
    //nunca el interior de la decisión.
    //Cubre únicamente lo que pasa por MediatR como IRequest, es decir hoy exactamente v3: v1, v2 y
    //UserAuthApplication no entran al pipeline y ahí el log se sigue llamando a mano en el call site.
    //Lo que sí ve, y no es poco: como los fallos esperados viajan dentro de Response<T> en vez de lanzarse,
    //el motivo del fallo llega serializado en la respuesta —incluido el ErrorType.
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> //login y response
    {
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger; //El logger gestiona toda la información
        public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            //Paso pre
            _logger.LogInformation("Clean Architecture Request Handling: {name} {@request}", typeof(TResponse).Name, JsonSerializer.Serialize(request)); //Payload request
            //Next. El next es el método delegado que lleva la solicitud al siguiente pipeline
            var response = await next();
            //Paso post
            _logger.LogInformation("Clean Architecture Request Handling: {name} {@response}", typeof(TResponse).Name, JsonSerializer.Serialize(response)); //Payload response

            return response; //Continua la app
        }
    }
}
