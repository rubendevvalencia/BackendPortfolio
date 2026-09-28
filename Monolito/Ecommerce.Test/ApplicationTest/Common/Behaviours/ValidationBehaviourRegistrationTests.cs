using Ecommerce.Application;
using Ecommerce.Application.Common.Behaviours;
using Ecommerce.Transversal.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CreateCustomerCommandV3 = Ecommerce.Application.Feature.Customers.Commands.CreateCustomerCommand.CreateCustomerCommand;
using CreateCustomerCommandV4 = Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer.CreateCustomerCommand;

namespace Ecommerce.Test.ApplicationTest.Common.Behaviours
{
    //Test de cableado: comprueba con la DI real a que peticiones se aplica ValidationBehaviour (solo v4).
    public class ValidationBehaviourRegistrationTests
    {
        [Fact]
        public void AddApplicationServices_PeticionV4_IncluyeValidationBehaviour()
        {
            //Arrange: LoggingBehaviour pide ILogger<>, aqui basta con el logger nulo.
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddApplicationServices();
            var provider = services.BuildServiceProvider();

            //Act
            var behaviours = provider.GetServices<IPipelineBehavior<CreateCustomerCommandV4, Response<bool>>>();

            //Assert
            var validationBehaviours = behaviours.OfType<ValidationBehaviour<CreateCustomerCommandV4, Response<bool>>>();
            var vecesValidationBehaviour = validationBehaviours.Count();
            Assert.Equal(1, vecesValidationBehaviour);
        }

        [Fact]
        public void AddApplicationServices_PeticionV3_NoIncluyeValidationBehaviour()
        {
            //Arrange: LoggingBehaviour pide ILogger<>, aqui basta con el logger nulo.
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddApplicationServices();
            var provider = services.BuildServiceProvider();

            //Act
            var behaviours = provider.GetServices<IPipelineBehavior<CreateCustomerCommandV3, Response<bool>>>().ToList();

            //Assert: v3 sigue pasando por LoggingBehaviour, pero no por ValidationBehaviour.
            var loggingBehaviours = behaviours.OfType<LoggingBehaviour<CreateCustomerCommandV3, Response<bool>>>();
            var vecesLoggingBehaviour = loggingBehaviours.Count();
            Assert.Equal(1, vecesLoggingBehaviour);

            var numeroDeBehaviours = behaviours.Count;
            Assert.Equal(1, numeroDeBehaviours);
        }
    }
}
