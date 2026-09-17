using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace Ecommerce.Api.Modules.HealthCheck
{
    public class HealthCheckCustome : IHealthCheck
    {
        //Vamos a usar Random para validar distintos tiempos de respuesta y ver como se comporta.
        private readonly Random _random = new Random();
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            //Aquí configuras la lógica para comprobar con cualquier servicio que quieras comprobar su conexión, api, db, etc..
            var time = _random.Next(1, 200); //ms

            if(time<100) return Task.FromResult(HealthCheckResult.Healthy("Healthy from HealthCheckCustome-1"));
            if (time < 200) return Task.FromResult(HealthCheckResult.Degraded("Degraded from HealthCheckCustome-1"));
            return Task.FromResult(HealthCheckResult.Unhealthy("Unhealthy from HealthCheckCustome-1"));

        }
    }
}
