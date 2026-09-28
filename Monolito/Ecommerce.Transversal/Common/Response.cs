using System.Security.Cryptography.X509Certificates;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Transversal.Common
{
    public class Response<T> : IResponse
    {
        public T Data { get; set; }
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty; //Solo se rellena en caso de error, si IsSuccess es false

        //Detalle de los errores de validacion agrupados por propiedad ("City" -> ["City is required."]).
        //Se mantiene vacio cuando el fallo no es de validacion.
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        public IEnumerable<BaseError> Error { get; set;  }

        //Motivo del fallo. Permite que el controller decida el status code sin inspeccionar el Message.
        public ErrorType ErrorType { get; set; } = ErrorType.None;

        public static Response<T> Success(T data)
        {
            var response = new Response<T>
            {
                Data = data,
                IsSuccess = true,
            };
            return response;
        }

        public static Response<T> Fail(string message, ErrorType errorType = ErrorType.Unexpected)
        {
            var response = new Response<T>
            {
                IsSuccess = false,
                Message = message,
                ErrorType = errorType
            };
            return response;
        }

        public static Response<T> NotFound(string message)
        {
            var response = Fail(message, ErrorType.NotFound);
            return response;
        }

        public static Response<T> Invalid(Dictionary<string, List<string>> errors)
        {
            var response = new Response<T>
            {
                IsSuccess = false,
                ErrorType = ErrorType.Validation,
                Message = "One or more validation errors occurred.",
                Errors = errors
            };
            return response;
        }
    }
}
