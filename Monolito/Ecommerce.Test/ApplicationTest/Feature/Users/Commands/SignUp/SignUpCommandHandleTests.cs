using Ecommerce.Application.Feature.Users.Commands.SignUp;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ecommerce.Test.ApplicationTest.Feature.Users.Commands.SignUp
{
    //Tests del handler de SignUp con dobles; sin caso de validacion ni de excepciones: eso es del pipeline.
    public class SignUpCommandHandleTests : ApplicationTestBase
    {
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly ILogger<SignUpCommandHandle> _logger = Substitute.For<ILogger<SignUpCommandHandle>>();
        private readonly SignUpCommandHandle _handler;

        //xUnit crea una instancia por test: los dobles empiezan limpios.
        public SignUpCommandHandleTests()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._user.Returns(_userRepository);

            //Comprobacion: si no devolviera nuestro doble, ningun test probaria nada.
            var repositorioDelUnitOfWork = _unitOfWork._user;
            if (repositorioDelUnitOfWork != _userRepository)
            {
                throw new InvalidOperationException("Arrange mal montado: _unitOfWork._user no devuelve el doble de IUserRepository.");
            }

            _handler = new SignUpCommandHandle(_unitOfWork, Mapper, _logger);
        }

        [Fact]
        public async Task Handle_DevuelveExitoCuandoElCommitEscribe()
        {
            //Arrange: email y username libres, y el commit escribe una fila.
            _userRepository.GetByEmailAsync("nuevo@test.com").Returns((User?)null);
            _userRepository.GetByUserNameAsync("nuevo").Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Se guarda el User que recibe el repositorio para mirarlo despues del Act.
            User? usuarioRegistrado = null;
            _userRepository
                .When(repo => repo.CreateUserAsync(Arg.Any<User>()))
                .Do(llamada => usuarioRegistrado = llamada.Arg<User>());

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("nuevo@test.com");
            if (emailEncontrado is not null)
            {
                throw new InvalidOperationException("Arrange mal montado: el email nuevo@test.com tiene que estar libre.");
            }

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("nuevo");
            if (userNameEncontrado is not null)
            {
                throw new InvalidOperationException("Arrange mal montado: el username 'nuevo' tiene que estar libre.");
            }

            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1)
            {
                throw new InvalidOperationException($"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");
            }

            //Se limpian las llamadas porque las consultas de arriba contarian para los Received(1) de abajo.
            _unitOfWork.ClearReceivedCalls();
            _userRepository.ClearReceivedCalls();

            SignUpCommand command = NewSignUpCommand(email: "nuevo@test.com", userName: "nuevo");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: la respuesta, el mapeo real de comando a entidad y un solo registro y commit.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(usuarioRegistrado);
            Assert.Equal("nuevo@test.com", usuarioRegistrado.Email);
            Assert.Equal("nuevo", usuarioRegistrado.UserName);
            await _userRepository.Received(1).CreateUserAsync(Arg.Any<User>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_RechazaElAltaCuandoElEmailYaEstaRegistrado()
        {
            //Arrange: ya hay un usuario con ese email.
            User usuarioExistente = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuarioExistente);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (emailEncontrado != usuarioExistente)
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            SignUpCommand command = NewSignUpCommand(email: "ruben@test.com", userName: "otro");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: fallo Duplicated y nada llega a la base de datos.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Duplicated, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_RechazaElAltaCuandoElUserNameYaEstaRegistrado()
        {
            //Arrange: el email esta libre y el username no.
            User usuarioExistente = NewUser(userName: "ruben");
            _userRepository.GetByEmailAsync("otro@test.com").Returns((User?)null);
            _userRepository.GetByUserNameAsync("ruben").Returns(usuarioExistente);

            //Comprobacion del Arrange: las dos consultas, porque el escenario es la combinacion.
            var emailEncontrado = await _userRepository.GetByEmailAsync("otro@test.com");
            if (emailEncontrado is not null)
            {
                throw new InvalidOperationException("Arrange mal montado: el email otro@test.com tiene que estar libre.");
            }

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("ruben");
            if (userNameEncontrado != usuarioExistente)
            {
                throw new InvalidOperationException("Arrange mal montado: el username 'ruben' tiene que estar cogido.");
            }

            SignUpCommand command = NewSignUpCommand(email: "otro@test.com", userName: "ruben");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Duplicated, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_DevuelveFalloCuandoElCommitNoEscribeFilas()
        {
            //Arrange: email y username libres, pero EF no escribe ninguna fila.
            _userRepository.GetByEmailAsync("nuevo@test.com").Returns((User?)null);
            _userRepository.GetByUserNameAsync("nuevo").Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            //Comprobacion del Arrange: es justo el valor que define este escenario.
            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 0)
            {
                throw new InvalidOperationException($"Arrange mal montado: el commit no deberia escribir ninguna fila y escribe {filasEscritas}.");
            }

            SignUpCommand command = NewSignUpCommand(email: "nuevo@test.com", userName: "nuevo");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: en un alta, 0 filas si es un fallo.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Failed to create user", response.Message);
        }

        //Defecto de seguridad pendiente: "User already exists" confirma que un email tiene cuenta.
        [Fact]
        public async Task Handle_HoyConfirmaQueUnEmailYaEstaRegistrado_DefectoDeSeguridad()
        {
            //Arrange
            User usuarioExistente = NewUser(email: "victima@test.com");
            _userRepository.GetByEmailAsync("victima@test.com").Returns(usuarioExistente);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("victima@test.com");
            if (emailEncontrado != usuarioExistente)
            {
                throw new InvalidOperationException("Arrange mal montado: victima@test.com deberia estar ya registrado.");
            }

            SignUpCommand command = NewSignUpCommand(email: "victima@test.com", userName: "otro");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: comportamiento ACTUAL. El mensaje distingue este caso de cualquier otro fallo.
            Assert.Equal("User already exists", response.Message);
        }
    }
}
