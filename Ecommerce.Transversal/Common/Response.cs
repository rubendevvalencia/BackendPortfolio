using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Transversal.Common
{
    public class Response<T>
    {
        public T Data { get; set; }
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty; //Solo se rellena en caso de error, si IsSuccess es false
    }
}
