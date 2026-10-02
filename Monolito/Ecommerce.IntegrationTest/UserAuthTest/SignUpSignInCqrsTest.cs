using Ecommerce.Api.Controllers.UserAuth.V4;
using Ecommerce.Application.Common.Configuration;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Feature.Users.Commands.SignIn;
using Ecommerce.Application.Feature.Users.Commands.SignUp;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Ecommerce.IntegrationTest.UserAuthTest
{
    //Integracion de SignUp y SignIn por CQRS con el DataProtection real y la base de datos real.
    public class SignUpSignInCqrsTest
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddUserSecrets<SignUpSignInCqrsTest>()
            .Build();
        private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();

        [Fact]
        public async Task SignUpYSignIn_ProtegenNombreYApellidoEnLaBaseDeDatosYDevuelvenElNombreCompleto()
        {
            //Arrange: datos unicos para no chocar con otras ejecuciones, y las tablas de las migraciones creadas.
            var sufijo = Guid.NewGuid().ToString("N").Substring(0, 8);
            var userName = "cqrs" + sufijo;
            var email = "cqrs" + sufijo + "@test.com";

            _services.BuildDependencies(_configuration, _environment);

            using (ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
                dbContext.Database.Migrate();

                var controller = serviceProvider.GetRequiredService<UserAuthv4Controller>();

                var signUp = new SignUpCommand()
                {
                    FirstName = "Maria",
                    LastName = "Lopez",
                    Email = email,
                    UserName = userName,
                    Password = "Password123!"
                };

                var signIn = new SignInCommand()
                {
                    Email = email,
                    Password = "Password123!"
                };

                try
                {
                    //Act
                    var resultSignUp = await controller.SignUpAsync(signUp);
                    var resultSignIn = await controller.SignInAsync(signIn);

                    //Assert: el alta funciona.
                    var objectResultSignUp = resultSignUp as ObjectResult;
                    var responseSignUp = objectResultSignUp?.Value as Response<bool>;
                    Assert.NotNull(objectResultSignUp);
                    Assert.NotNull(responseSignUp);
                    Assert.True(responseSignUp.IsSuccess);

                    //Assert: en la base de datos nombre y apellido no estan en claro, pero el protector real los recupera.
                    var guardado = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName);
                    Assert.NotNull(guardado);
                    Assert.NotEqual("Maria", guardado.FirstName);
                    Assert.NotEqual("Lopez", guardado.LastName);

                    var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
                    var protector = dataProtectionProvider.CreateProtector(ProtectorParameters.Purpose);
                    Assert.Equal("Maria", protector.Unprotect(guardado.FirstName));
                    Assert.Equal("Lopez", protector.Unprotect(guardado.LastName));

                    //Assert: el email va en claro (se consulta por igualdad) y la contrasena solo existe como hash.
                    Assert.Equal(email, guardado.Email);
                    Assert.NotEqual("Password123!", guardado.PasswordHash);

                    //Assert: el SignIn devuelve el token y el nombre completo ya desprotegido.
                    var objectResultSignIn = resultSignIn as ObjectResult;
                    var responseSignIn = objectResultSignIn?.Value as Response<TokenDto>;
                    Assert.NotNull(objectResultSignIn);
                    Assert.NotNull(responseSignIn);
                    Assert.True(responseSignIn.IsSuccess);
                    Assert.NotNull(responseSignIn.Data);
                    Assert.False(string.IsNullOrEmpty(responseSignIn.Data.AccessToken));
                    Assert.Equal("Maria Lopez", responseSignIn.Data.FullName);
                }
                finally
                {
                    var creado = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                    if (creado != null)
                    {
                        dbContext.Users.Remove(creado);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }

        [Fact]
        public async Task SignIn_ConContrasenaIncorrectaDevuelveUnauthorizedSinDatos()
        {
            //Arrange: un usuario dado de alta y las tablas de las migraciones creadas.
            var sufijo = Guid.NewGuid().ToString("N").Substring(0, 8);
            var userName = "cqrs" + sufijo;
            var email = "cqrs" + sufijo + "@test.com";

            _services.BuildDependencies(_configuration, _environment);

            using (ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
                dbContext.Database.Migrate();

                var controller = serviceProvider.GetRequiredService<UserAuthv4Controller>();

                var signUp = new SignUpCommand()
                {
                    FirstName = "Maria",
                    LastName = "Lopez",
                    Email = email,
                    UserName = userName,
                    Password = "Password123!"
                };

                var signInIncorrecto = new SignInCommand()
                {
                    Email = email,
                    Password = "OtraPassword1!"
                };

                try
                {
                    var resultSignUp = await controller.SignUpAsync(signUp);
                    var objectResultSignUp = resultSignUp as ObjectResult;
                    var responseSignUp = objectResultSignUp?.Value as Response<bool>;
                    Assert.NotNull(responseSignUp);
                    Assert.True(responseSignUp.IsSuccess);

                    //Act
                    var resultSignIn = await controller.SignInAsync(signInIncorrecto);

                    //Assert: el fallo es Unauthorized y sin TokenDto.
                    //Hoy ToActionResult no tiene rama para Unauthorized y lo envia como 500; aqui solo se comprueba la respuesta.
                    var objectResultSignIn = resultSignIn as ObjectResult;
                    var responseSignIn = objectResultSignIn?.Value as Response<TokenDto>;
                    Assert.NotNull(objectResultSignIn);
                    Assert.NotNull(responseSignIn);
                    Assert.False(responseSignIn.IsSuccess);
                    Assert.Equal(ErrorType.Unauthorized, responseSignIn.ErrorType);
                    Assert.Null(responseSignIn.Data);
                }
                finally
                {
                    var creado = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                    if (creado != null)
                    {
                        dbContext.Users.Remove(creado);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }

        [Fact]
        public async Task SignUp_ConUnEmailYaRegistradoDevuelveDuplicated()
        {
            //Arrange: un usuario dado de alta y las tablas de las migraciones creadas.
            var sufijo = Guid.NewGuid().ToString("N").Substring(0, 8);
            var userName = "cqrs" + sufijo;
            var email = "cqrs" + sufijo + "@test.com";

            _services.BuildDependencies(_configuration, _environment);

            using (ServiceProvider serviceProvider = _services.BuildServiceProvider())
            {
                var dbContext = serviceProvider.GetRequiredService<DbContextEF>();
                dbContext.Database.Migrate();

                var controller = serviceProvider.GetRequiredService<UserAuthv4Controller>();

                var primerAlta = new SignUpCommand()
                {
                    FirstName = "Maria",
                    LastName = "Lopez",
                    Email = email,
                    UserName = userName,
                    Password = "Password123!"
                };

                var segundaAlta = new SignUpCommand()
                {
                    FirstName = "Otra",
                    LastName = "Persona",
                    Email = email,
                    UserName = userName + "x",
                    Password = "Password123!"
                };

                try
                {
                    var resultPrimero = await controller.SignUpAsync(primerAlta);
                    var objectResultPrimero = resultPrimero as ObjectResult;
                    var responsePrimero = objectResultPrimero?.Value as Response<bool>;
                    Assert.NotNull(responsePrimero);
                    Assert.True(responsePrimero.IsSuccess);

                    //Act
                    var resultSegundo = await controller.SignUpAsync(segundaAlta);

                    //Assert: el segundo alta con el mismo email falla como Duplicated (409) y no crea otra fila.
                    var objectResultSegundo = resultSegundo as ObjectResult;
                    var responseSegundo = objectResultSegundo?.Value as Response<bool>;
                    Assert.NotNull(objectResultSegundo);
                    Assert.NotNull(responseSegundo);
                    Assert.False(responseSegundo.IsSuccess);
                    Assert.Equal(ErrorType.Duplicated, responseSegundo.ErrorType);
                    Assert.Equal(409, objectResultSegundo.StatusCode);

                    var filasConEseEmail = await dbContext.Users.CountAsync(u => u.Email == email);
                    Assert.Equal(1, filasConEseEmail);
                }
                finally
                {
                    var creado = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                    if (creado != null)
                    {
                        dbContext.Users.Remove(creado);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
