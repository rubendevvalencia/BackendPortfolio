using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Ecommerce.Test.InfrastructureTest
{
    //Fija que EnableSensitiveDataLogging (DbContextEF, via ConfigureServices) solo se activa en Development:
    //en el resto de entornos, los valores de los parametros (PasswordHash incluido) no deben salir en los logs.
    public class SensitiveDataLoggingTest
    {
        [Theory]
        [InlineData("Development", true)]
        [InlineData("Production", false)]
        public void AddInfrastructureServices_SoloActivaSensitiveDataLoggingEnDevelopment(string environmentName, bool sensitiveDataLoggingEsperado)
        {
            //Arrange
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:EcommerceDb"] = "Server=(local);Database=Fake;Trusted_Connection=True;"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);

            var provider = new ServiceCollection()
                .AddInfrastructureServices(configuration, environment)
                .BuildServiceProvider(validateScopes: true);

            //Act
            using var scope = provider.CreateScope();
            var dbContextOptions = scope.ServiceProvider.GetRequiredService<DbContextOptions<DbContextEF>>();
            var coreOptions = dbContextOptions.FindExtension<CoreOptionsExtension>();

            //Assert
            Assert.NotNull(coreOptions);
            Assert.Equal(sensitiveDataLoggingEsperado, coreOptions.IsSensitiveDataLoggingEnabled);
        }
    }
}
