using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Test.InfrastructureTest
{
    //Test de cableado: comprueba que AddInfrastructureServices registra todo y el grafo se resuelve.
    public class ConfigureServicesTest
    {
        //Cadena de conexion falsa: no se abre ninguna conexion porque no se ejecuta ninguna consulta.
        private static IConfiguration CreateConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:EcommerceDb"] = "Server=(local);Database=Fake;Trusted_Connection=True;"
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);

            var configuration = builder.Build();
            return configuration;
        }

        [Fact]
        public void AddInfrastructureServices_ResuelveElGrafoCompleto()
        {
            //Arrange
            var configuration = CreateConfiguration();

            //IConfiguration la registra el host en produccion; aqui se añade a mano.
            var services = new ServiceCollection()
                .AddSingleton(configuration);

            //Act: validateScopes detecta un singleton que atrape al DbContext scoped.
            var provider = services
                .AddInfrastructureServices(configuration)
                .BuildServiceProvider(validateScopes: true);

            //Assert
            using var scope = provider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            Assert.NotNull(unitOfWork);
            Assert.NotNull(unitOfWork._customersUoW);
            Assert.NotNull(unitOfWork._user);
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICustomerRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICustomerRepositoryUoW>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<DbContextEF>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        }

        [Theory]
        [InlineData(typeof(IUnitOfWork))]
        [InlineData(typeof(ICustomerRepository))]
        [InlineData(typeof(ICustomerRepositoryUoW))]
        [InlineData(typeof(IUserRepository))]
        [InlineData(typeof(DbContextEF))]
        [InlineData(typeof(AuditableEntitySaveChangesInterceptor))]
        public void AddInfrastructureServices_RegistraLosServiciosComoScoped(Type serviceType)
        {
            //Arrange & Act
            var services = new ServiceCollection()
                .AddInfrastructureServices(CreateConfiguration());

            //Assert: todo lo que depende del DbContext tiene que ser scoped.
            var descriptor = Assert.Single(services, d => d.ServiceType == serviceType);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddInfrastructureServices_DevuelveLaMismaColeccionParaEncadenar()
        {
            //Arrange
            var services = new ServiceCollection();

            //Act
            var result = services.AddInfrastructureServices(CreateConfiguration());

            //Assert
            Assert.Same(services, result);
        }
    }
}
