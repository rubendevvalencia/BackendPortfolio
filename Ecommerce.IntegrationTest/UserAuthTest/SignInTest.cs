using Ecommerce.Api.Controllers;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Ecommerce.IntegrationTest.UserAuthTest
{
    public class SignInTest
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddUserSecrets<SignInTest>()
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

        [Theory]
        [InlineData("", "test1234")]               // Email vacío
        [InlineData("no-es-un-email", "test1234")]  // Email con formato inválido
        [InlineData("test@email.com", "corta")]     // Password de 5 caracteres, por debajo del mínimo
        public async Task UserAuthController_PostSignIn_Invalid(string email, string password)
        {
            _services.BuildDependencies(_configuration, _environment);

            using ServiceProvider serviceProvider = _services.BuildServiceProvider();

            var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
            dbContext.Database.Migrate();

            SignInDto data = new SignInDto
            {
                Email = email,
                Password = password,
            };

            var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();

            var result = await signDeparment.SignInAsync(data);

            var objectResult = result as ObjectResult;
            var response = objectResult?.Value as Response<TokenDto>;

            Assert.NotNull(objectResult);
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task UserAuthController_PostSignIn_ContrasenaIncorrecta_Unauthorized()
        {
            _services.BuildDependencies(_configuration, _environment);

            using (ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
                dbContext.Database.Migrate();

                SignUpDto signUpData = CreateEntity();
                var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();

                try
                {
                    await signDeparment.SignUpAsync(signUpData);

                    SignInDto wrongPasswordData = new SignInDto
                    {
                        Email = signUpData.Email,
                        Password = "otraPassword1",
                    };

                    var result = await signDeparment.SignInAsync(wrongPasswordData);

                    var objectResult = result as ObjectResult;
                    var response = objectResult?.Value as Response<TokenDto>;

                    Assert.NotNull(objectResult);
                    Assert.NotNull(response);
                    Assert.False(response.IsSuccess);
                    Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
                }
                finally
                {
                    var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == signUpData.Email);
                    if (createdUser != null)
                    {
                        dbContext.Users.Remove(createdUser);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
        
        [Fact]
        public async Task UserAuthController_PostSignIn_EmailInexistente_Unauthorized()
        {
            _services.BuildDependencies(_configuration, _environment);

            using ServiceProvider serviceProvider = _services.BuildServiceProvider();

            var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
            dbContext.Database.Migrate();

            SignInDto unknownEmailData = new SignInDto
            {
                Email = "fantasma@email.com",
                Password = "test1234",
            };

            var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();

            var result = await signDeparment.SignInAsync(unknownEmailData);

            var objectResult = result as ObjectResult;
            var response = objectResult?.Value as Response<TokenDto>;

            Assert.NotNull(objectResult);
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        }

        [Fact]
        public async Task UserAuthController_PostSignIn_ContrasenaIncorrectaYEmailInexistente_MismoMensaje()
        {
            _services.BuildDependencies(_configuration, _environment);

            using (ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
                dbContext.Database.Migrate();

                SignUpDto signUpData = CreateEntity();
                var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();

                try
                {
                    await signDeparment.SignUpAsync(signUpData);

                    SignInDto wrongPasswordData = new SignInDto
                    {
                        Email = signUpData.Email,
                        Password = "otraPassword1",
                    };
                    SignInDto unknownEmailData = new SignInDto
                    {
                        Email = "fantasma@email.com",
                        Password = signUpData.Password,
                    };
                    
                    var wrongPasswordResult = await signDeparment.SignInAsync(wrongPasswordData);
                    var wrongPasswordObjectResult = wrongPasswordResult as ObjectResult;
                    var wrongPasswordResponse = wrongPasswordObjectResult?.Value as Response<TokenDto>;
                    
                    var unknownEmailResult = await signDeparment.SignInAsync(unknownEmailData);
                    var unknownEmailObjectResult = unknownEmailResult as ObjectResult;
                    var unknownEmailResponse = unknownEmailObjectResult?.Value as Response<TokenDto>;

                    Assert.NotNull(wrongPasswordResponse);
                    Assert.NotNull(unknownEmailResponse);
                    Assert.Equal(unknownEmailResponse.Message, wrongPasswordResponse.Message);
                }
                finally
                {
                    var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == signUpData.Email);
                    if (createdUser != null)
                    {
                        dbContext.Users.Remove(createdUser);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
