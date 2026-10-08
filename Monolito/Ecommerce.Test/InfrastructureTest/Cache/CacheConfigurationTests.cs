using Ecommerce.Infrastructure.Data.Cache;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Ecommerce.Test.InfrastructureTest.Cache
{
    //Tests de la lectura de caducidades con un IConfiguration sustituido: cada test fija las claves que necesita.
    public class CacheConfigurationTests
    {
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

        [Theory]
        [InlineData(eCacheKey.Default, "Cache:Default", "02:00:00", "01:00:00", 120, 60)]
        [InlineData(eCacheKey.CustomerAll, "Cache:Policies:CustomerAll", "01:00:00", "00:12:00", 60, 12)]
        public void Configuration_SegunLaClave_DevuelveSusCaducidades(eCacheKey key, string prefijo, string absoluteTime, string slidingTime, int absoluteMinutos, int slidingMinutos)
        {
            //Arrange: solo se fijan las claves de esta fila; si el codigo leyera las de la otra clave recibiria null y fallaria.
            _configuration[$"{prefijo}:AbsoluteExpiration"].Returns(absoluteTime);
            _configuration[$"{prefijo}:SlidingExpiration"].Returns(slidingTime);

            //Act
            var result = CacheConfiguration.Configuration(key, _configuration);

            //Assert: [0] es la absoluta y [1] la deslizante.
            Assert.Equal(2, result.Length);
            Assert.Equal(TimeSpan.FromMinutes(absoluteMinutos), result[0]);
            Assert.Equal(TimeSpan.FromMinutes(slidingMinutos), result[1]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("30")]         //TryParse lo leeria como 30 dias; con el formato exacto falla
        [InlineData("abc")]
        [InlineData("1.00:00:00")] //Con dias tampoco vale
        [InlineData("25:00:00")]   //Horas fuera de rango
        public void Configuration_AbsoluteExpirationInvalida_LanzaInvalidOperationException(string? absoluteTime)
        {
            //Arrange: la deslizante es valida, solo falla la absoluta.
            _configuration["Cache:Default:AbsoluteExpiration"].Returns(absoluteTime);
            _configuration["Cache:Default:SlidingExpiration"].Returns("01:00:00");

            //Act
            var excepcion = Assert.Throws<InvalidOperationException>(() => CacheConfiguration.Configuration(eCacheKey.Default, _configuration));

            //Assert: el mensaje dice cual de las dos falla y de que clave.
            Assert.Contains("AbsoluteExpiration", excepcion.Message);
            Assert.Contains("'Default'", excepcion.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("30")]
        [InlineData("abc")]
        [InlineData("1.00:00:00")]
        [InlineData("25:00:00")]
        public void Configuration_SlidingExpirationInvalida_LanzaInvalidOperationException(string? slidingText)
        {
            //Arrange: la absoluta es valida, solo falla la deslizante.
            _configuration["Cache:Policies:CustomerAll:AbsoluteExpiration"].Returns("01:00:00");
            _configuration["Cache:Policies:CustomerAll:SlidingExpiration"].Returns(slidingText);

            //Act
            var excepcion = Assert.Throws<InvalidOperationException>(() => CacheConfiguration.Configuration(eCacheKey.CustomerAll, _configuration));

            //Assert: el mensaje dice cual de las dos falla y de que clave.
            Assert.Contains("SlidingExpiration", excepcion.Message);
            Assert.Contains("'CustomerAll'", excepcion.Message);
        }
    }
}
