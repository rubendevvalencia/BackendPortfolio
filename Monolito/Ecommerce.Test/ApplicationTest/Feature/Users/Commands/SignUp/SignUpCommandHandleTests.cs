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
            if (repositorioDelUnitOfWork != _userRepository) throw new InvalidOperationException("Arrange mal montado: _unitOfWork._user no devuelve el doble de IUserRepository.");

            _handler = new SignUpCommandHandle(_unitOfWork, Mapper, _logger);
        }

        [Fact]
        public async Task Handle_DevuelveExitoCuandoElCommitEscribe()
        {
            //Arrange: email y username libres, y el commit escribe una fila.
            _userRepository.GetByEmailAsync("nuevo@test.com").Returns((User?)null);
            _userRepository.GetByUserNameAsync("nuevo").Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("nuevo@test.com");
            if (emailEncontrado is not null) throw new InvalidOperationException("Arrange mal montado: el email nuevo@test.com tiene que estar libre.");

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("nuevo");
            if (userNameEncontrado is not null) throw new InvalidOperationException("Arrange mal montado: el username 'nuevo' tiene que estar libre.");

            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1) throw new InvalidOperationException($"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");

            //Se registra lo que reciben el repositorio y el commit. Va despues de la comprobacion para que sus llamadas no cuenten.
            User? usuarioRegistrado = null;
            int contCreateUser = 0;
            int contSaveChanges = 0;
            _userRepository
                .When(repo => repo.CreateUserAsync(Arg.Any<User>()))
                .Do(llamada =>
                {
                    usuarioRegistrado = llamada.Arg<User>();
                    contCreateUser++;
                });

            _unitOfWork
                .When(uow => uow.SaveChangesAsync(Arg.Any<CancellationToken>()))
                .Do(llamada => contSaveChanges++);

            var command = NewSignUpCommand(email: "nuevo@test.com", userName: "nuevo");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: la respuesta, el mapeo real de comando a entidad y un solo registro y commit.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(usuarioRegistrado);
            Assert.Equal("nuevo@test.com", usuarioRegistrado.Email);
            Assert.Equal("nuevo", usuarioRegistrado.UserName);
            Assert.Equal(1, contCreateUser);
            Assert.Equal(1, contSaveChanges);
        }

        [Fact]
        public async Task Handle_RechazaElAltaCuandoElEmailYaEstaRegistrado()
        {
            //Arrange: ya hay un usuario con ese email.
            User usuarioExistente = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuarioExistente);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (emailEncontrado != usuarioExistente) throw new InvalidOperationException("Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");

            //Se cuentan el alta y el commit, que no deberian ocurrir. Van despues de la comprobacion.
            int contCreateUser = 0;
            int contSaveChanges = 0;
            _userRepository
                .When(repo => repo.CreateUserAsync(Arg.Any<User>()))
                .Do(llamada => contCreateUser++);

            _unitOfWork
                .When(uow => uow.SaveChangesAsync(Arg.Any<CancellationToken>()))
                .Do(llamada => contSaveChanges++);

            var command = NewSignUpCommand(email: "ruben@test.com", userName: "otro");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: fallo Duplicated y nada llega a la base de datos.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Duplicated, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            Assert.Equal(0, contCreateUser);
            Assert.Equal(0, contSaveChanges);
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
            if (emailEncontrado is not null) throw new InvalidOperationException("Arrange mal montado: el email otro@test.com tiene que estar libre.");

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("ruben");
            if (userNameEncontrado != usuarioExistente) throw new InvalidOperationException("Arrange mal montado: el username 'ruben' tiene que estar cogido.");

            //Se cuentan el alta y el commit, que no deberian ocurrir. Van despues de la comprobacion.
            int contCreateUser = 0;
            int contSaveChanges = 0;
            _userRepository
                .When(repo => repo.CreateUserAsync(Arg.Any<User>()))
                .Do(llamada => contCreateUser++);

            _unitOfWork
                .When(uow => uow.SaveChangesAsync(Arg.Any<CancellationToken>()))
                .Do(llamada => contSaveChanges++);

            var command = NewSignUpCommand(email: "otro@test.com", userName: "ruben");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Duplicated, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            Assert.Equal(0, contCreateUser);
            Assert.Equal(0, contSaveChanges);
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
            if (filasEscritas != 0) throw new InvalidOperationException($"Arrange mal montado: el commit no deberia escribir ninguna fila y escribe {filasEscritas}.");

            var command = NewSignUpCommand(email: "nuevo@test.com", userName: "nuevo");

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
            if (emailEncontrado != usuarioExistente) throw new InvalidOperationException("Arrange mal montado: victima@test.com deberia estar ya registrado.");

            var command = NewSignUpCommand(email: "victima@test.com", userName: "otro");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: comportamiento ACTUAL. El mensaje distingue este caso de cualquier otro fallo.
            Assert.Equal("User already exists", response.Message);
        }

        [Fact]
        public async Task Handle_RechazaElAltaCuandoEmailYUserNameYaEstanRegistrados()
        {
            //Arrange: los dos datos estan cogidos, cada uno por un usuario distinto.
            User usuarioConEmail = NewUser(id: 1, email: "ruben@test.com", userName: "otro1");
            User usuarioConUserName = NewUser(id: 2, email: "otro2@test.com", userName: "ruben");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuarioConEmail);
            _userRepository.GetByUserNameAsync("ruben").Returns(usuarioConUserName);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (emailEncontrado != usuarioConEmail) throw new InvalidOperationException("Arrange mal montado: el email ruben@test.com tiene que estar cogido.");

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("ruben");
            if (userNameEncontrado != usuarioConUserName) throw new InvalidOperationException("Arrange mal montado: el username 'ruben' tiene que estar cogido.");

            //Se cuentan el alta y el commit, que no deberian ocurrir. Van despues de la comprobacion.
            int contCreateUser = 0;
            int contSaveChanges = 0;
            _userRepository
                .When(repo => repo.CreateUserAsync(Arg.Any<User>()))
                .Do(llamada => contCreateUser++);

            _unitOfWork
                .When(uow => uow.SaveChangesAsync(Arg.Any<CancellationToken>()))
                .Do(llamada => contSaveChanges++);

            var command = NewSignUpCommand(email: "ruben@test.com", userName: "ruben");

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: un solo fallo Duplicated, con Data en false y sin tocar la base de datos.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Duplicated, response.ErrorType);
            Assert.Equal(0, contCreateUser);
            Assert.Equal(0, contSaveChanges);
        }

        [Fact]
        public async Task Handle_ConsultaPorElEmailYElUserNameDelComando()
        {
            //Arrange: todo libre y el commit escribe una fila.
            _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
            _userRepository.GetByUserNameAsync(Arg.Any<string>()).Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("cualquiera@test.com");
            if (emailEncontrado is not null) throw new InvalidOperationException("Arrange mal montado: el repositorio no deberia encontrar ningun email.");

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("cualquiera");
            if (userNameEncontrado is not null) throw new InvalidOperationException("Arrange mal montado: el repositorio no deberia encontrar ningun username.");

            //Se registra lo que recibe el repositorio. Va despues de la comprobacion para que sus llamadas no cuenten.
            string? emailConsultado = null;
            int contGetByEmail = 0;
            string? userNameConsultado = null;
            int contGetByUserName = 0;
            _userRepository
                .When(repo => repo.GetByEmailAsync(Arg.Any<string>()))
                .Do(llamada =>
                {
                    emailConsultado = llamada.Arg<string>();
                    contGetByEmail++;
                });

            _userRepository
                .When(repo => repo.GetByUserNameAsync(Arg.Any<string>()))
                .Do(llamada =>
                {
                    userNameConsultado = llamada.Arg<string>();
                    contGetByUserName++;
                });

            var command = NewSignUpCommand(email: "Nuevo@Test.com", userName: "NuevoUser");

            //Act
            await _handler.Handle(command, CancellationToken.None);

            //Assert: se consulta con los valores tal cual vienen en el comando, sin normalizar.
            Assert.Equal(1, contGetByEmail);
            Assert.Equal("Nuevo@Test.com", emailConsultado);
            Assert.Equal(1, contGetByUserName);
            Assert.Equal("NuevoUser", userNameConsultado);
        }

        [Fact]
        public async Task Handle_MapeaTodosLosCamposDelComandoALaEntidad()
        {
            //Arrange: email y username libres, y el commit escribe una fila.
            _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
            _userRepository.GetByUserNameAsync(Arg.Any<string>()).Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("cualquiera@test.com");
            if (emailEncontrado is not null) throw new InvalidOperationException("Arrange mal montado: el repositorio no deberia encontrar ningun email.");

            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1) throw new InvalidOperationException($"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");

            //Se registra el User que recibe el repositorio. Va despues de la comprobacion para que su llamada no cuente.
            User? usuarioRegistrado = null;
            int contCreateUser = 0;
            _userRepository
                .When(repo => repo.CreateUserAsync(Arg.Any<User>()))
                .Do(llamada =>
                {
                    usuarioRegistrado = llamada.Arg<User>();
                    contCreateUser++;
                });

            var command = new SignUpCommand()
            {
                FirstName = "Maria",
                LastName = "Lopez",
                Email = "maria@test.com",
                UserName = "maria",
                Password = "OtraPassword1!"
            };

            //Act
            await _handler.Handle(command, CancellationToken.None);

            //Assert: la contrasena llega en claro al repositorio, que es quien la cifra.
            Assert.Equal(1, contCreateUser);
            Assert.NotNull(usuarioRegistrado);
            Assert.Equal("Maria", usuarioRegistrado.FirstName);
            Assert.Equal("Lopez", usuarioRegistrado.LastName);
            Assert.Equal("maria@test.com", usuarioRegistrado.Email);
            Assert.Equal("maria", usuarioRegistrado.UserName);
            Assert.Equal("OtraPassword1!", usuarioRegistrado.PasswordHash);
        }

        [Fact]
        public async Task Handle_PasaElCancellationTokenAlCommit()
        {
            //Arrange: todo libre y el commit escribe una fila.
            _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
            _userRepository.GetByUserNameAsync(Arg.Any<string>()).Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Comprobacion del Arrange.
            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1) throw new InvalidOperationException($"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");

            //Se registra el token que recibe el commit. Va despues de la comprobacion para que su llamada no cuente.
            CancellationToken tokenRecibido = CancellationToken.None;
            int contSaveChanges = 0;
            _unitOfWork
                .When(uow => uow.SaveChangesAsync(Arg.Any<CancellationToken>()))
                .Do(llamada =>
                {
                    tokenRecibido = llamada.Arg<CancellationToken>();
                    contSaveChanges++;
                });

            using var origenDelToken = new CancellationTokenSource();
            CancellationToken token = origenDelToken.Token;
            var command = NewSignUpCommand();

            //Act
            await _handler.Handle(command, token);

            //Assert: el commit recibe el mismo token que el handler, no CancellationToken.None.
            Assert.Equal(1, contSaveChanges);
            Assert.Equal(token, tokenRecibido);
        }

        [Fact]
        public async Task Handle_RegistraElAltaAunqueElCommitNoEscribaFilas()
        {
            //Arrange: todo libre, pero EF no escribe ninguna fila.
            _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
            _userRepository.GetByUserNameAsync(Arg.Any<string>()).Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            //Comprobacion del Arrange.
            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 0) throw new InvalidOperationException($"Arrange mal montado: el commit no deberia escribir ninguna fila y escribe {filasEscritas}.");

            //Se cuentan el alta y el commit. Van despues de la comprobacion para que su llamada no cuente.
            int contCreateUser = 0;
            int contSaveChanges = 0;
            _userRepository
                .When(repo => repo.CreateUserAsync(Arg.Any<User>()))
                .Do(llamada => contCreateUser++);

            _unitOfWork
                .When(uow => uow.SaveChangesAsync(Arg.Any<CancellationToken>()))
                .Do(llamada => contSaveChanges++);

            var command = NewSignUpCommand();

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert: el alta se registra una vez, se intenta confirmar una vez y el resultado es fallo con Data en false.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(1, contCreateUser);
            Assert.Equal(1, contSaveChanges);
        }

        //Cada fila es un numero de filas escritas distinto de cero: todas cuentan como exito.
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        public async Task Handle_DevuelveExitoConCualquierNumeroDeFilasMayorQueCero(int filas)
        {
            //Arrange: todo libre y el commit escribe `filas` filas.
            _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
            _userRepository.GetByUserNameAsync(Arg.Any<string>()).Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(filas);

            //Comprobacion del Arrange.
            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != filas) throw new InvalidOperationException($"Arrange mal montado: el commit deberia escribir {filas} filas y escribe {filasEscritas}.");

            var command = NewSignUpCommand();

            //Act
            var response = await _handler.Handle(command, CancellationToken.None);

            //Assert
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
        }
    }
}
