using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistroPerf.Configuration.Models
{
    public class EndPointsDefinition
    {
        public string SignUpUrl { get; }
        public string SignInUrl { get; }

        public EndPointsDefinition(string signUpUrl, string signInUrl)
        {
            SignUpUrl = signUpUrl;
            SignInUrl = signInUrl;
        }
    }
}
