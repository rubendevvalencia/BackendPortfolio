using Ecommerce.Transversal.Common.Enums;

namespace Ecommerce.Transversal.Common
{
    public class Response<T>
    {
        public T Data { get; set; }
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty; //Solo se rellena en caso de error, si IsSuccess es false

        //Detalle de los errores de validacion agrupados por propiedad ("City" -> ["City is required."]).
        //Se mantiene vacio cuando el fallo no es de validacion.
        public Dictionary<string, List<string>> Errors { get; set; } = new();

        //Motivo del fallo. Permite que el controller decida el status code sin inspeccionar el Message.
        public ErrorType ErrorType { get; set; } = ErrorType.None;

        public static Response<T> Success(T data) => new()
        {
            Data = data,
            IsSuccess = true,
        };

        public static Response<T> Fail(string message, ErrorType errorType = ErrorType.Unexpected) => new()
        {
            IsSuccess = false,
            Message = message,
            ErrorType = errorType
        };

        public static Response<T> NotFound(string message) => Fail(message, ErrorType.NotFound);

        public static Response<T> Invalid(Dictionary<string, List<string>> errors) => new()
        {
            IsSuccess = false,
            ErrorType = ErrorType.Validation,
            Message = "One or more validation errors occurred.",
            Errors = errors
        };
    }
}
