using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistroPerf.Model.Response
{
    public sealed record TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; }

    }
}
