using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistroPerf.Converters
{
    public class ResponseConverter
    {
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty; //Solo se rellena en caso de error, si IsSuccess es false

        //Detalle de los errores de validacion agrupados por propiedad ("City" -> ["City is required."]).
        //Se mantiene vacio cuando el fallo no es de validacion.
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        public IEnumerable<BaseError> Error { get; set; }

    }

    public class BaseError
    {
        public string? PropertyMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
