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
    //Test de cableado (composition root), no de logica: verifica que AddInfrastructureServices
    //registra todo lo necesario y que el grafo de dependencias se puede resolver.
    //Los tests de repositorio instancian las clases a mano, asi que nunca ejecutan este archivo:
    //sin este test, olvidar un AddScoped no rompe la suite pero si el arranque de la aplicacion.
    public class ConfigureServicesTest
    {
        //La cadena de conexion puede ser falsa: UseSqlServer solo registra el proveedor y no abre
        //ninguna conexion hasta que se ejecuta una consulta, cosa que aqui no ocurre.
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

            //DbContextEF pide IConfiguration por constructor y AddInfrastructureServices no la registra:
            //en produccion la aporta WebApplicationBuilder, asi que aqui se replica ese registro para
            //reproducir el contenedor real. Sin esta linea el grafo no resuelve.
            var services = new ServiceCollection()
                .AddSingleton(configuration);

            //Act
            //validateScopes: true detecta las captive dependencies, por ejemplo un singleton
            //que se quede con el DbContextEF scoped atrapado dentro.
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

            //Assert: scoped es el lifetime correcto para todo lo que cuelga del DbContext,
            //que vive lo que dura una peticion.
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
