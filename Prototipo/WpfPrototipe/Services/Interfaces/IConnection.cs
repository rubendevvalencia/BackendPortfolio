using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RegistroPerf.Services.Interfaces
{
    public interface IConnection
    {
        public HttpClient CreateClient(string url);
    }
}
