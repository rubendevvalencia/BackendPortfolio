using Ecommerce.Api.Controllers;
using Ecommerce.Application.Dto.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Ecommerce.IntegrationTest.UserAuthTest
{
    public class SignUpTest
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddUserSecrets<SignUpTest>()
            .Build();
        private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();

        private SignUpDto CreateEntity()
        {
            return new SignUpDto
            {
                FirstName = "test",
                LastName = "test",
                Email = "test@email.com",
                UserName = "test",
                Password = "test1234",
            };       
        }

        [Fact]
        public async Task UserAuthController_PostSignUp_Ok()
        {
            _services.BuildDependencies(_configuration, _environment);

            using(ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                SignUpDto data = CreateEntity();
                var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();
                var result = await signDeparment.SignUpAsync(data);
                Assert.NotNull(result);
            }
        }
    }
}
