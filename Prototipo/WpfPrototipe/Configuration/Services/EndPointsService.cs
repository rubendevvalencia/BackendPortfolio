using RegistroPerf.Configuration.Models;
using RegistroPerf.Configuration.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistroPerf.Configuration.Services
{
    public class EndPointsService : IEndPointsService
    {
        public EndPointsDefinition ReadUrlAppSettings()
        {
            // App.ApiConf ya se cargó del appsettings en OnStartup (y ahí se valida que las claves existan).
            string signUpUrl = App.ApiConf.SignUpUrl;
            string signInUrl = App.ApiConf.SignInUrl;

            EndPointsDefinition endPoints = new EndPointsDefinition(signUpUrl, signInUrl);
            return endPoints;
        }
    }
}
