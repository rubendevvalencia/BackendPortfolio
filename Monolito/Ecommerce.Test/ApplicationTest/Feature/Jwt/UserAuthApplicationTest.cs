using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Feature.Users;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.MainService.Jwt
{
    //Tests de UserAuthApplication con dobles; cada test comprueba sus dobles antes del Act.
    public class UserAuthApplicationTest : ApplicationTestBase
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IJwtApplication _jwt = Substitute.For<IJwtApplication>();
        private readonly ILogger<UserAuthApplication> _logger = Substitute.For<ILogger<UserAuthApplication>>();
        private readonly UserAuthApplication _auth;
        private readonly IDataProtector _protector = Substitute.For<IDataProtector>();

        //xUnit crea una instancia por test: los dobles empiezan limpios.
        public UserAuthApplicationTest()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._user.Returns(_userRepository);

            //Comprobacion: si no devolviera nuestro doble, ningun test probaria nada.
            var repositorioDelUnitOfWork = _unitOfWork._user;
            if (repositorioDelUnitOfWork != _userRepository)
            {
                throw new InvalidOperationException("Arrange mal montado: _unitOfWork._user no devuelve el doble de IUserRepository.");
            }

            //El protector falso devuelve los mismos bytes: asi un test puede deshacer el Protect con Unprotect.
            //CreateProtector devuelve el mismo doble, para que el protector derivado del constructor tambien lo sea.
            _protector.CreateProtector(Arg.Any<string>()).Returns(_protector);
            _protector.Protect(Arg.Any<byte[]>()).Returns(llamada => llamada.Arg<byte[]>());
            _protector.Unprotect(Arg.Any<byte[]>()).Returns(llamada => llamada.Arg<byte[]>());

            //Comprobacion: el ciclo Protect/Unprotect tiene que devolver el texto original.
            var textoProtegido = _protector.Protect("ida-y-vuelta");
            var textoRecuperado = _protector.Unprotect(textoProtegido);
            if (textoRecuperado != "ida-y-vuelta")
            {
                throw new InvalidOperationException("Arrange mal montado: el protector falso no deshace su propio Protect.");
            }

            _auth = new UserAuthApplication(_unitOfWork, Mapper, SignUpValidator, SignInValidator, _jwt, _logger, _protector);
        }

        [Fact]
        public async Task SignUpAsync_DevuelveExitoCuandoElCommitEscribe()
        {
            //Arrange: el email y el nombre de usuario estan libres, y el commit escribe una fila.
            _userRepository.GetByEmailAsync("nuevo@test.com").Returns((User?)null);
            _userRepository.GetByUserNameAsync("nuevo").Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Se captura el User que recibe el repositorio para mirarlo despues del Act.
            User? usuarioRegistrado = null;
            string? passwordRecibida = null;
            await _userRepository.CreateUserAsync(
                Arg.Do<User>(u => usuarioRegistrado = u),
                Arg.Do<string>(p => passwordRecibida = p));

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("nuevo@test.com");
            if (emailEncontrado != null) throw new InvalidOperationException("Arrange mal montado: el email nuevo@test.com tiene que estar libre.");
            
            var userNameEncontrado = await _userRepository.GetByUserNameAsync("nuevo");
            if (userNameEncontrado != null) throw new InvalidOperationException("Arrange mal montado: el nombre de usuario 'nuevo' tiene que estar libre.");
            

            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1) throw new InvalidOperationException($"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");
            

            SignUpDto signUpDto = NewSignUpDto("nuevo@test.com", "nuevo");

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: ademas del Response se comprueba el mapeo real de DTO a entidad.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(usuarioRegistrado);
            Assert.Equal("nuevo@test.com", usuarioRegistrado.Email);
            Assert.Equal("nuevo", usuarioRegistrado.UserName);
            Assert.Equal(signUpDto.Password, passwordRecibida);
        }

        //El repositorio registra el alta y el caso de uso confirma una sola vez (pendiente #1, corregido).
        [Fact]
        public async Task SignUpAsync_RegistraElAltaYConfirmaUnaSolaVez()
        {
            //Arrange
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Comprobacion del Arrange.
            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1)
            {
                throw new InvalidOperationException( $"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");
            }

            //Se limpian las llamadas porque la consulta de arriba contaria para el Received(1) de abajo.
            _unitOfWork.ClearReceivedCalls();

            SignUpDto signUpDto = NewSignUpDto();

            //Act
            await _auth.SignUpAsync(signUpDto);

            //Assert: el alta se registra una vez y el commit lo pide el caso de uso, tambien una vez.
            await _userRepository.Received(1).CreateUserAsync(Arg.Any<User>(), Arg.Any<string>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SignUpAsync_DevuelveFalloCuandoElCommitNoEscribeFilas()
        {
            //Arrange: el alta se registra, pero EF no escribe ninguna fila.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            //Comprobacion del Arrange: es justo el valor que define este escenario.
            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 0)
            {
                throw new InvalidOperationException( $"Arrange mal montado: el commit no deberia escribir ninguna fila y escribe {filasEscritas}.");
            }

            SignUpDto signUpDto = NewSignUpDto();

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: en un alta, 0 filas si es un fallo.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Failed to create user", response.Message);
        }

        //Cada fila rompe una regla de SignUpDtoValidator: la validacion corta antes de tocar la base de datos.
        [Theory]
        [InlineData("", "ruben", ValidPassword, nameof(SignUpDto.Email))]
        [InlineData("no-es-un-email", "ruben", ValidPassword, nameof(SignUpDto.Email))]
        [InlineData("ruben@test.com", "", ValidPassword, nameof(SignUpDto.UserName))]
        [InlineData("ruben@test.com", "ruben", "corta", nameof(SignUpDto.Password))]
        public async Task SignUpAsync_NoTocaLaPersistenciaCuandoElDtoNoEsValido(string email, string userName, string password, string propiedadConError)
        {
            //Arrange
            SignUpDto signUpDto = NewSignUpDto();
            signUpDto.Email = email;
            signUpDto.UserName = userName;
            signUpDto.Password = password;

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: ni se consulta si el usuario existe ni se registra ni se confirma.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.True(response.Errors.ContainsKey(propiedadConError));
            await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>());
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>(), Arg.Any<string>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SignUpAsync_RechazaElAltaCuandoElEmailYaEstaRegistrado()
        {
            //Arrange: ya hay un usuario con ese email.
            User usuarioExistente = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuarioExistente);

            //Comprobacion del Arrange: tiene que devolver ESA instancia, si no el test probaria otra cosa.
            var emailEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (emailEncontrado != usuarioExistente)
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            SignUpDto signUpDto = NewSignUpDto(email: "ruben@test.com");

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: un duplicado es un 400, no un 500. Y nada llega a la base de datos.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>(), Arg.Any<string>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SignUpAsync_RechazaElAltaCuandoElUserNameYaEstaRegistrado()
        {
            //Arrange: el email esta libre y el nombre de usuario no. Son dos consultas distintas.
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
                throw new InvalidOperationException("Arrange mal montado: el nombre de usuario 'ruben' tiene que estar cogido.");
            }

            SignUpDto signUpDto = NewSignUpDto(email: "otro@test.com", userName: "ruben");

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>(), Arg.Any<string>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SignUpAsync_TraduceLaExcepcionAFalloInesperado()
        {
            //Arrange: el commit revienta, como haria un fallo real de base de datos.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                       .ThrowsAsync(new InvalidOperationException("problem in db"));

            //Comprobacion del Arrange: el doble tiene que reventar.
            var mensajeDelDoble = string.Empty;
            try
            {
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
            catch (InvalidOperationException ex)
            {
                mensajeDelDoble = ex.Message;
            }

            if (mensajeDelDoble != "problem in db")
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el commit deberia reventar con 'problem in db'.");
            }

            SignUpDto signUpDto = NewSignUpDto();

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: el caso de uso no propaga la excepcion, la traduce a Response.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);

            //No se comprueba el log: LogError es un metodo de extension y NSubstitute no puede interceptarlo.
        }

        [Fact]
        public async Task SingInAsync_DevuelveElTokenCuandoLasCredencialesSonValidas()
        {
            //Arrange: el usuario existe y la contrasena es correcta.
            User usuario = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuario);
            _userRepository.CheckPass(usuario, ValidPassword).Returns(true);
            _jwt.GenerateToken(usuario).Returns(("token-firmado", 3600));

            //Comprobacion del Arrange: las tres piezas de la cadena.
            var usuarioEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (usuarioEncontrado != usuario)
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            var contrasenaValida = _userRepository.CheckPass(usuario, ValidPassword);
            if (!contrasenaValida)
            {
                throw new InvalidOperationException("Arrange mal montado: la contrasena del test tiene que darse por valida.");
            }

            var tokenGenerado = _jwt.GenerateToken(usuario);
            if (tokenGenerado.Item1 != "token-firmado" || tokenGenerado.Item2 != 3600)
            {
                throw new InvalidOperationException( "Arrange mal montado: JwtApplication no devuelve el token de prueba.");
            }

            //Se limpian las llamadas por el Received(1).GenerateToken de abajo, igual que en el alta.
            _jwt.ClearReceivedCalls();

            SignInDto signInDto = NewSignInDto(email: "ruben@test.com");

            //Act
            var response = await _auth.SingInAsync(signInDto);

            //Assert: el TokenDto se arma con lo que devuelve JwtApplication y el tipo es fijo.
            Assert.True(response.IsSuccess);
            Assert.Equal("token-firmado", response.Data.AccessToken);
            Assert.Equal(3600, response.Data.ExpiresIn);
            Assert.Equal("Bearer", response.Data.TokenType);

            //Se firma para el usuario que devolvio el repositorio, no para uno reconstruido del DTO.
            _jwt.Received(1).GenerateToken(usuario);
        }

        //Cada fila rompe una regla de SignInValidator: se valida antes de ir a la base de datos.
        [Theory]
        [InlineData("", ValidPassword, nameof(SignInDto.Email))]
        [InlineData("no-es-un-email", ValidPassword, nameof(SignInDto.Email))]
        [InlineData("ruben@test.com", "corta", nameof(SignInDto.Password))]
        public async Task SingInAsync_NoBuscaAlUsuarioCuandoElDtoNoEsValido(string email, string password, string propiedadConError)
        {
            //Arrange
            SignInDto signInDto = NewSignInDto(email, password);

            //Act
            var response = await _auth.SingInAsync(signInDto);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.True(response.Errors.ContainsKey(propiedadConError));
            await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>());
            _jwt.DidNotReceive().GenerateToken(Arg.Any<User>());
        }

        [Fact]
        public async Task SingInAsync_NoFirmaTokenCuandoLaContrasenaEsIncorrecta()
        {
            //Arrange: el usuario existe, pero la comprobacion del hash falla.
            User usuario = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuario);
            _userRepository.CheckPass(usuario, "OtraPassword1!").Returns(false);

            //Comprobacion del Arrange: el usuario tiene que existir para probar la contrasena.
            var usuarioEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (usuarioEncontrado != usuario)
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            var contrasenaValida = _userRepository.CheckPass(usuario, "OtraPassword1!");
            if (contrasenaValida)
            {
                throw new InvalidOperationException( "Arrange mal montado: la contrasena del test tiene que darse por invalida.");
            }

            SignInDto signInDto = NewSignInDto(email: "ruben@test.com", password: "OtraPassword1!");

            //Act
            var response = await _auth.SingInAsync(signInDto);

            //Assert: lo que importa es que de aqui no sale ningun token.
            Assert.False(response.IsSuccess);
            Assert.Null(response.Data);
            _jwt.DidNotReceive().GenerateToken(Arg.Any<User>());
        }

        [Fact]
        public async Task SingInAsync_NoCompruebaLaContrasenaCuandoElUsuarioNoExiste()
        {
            //Arrange: no hay ningun usuario con ese email.
            _userRepository.GetByEmailAsync("fantasma@test.com").Returns((User?)null);

            //Comprobacion del Arrange.
            var usuarioEncontrado = await _userRepository.GetByEmailAsync("fantasma@test.com");
            if (usuarioEncontrado is not null)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: fantasma@test.com no deberia estar registrado.");
            }

            SignInDto signInDto = NewSignInDto(email: "fantasma@test.com");

            //Act
            var response = await _auth.SingInAsync(signInDto);

            //Assert
            Assert.False(response.IsSuccess);
            _userRepository.DidNotReceive().CheckPass(Arg.Any<User>(), Arg.Any<string>());
            _jwt.DidNotReceive().GenerateToken(Arg.Any<User>());
        }

        //Pendiente #3 corregido: SignIn ya no distingue email inexistente de contrasena incorrecta.
        [Fact]
        public async Task SingInAsync_YaNoDistingueEmailInexistenteDeContrasenaIncorrecta()
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
                throw new InvalidOperationException( "Arrange mal montado: la contrasena del test tiene que darse por invalida.");
            }

            //Act
            var emailInexistente = await _auth.SingInAsync(NewSignInDto(email: "fantasma@test.com"));
            var contrasenaIncorrecta = await _auth.SingInAsync(NewSignInDto(email: "registrado@test.com"));

            //Assert: los dos fallan igual, sin ninguna pista de cual de las dos cosas fallo.
            Assert.False(emailInexistente.IsSuccess);
            Assert.False(contrasenaIncorrecta.IsSuccess);
            Assert.Equal(ErrorType.Unauthorized, emailInexistente.ErrorType);
            Assert.Equal(ErrorType.Unauthorized, contrasenaIncorrecta.ErrorType);
            Assert.Equal(emailInexistente.Message, contrasenaIncorrecta.Message);
        }

        //Defecto de seguridad pendiente, sin corregir todavia: "User already exists" confirma que un email tiene cuenta.
        [Fact]
        public async Task SignUpAsync_HoyConfirmaQueUnEmailYaEstaRegistrado_DefectoDeSeguridad()
        {
            //Arrange
            User usuarioExistente = NewUser(email: "victima@test.com");
            _userRepository.GetByEmailAsync("victima@test.com").Returns(usuarioExistente);

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("victima@test.com");
            if (emailEncontrado != usuarioExistente)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: victima@test.com deberia estar ya registrado.");
            }

            SignUpDto signUpDto = NewSignUpDto(email: "victima@test.com", userName: "otro");

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: comportamiento ACTUAL. El mensaje distingue este caso de cualquier otro fallo.
            Assert.Equal("User already exists", response.Message);
        }

        //Pendiente #6 corregido: el detalle de la excepcion no sale por la API, solo va al log.
        [Fact]
        public async Task SignUpAsync_NoDevuelveElDetalleDeLaExcepcion()
        {
            //Arrange: una excepcion con detalle de infraestructura, del estilo de las de SQL Server.
            var detalleInterno = "Login failed for user 'sa'. Server=prod-sql-01;Database=EcommerceDb";
            _userRepository.GetByEmailAsync(Arg.Any<string>())
                           .ThrowsAsync(new InvalidOperationException(detalleInterno));

            //Comprobacion del Arrange: el doble revienta con el mensaje que luego NO debe salir.
            var mensajeDelDoble = string.Empty;
            try
            {
                await _userRepository.GetByEmailAsync("cualquiera@test.com");
            }
            catch (InvalidOperationException ex)
            {
                mensajeDelDoble = ex.Message;
            }

            if (mensajeDelDoble != detalleInterno)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia reventar con el detalle interno.");
            }

            SignUpDto signUpDto = NewSignUpDto();

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: fallo inesperado, con un mensaje que no filtra nada de la excepcion.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);

            //Un mensaje vacio taparia la fuga, pero dejaria al cliente sin nada que mostrar.
            Assert.NotEmpty(response.Message);

            //Se comprueba cada fragmento por separado para saber cual se escapo.
            Assert.DoesNotContain(detalleInterno, response.Message);
            Assert.DoesNotContain("prod-sql-01", response.Message);
            Assert.DoesNotContain("EcommerceDb", response.Message);
        }

        //El mensaje tiene que ser el mismo sea cual sea la excepcion, para no dar pistas.
        [Fact]
        public async Task SignUpAsync_DevuelveElMismoMensajeSeaCualSeaLaExcepcion()
        {
            //Arrange: primera excepcion, un fallo de credenciales de base de datos.
            _userRepository.GetByEmailAsync(Arg.Any<string>())
                           .ThrowsAsync(new InvalidOperationException("Login failed for user 'sa'."));

            //Comprobacion del Arrange.
            var mensajeDelPrimerDoble = string.Empty;
            try
            {
                await _userRepository.GetByEmailAsync("cualquiera@test.com");
            }
            catch (InvalidOperationException ex)
            {
                mensajeDelPrimerDoble = ex.Message;
            }

            if (mensajeDelPrimerDoble != "Login failed for user 'sa'.")
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia reventar con el fallo de credenciales.");
            }

            SignUpDto signUpDto = NewSignUpDto();

            //Act: primera llamada.
            var primeraResponse = await _auth.SignUpAsync(signUpDto);

            //Arrange: el mismo doble con otra excepcion y otro mensaje.
            _userRepository.GetByEmailAsync(Arg.Any<string>())
                           .ThrowsAsync(new TimeoutException("Timeout expired. Server=prod-sql-01"));

            //Comprobacion del Arrange: ahora tiene que reventar con la segunda, no con la primera.
            var mensajeDelSegundoDoble = string.Empty;
            try
            {
                await _userRepository.GetByEmailAsync("cualquiera@test.com");
            }
            catch (TimeoutException ex)
            {
                mensajeDelSegundoDoble = ex.Message;
            }

            if (mensajeDelSegundoDoble != "Timeout expired. Server=prod-sql-01")
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia reventar ahora con el timeout.");
            }

            //Act: segunda llamada, mismo caso de uso.
            var segundaResponse = await _auth.SignUpAsync(signUpDto);

            //Assert: dos excepciones distintas, una sola respuesta hacia fuera.
            Assert.Equal(primeraResponse.Message, segundaResponse.Message);
            Assert.Equal(ErrorType.Unexpected, primeraResponse.ErrorType);
            Assert.Equal(ErrorType.Unexpected, segundaResponse.ErrorType);
        }

        [Fact]
        public async Task SingInAsync_NoDevuelveElDetalleDeLaExcepcion()
        {
            //Arrange: la misma excepcion con detalle de infraestructura que en el test de SignUp.
            var detalleInterno = "Login failed for user 'sa'. Server=prod-sql-01;Database=EcommerceDb";
            _userRepository.GetByEmailAsync(Arg.Any<string>())
                           .ThrowsAsync(new InvalidOperationException(detalleInterno));

            //Comprobacion del Arrange.
            var mensajeDelDoble = string.Empty;
            try
            {
                await _userRepository.GetByEmailAsync("cualquiera@test.com");
            }
            catch (InvalidOperationException ex)
            {
                mensajeDelDoble = ex.Message;
            }

            if (mensajeDelDoble != detalleInterno)
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia reventar con el detalle interno.");
            }

            SignInDto signInDto = NewSignInDto();

            //Act
            var response = await _auth.SingInAsync(signInDto);

            //Assert: fallo inesperado y sin el detalle de la excepcion.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Null(response.Data);
            Assert.NotEmpty(response.Message);
            Assert.DoesNotContain(detalleInterno, response.Message);
            Assert.DoesNotContain("prod-sql-01", response.Message);
            Assert.DoesNotContain("EcommerceDb", response.Message);
        }

        //El detalle tiene que seguir en el log. Se espia ILogger.Log porque LogError es una extension.
        [Fact]
        public async Task SingInAsync_EscribeElDetalleDeLaExcepcionEnElLog()
        {
            //Arrange
            var detalleInterno = "Login failed for user 'sa'. Server=prod-sql-01;Database=EcommerceDb";
            var excepcion = new InvalidOperationException(detalleInterno);
            _userRepository.GetByEmailAsync(Arg.Any<string>()).ThrowsAsync(excepcion);

            //Comprobacion del Arrange.
            Exception? excepcionDelDoble = null;
            try
            {
                await _userRepository.GetByEmailAsync("cualquiera@test.com");
            }
            catch (InvalidOperationException ex)
            {
                excepcionDelDoble = ex;
            }

            if (excepcionDelDoble != excepcion)
            {
                throw new InvalidOperationException("Arrange mal montado: el repositorio deberia reventar con ESA misma excepcion.");
            }

            //Se limpian las llamadas para que el Received(1) solo cuente lo que escribe el caso de uso.
            _logger.ClearReceivedCalls();

            SignInDto signInDto = NewSignInDto();

            //Act
            await _auth.SingInAsync(signInDto);

            //Assert: una entrada de Error con la excepcion original dentro, la que lleva el detalle.
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<Arg.AnyType>(),
                excepcion,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>());
        }
    }
}
