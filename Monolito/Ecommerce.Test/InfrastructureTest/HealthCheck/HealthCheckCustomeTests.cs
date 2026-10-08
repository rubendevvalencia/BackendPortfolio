using Ecommerce.Infrastructure.HealthCheck;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Ecommerce.Test.InfrastructureTest.HealthCheck
{
    //Tests de los ifs de tiempo del check: el Random se sustituye para que devuelva siempre el valor que se quiere probar.
    public class HealthCheckCustomeTests
    {
        private readonly Random _random = Substitute.For<Random>();
        private readonly HealthCheckCustome _healthCheck;

        public HealthCheckCustomeTests()
        {
            _healthCheck = new HealthCheckCustome(_random);
        }

        [Theory]
        [InlineData(90, HealthStatus.Healthy, "Healthy from HealthCheckCustome-1")]     //Menor que 100
        [InlineData(150, HealthStatus.Degraded, "Degraded from HealthCheckCustome-1")]  //Entre 100 y 200
        [InlineData(220, HealthStatus.Unhealthy, "Unhealthy from HealthCheckCustome-1")] //200 o mas
        public async Task CheckHealthAsync_SegunElTiempo_DevuelveElEstadoEsperado(int tiempo, HealthStatus estadoEsperado, string descripcionEsperada)
        {
            //Arrange: el Random devuelve siempre el tiempo de la fila.
            _random.Next(1, 300).Returns(tiempo);
            var context = new HealthCheckContext();

            //Act
            var result = await _healthCheck.CheckHealthAsync(context, CancellationToken.None);

            //Assert
            Assert.Equal(estadoEsperado, result.Status);
            Assert.Equal(descripcionEsperada, result.Description);
        }
    }
}
