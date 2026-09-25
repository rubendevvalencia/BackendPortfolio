using Ecommerce.Api.Controllers;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task UserAuthController_PostSignUpPostSignIn_Ok()
        {
            _services.BuildDependencies(_configuration, _environment);

            using(ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                //Esto te permite instalar y crear las tablas de las migraciones y así no te preocupas de que dé error 400 cuando no toca
                var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
                dbContext.Database.Migrate();

                SignUpDto data = CreateEntity();
                var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();

                try
                {
                    var result = await signDeparment.SignUpAsync(data);

                    var objectResult = result as ObjectResult;
                    var response = objectResult?.Value as Response<bool>;

                    Assert.NotNull(objectResult);
                    Assert.NotNull(response);
                    Assert.True(response.IsSuccess);

                    SignInDto readData = new SignInDto
                    {
                       Email = data.Email,
                       Password = data.Password
                    };
                    var resultRead = await signDeparment.SignInAsync(readData);
                    var objectResultRead = resultRead as ObjectResult;
                    var responseRead = objectResultRead?.Value as Response<TokenDto>;

                    Assert.NotNull(objectResultRead);
                    Assert.NotNull(responseRead);
                    Assert.True(responseRead.IsSuccess);

                    var token = responseRead.Data?.AccessToken;
                    Assert.NotNull(token);
                }
                finally
                {
                    var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == data.Email);
                    if (createdUser != null)
                    {
                        dbContext.Users.Remove(createdUser);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }

        [Fact]
        public async Task UserAuthController_PostSignUpPostSignIn_UserWasRegistered()
        {
            _services.BuildDependencies(_configuration, _environment);

            using(ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                var dbContext = serviceProvider.GetRequiredService<DbContextEF>();    //Esto te permite instalar y crear las tablas de las migraciones y así no te preocupas de que dé error 400 cuando no toca
                dbContext.Database.Migrate(); //Migra automáticamente base de datos y tablas
                SignUpDto data = CreateEntity();

                try
                {                 
                    var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();
                    var result = await signDeparment.SignUpAsync(data);
                    var objectResult = result as ObjectResult;
                    var response = objectResult?.Value as Response<bool>;

                    SignUpDto data2 = CreateEntity();
                    var result2 = await signDeparment.SignUpAsync(data2);
                    var objectResult2 = result2 as ObjectResult;
                    var response2 = objectResult2?.Value as Response<bool>;

                    Assert.NotNull(objectResult2);
                    Assert.NotNull(response2);
                    Assert.False(response2.IsSuccess);
                    Assert.Equal(ErrorType.Validation, response2.ErrorType);

                }
                finally
                {
                    var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == data.Email);
                    if (createdUser != null)
                    {
                        dbContext.Users.Remove(createdUser);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
            
    
        [Theory]
        [InlineData("", "test", "test@email.com", "test", "test1234")]      // FirstName vacío
        [InlineData("test", "test", "no-es-un-email", "test", "test1234")]  // Email con formato inválido
        [InlineData("test", "test", "test@email.com", "test", "1234567")]   // Password de 7 caracteres, por debajo del mínimo
        public async Task UserAuthController_PostSignUp_Invalid(string firstName, string lastName, string email, string userName, string password)
        {
            _services.BuildDependencies(_configuration, _environment);

            using ServiceProvider serviceProvider = _services.BuildServiceProvider();

            var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
            dbContext.Database.Migrate();

            SignUpDto data = new SignUpDto
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = userName,
                Password = password,
            };

            var signDeparment = serviceProvider.GetRequiredService<UserAuthController>();

            var result = await signDeparment.SignUpAsync(data);

            var objectResult = result as ObjectResult;
            var response = objectResult?.Value as Response<bool>;

            Assert.NotNull(objectResult);
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
        }

            
    }
}
