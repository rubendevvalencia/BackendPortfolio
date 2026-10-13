using Ecommerce.Application.Feature.Users.Commands.Delete;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure.Repository;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;

namespace Ecommerce.Test.ApplicationTest.Feature.Users.Commands.Delete
{
    //Tests del handler de Delete con dobles; sin caso de validacion ni de excepciones: eso es del pipeline.
    public class DeleteCommandHandleTests : ApplicationTestBase
    {
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly DeleteCommandHandle _handler;

        //xUnit crea una instancia por test: los dobles empiezan limpios.
        public DeleteCommandHandleTests()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._user.Returns(_userRepository);
            _handler = new DeleteCommandHandle(_unitOfWork);
        }

        [Fact]
        public async Task Handle_DevuelveExitoCuandoElBorradoEscribeFilas()
        {
            //Arrange: el usuario existe, la contrasena es valida, el commit escribe una fila y despues ya no se encuentra.
            User user = NewUser(id: 7, email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(user);
            _userRepository.CheckPass(user, "Password123!").Returns(true);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
            _userRepository.GetByIdAsync(7).Returns((User?)null);
            var command = new DeleteCommand { Email = "ruben@test.com", Password = "Password123!" };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: exito con Data en true, y el usuario se marca una vez y se confirma una vez.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            _userRepository.Received(1).DeleteUser(user);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_RechazaElBorradoCuandoElEmailNoExiste()
        {
            //Arrange: no hay ningun usuario con ese email (un usuario borrado tampoco, por el filtro global).
            _userRepository.GetByEmailAsync("nadie@test.com").Returns((User?)null);
            var command = new DeleteCommand { Email = "nadie@test.com", Password = "Password123!" };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: fallo Unauthorized y no se comprueba la contrasena ni se toca la base de datos.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unauthorized, response.ErrorType);
            Assert.Equal("Invalid credentials", response.Message);
            _userRepository.DidNotReceive().CheckPass(Arg.Any<User>(), Arg.Any<string>());
            _userRepository.DidNotReceive().DeleteUser(Arg.Any<User>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_RechazaElBorradoCuandoLaContrasenaEsIncorrecta()
        {
            //Arrange: el usuario existe, pero la contrasena no coincide.
            User user = NewUser(id: 7, email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(user);
            _userRepository.CheckPass(user, "Incorrecta1!").Returns(false);
            var command = new DeleteCommand { Email = "ruben@test.com", Password = "Incorrecta1!" };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: mismo fallo y mismo mensaje que con un email inexistente, para no revelar que la cuenta existe.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unauthorized, response.ErrorType);
            Assert.Equal("Invalid credentials", response.Message);
            _userRepository.DidNotReceive().DeleteUser(Arg.Any<User>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_DevuelveFalloCuandoElCommitNoEscribeFilas()
        {
            //Arrange: credenciales validas, pero EF no escribe ninguna fila.
            User user = NewUser(id: 7, email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(user);
            _userRepository.CheckPass(user, "Password123!").Returns(true);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);
            _userRepository.GetByIdAsync(7).Returns((User?)null);
            var command = new DeleteCommand { Email = "ruben@test.com", Password = "Password123!" };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: 0 filas es un fallo Unexpected, aunque el usuario ya no se encuentre.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            _userRepository.Received(1).DeleteUser(user);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_DevuelveFalloCuandoElUsuarioSigueExistiendoTrasElCommit()
        {
            //Arrange: el commit escribe una fila, pero el usuario se sigue encontrando despues.
            User user = NewUser(id: 7, email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(user);
            _userRepository.CheckPass(user, "Password123!").Returns(true);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
            _userRepository.GetByIdAsync(7).Returns(user);
            var command = new DeleteCommand { Email = "ruben@test.com", Password = "Password123!" };

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: el borrado no se da por bueno si el usuario sigue visible.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
        }

        [Fact]
        public async Task Handle_PasaElCancellationTokenAlCommit()
        {
            //Arrange: credenciales validas y un token propio, distinto de CancellationToken.None.
            User user = NewUser(id: 7, email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(user);
            _userRepository.CheckPass(user, "Password123!").Returns(true);
            using var origenDelToken = new CancellationTokenSource();
            CancellationToken token = origenDelToken.Token;
            _unitOfWork.SaveChangesAsync(token).Returns(1);
            _userRepository.GetByIdAsync(7).Returns((User?)null);
            var command = new DeleteCommand { Email = "ruben@test.com", Password = "Password123!" };

            //Act
            await _handler.Handle(command, token);

            //Assert: el commit recibe el mismo token que el handler, no CancellationToken.None.
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }
    }
}
