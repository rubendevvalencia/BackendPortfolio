using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace RegistroPerf.Converters
{
    public class ResponseConverter<T>
    {
        public T Data { get; set; }
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty; //Solo se rellena en caso de error, si IsSuccess es false
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        public IEnumerable<BaseError> Error { get; set; }

    }

    public class BaseError
    {
        public string? PropertyMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
