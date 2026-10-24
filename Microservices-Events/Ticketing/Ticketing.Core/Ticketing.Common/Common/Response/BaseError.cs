using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Common.Common.Response
{
    public  class BaseError
    {
        public string? PropertyMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
