using RegistroPerf.Configuration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistroPerf.Configuration.Services.Interfaces
{
    public interface IEndPointsService
    {
        public EndPointsDefinition ReadUrlAppSettings();
    }
}
