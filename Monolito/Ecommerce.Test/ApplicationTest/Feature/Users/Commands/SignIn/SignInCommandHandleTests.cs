using AutoMapper;
using Ecommerce.Application.Feature.Users.Commands.SignIn;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ecommerce.Test.ApplicationTest.Feature.Users.Commands.SignIn
{
    //Tests del handler de SignIn con dobles; sin caso de validacion ni de excepciones: eso es del pipeline.
    public class SignInCommandHandleTests : ApplicationTestBase
    {
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IJwtApplication _jwt = Substitute.For<IJwtApplication>();
        private readonly IMediator _mediator = Substitute.For<IMediator>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly ILogger<SignInCommandHandle> _logger = Substitute.For<ILogger<SignInCommandHandle>>();
        private readonly SignInCommandHandle _handler;

        //xUnit crea una instancia por test: los dobles empiezan limpios.
        public SignInCommandHandleTests()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._user.Returns(_userRepository);

            //Comprobacion: si no devolviera nuestro doble, ningun test probaria nada.
            var repositorioDelUnitOfWork = _unitOfWork._user;
            if (repositorioDelUnitOfWork != _userRepository) throw new InvalidOperationException("Arrange mal montado: _unitOfWork._user no devuelve el doble de IUserRepository.");
            _handler = new SignInCommandHandle(_mediator, _unitOfWork, _mapper, _logger, _jwt);
        }

        [Fact]
        public async Task Handle_DevuelveElTokenCuandoLasCredencialesSonValidas()
        {
            //Arrange: el usuario existe y la contrasena es correcta.
            User usuario = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuario);
            _userRepository.CheckPass(usuario, ValidPassword).Returns(true);
            _jwt.GenerateToken(usuario).Returns(("token-firmado", 3600));

            //Comprobacion del Arrange: las tres piezas de la cadena.
            var usuarioEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (usuarioEncontrado != usuario) throw new InvalidOperationException("Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            
            var contrasenaValida = _userRepository.CheckPass(usuario, ValidPassword);
            if (!contrasenaValida) throw new InvalidOperationException("Arrange mal montado: la contrasena del test tiene que darse por valida.");
        
            var tokenGenerado = _jwt.GenerateToken(usuario);
            if (tokenGenerado.Item1 != "token-firmado" || tokenGenerado.Item2 != 3600) throw new InvalidOperationException("Arrange mal montado: IJwtApplication no devuelve el token de prueba.");
            
            //Se limpian las llamadas por el Received(1).GenerateToken de abajo.
            _jwt.ClearReceivedCalls();

            var command = new SignInCommand { Email = "ruben@test.com", Password = ValidPassword };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: el TokenDto se arma con lo que devuelve IJwtApplication y el tipo es fijo.
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal("token-firmado", response.Data.AccessToken);
            Assert.Equal(3600, response.Data.ExpiresIn);
            Assert.Equal("Bearer", response.Data.TokenType);

            //Se firma para el usuario que devolvio el repositorio.
            _jwt.Received(1).GenerateToken(usuario);
        }

        [Fact]
        public async Task Handle_NoCompruebaLaContrasenaCuandoElUsuarioNoExiste()
        {
            //Arrange: no hay ningun usuario con ese email.
            _userRepository.GetByEmailAsync("fantasma@test.com").Returns((User?)null);

            //Comprobacion del Arrange.
            var usuarioEncontrado = await _userRepository.GetByEmailAsync("fantasma@test.com");
            if (usuarioEncontrado is not null)
            {
                throw new InvalidOperationException("Arrange mal montado: fantasma@test.com no deberia estar registrado.");
            }

            var command = new SignInCommand { Email = "fantasma@test.com", Password = ValidPassword };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: Unauthorized y ni se comprueba la contrasena ni se firma nada.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unauthorized, response.ErrorType);
            Assert.Equal("Invalid credentials", response.Message);
            _userRepository.DidNotReceive().CheckPass(Arg.Any<User>(), Arg.Any<string>());
            _jwt.DidNotReceive().GenerateToken(Arg.Any<User>());
        }

        [Fact]
        public async Task Handle_NoFirmaTokenCuandoLaContrasenaEsIncorrecta()
        {
            //Arrange: el usuario existe, pero la comprobacion del hash falla.
            User usuario = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuario);
            _userRepository.CheckPass(usuario, "OtraPassword1!").Returns(false);

            //Comprobacion del Arrange.
            var usuarioEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (usuarioEncontrado != usuario)
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            var contrasenaValida = _userRepository.CheckPass(usuario, "OtraPassword1!");
            if (contrasenaValida)
            {
                throw new InvalidOperationException("Arrange mal montado: la contrasena del test tiene que darse por invalida.");
            }

            var command = new SignInCommand { Email = "ruben@test.com", Password = "OtraPassword1!" };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: lo que importa es que de aqui no sale ningun token.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unauthorized, response.ErrorType);
            Assert.Null(response.Data);
            _jwt.DidNotReceive().GenerateToken(Arg.Any<User>());
        }

        //SignIn no distingue email inexistente de contrasena incorrecta.
        [Fact]
        public async Task Handle_NoDistingueEmailInexistenteDeContrasenaIncorrecta()
        {
            //Arrange: dos intentos fallidos que solo se diferencian en si el email existe o no.
            User registrado = NewUser(email: "registrado@test.com");
            _userRepository.GetByEmailAsync("fantasma@test.com").Returns((User?)null);
            _userRepository.GetByEmailAsync("registrado@test.com").Returns(registrado);
            _userRepository.CheckPass(registrado, ValidPassword).Returns(false);

            //Comprobacion del Arrange: los dos escenarios tienen que estar bien montados para compararlos.
            var emailInexistenteEncontrado = await _userRepository.GetByEmailAsync("fantasma@test.com");
            if (emailInexistenteEncontrado is not null)
            {
                throw new InvalidOperationException("Arrange mal montado: fantasma@test.com no deberia estar registrado.");
            }

            var emailRegistradoEncontrado = await _userRepository.GetByEmailAsync("registrado@test.com");
            if (emailRegistradoEncontrado != registrado)
            {
                throw new InvalidOperationException("Arrange mal montado: registrado@test.com si deberia estar registrado.");
            }

            var contrasenaValida = _userRepository.CheckPass(registrado, ValidPassword);
            if (contrasenaValida)
            {
                throw new InvalidOperationException("Arrange mal montado: la contrasena del test tiene que darse por invalida.");
            }

            var commandEmailInexistente = new SignInCommand { Email = "fantasma@test.com", Password = ValidPassword };
            var commandContrasenaIncorrecta = new SignInCommand { Email = "registrado@test.com", Password = ValidPassword };

            //Act
            var emailInexistente = await _handler.Handle(commandEmailInexistente, CancellationToken.None);
            var contrasenaIncorrecta = await _handler.Handle(commandContrasenaIncorrecta, CancellationToken.None);

            //Assert: los dos fallan igual, sin ninguna pista de cual de las dos cosas fallo.
            Assert.False(emailInexistente.IsSuccess);
            Assert.False(contrasenaIncorrecta.IsSuccess);
            Assert.Equal(ErrorType.Unauthorized, emailInexistente.ErrorType);
            Assert.Equal(ErrorType.Unauthorized, contrasenaIncorrecta.ErrorType);
            Assert.Equal(emailInexistente.Message, contrasenaIncorrecta.Message);
        }
    }
}
