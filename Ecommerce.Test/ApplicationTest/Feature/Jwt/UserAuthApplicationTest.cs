using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Feature.Users;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Transversal.Common.Enums;
using Ecommerce.Transversal.Loggin.Interface;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.MainService.Jwt
{
    //Tests de UserAuthApplication. Se sustituyen los limites (UnitOfWork, repositorio, firma del
    //token y logger); el mapper y los validadores se usan REALES porque forman parte del caso de uso.
    //La firma del JWT se sustituye a proposito: comprobar que el token esta bien firmado es trabajo
    //de JwtApplication; aqui solo importa QUE se pide un token y para QUE usuario.
    //
    //Cada test se lee entero de arriba abajo: prepara sus propios dobles, COMPRUEBA que esos dobles
    //devuelven lo que se espera, llama al caso de uso y comprueba el resultado.
    //
    //Por que esa comprobacion intermedia: un doble mal montado (un email que no coincide, un metodo
    //que no es el que cree que ha configurado) devuelve el valor por defecto en silencio, y entonces
    //el test se ejecuta sobre un escenario distinto del que dice su nombre. Puede quedarse verde por
    //el motivo equivocado. Consultando el doble y reventando ahi mismo, ese fallo sale antes del Act
    //y con un mensaje que dice que esta mal montado, en vez de un Assert final incomprensible.
    public class UserAuthApplicationTest : ApplicationTestBase
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IJwtApplication _jwt = Substitute.For<IJwtApplication>();
        private readonly ILogger<UserAuthApplication> _logger = Substitute.For<ILogger<UserAuthApplication>>();
        private readonly UserAuthApplication _auth;

        //xUnit crea una instancia de la clase por cada [Fact], asi que los dobles
        //nacen limpios en cada test y no hay estado compartido entre ellos.
        public UserAuthApplicationTest()
        {
            //El caso de uso llega al repositorio a traves del UnitOfWork:
            //la propiedad tiene que devolver nuestro doble.
            _unitOfWork._user.Returns(_userRepository);

            //Comprobacion: si esto no devolviera nuestro doble, TODOS los tests de la clase estarian
            //hablando con un repositorio distinto del que configuran, y ninguno probaria nada.
            var repositorioDelUnitOfWork = _unitOfWork._user;
            if (repositorioDelUnitOfWork != _userRepository)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: _unitOfWork._user no devuelve el doble de IUserRepository.");
            }

            _auth = new UserAuthApplication(_unitOfWork, Mapper, SignUpValidator, SignInValidator, _jwt, _logger);
        }

        // ---------- SignUpAsync ----------

        [Fact]
        public async Task SignUpAsync_DevuelveExitoCuandoElCommitEscribe()
        {
            //Arrange: el email y el nombre de usuario estan libres, y el commit escribe una fila.
            _userRepository.GetByEmailAsync("nuevo@test.com").Returns((User?)null);
            _userRepository.GetByUserNameAsync("nuevo").Returns((User?)null);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Se captura el User que recibe el repositorio para mirarlo despues del Act.
            User? usuarioRegistrado = null;
            await _userRepository.CreateUserAsync(Arg.Do<User>(u => usuarioRegistrado = u));

            //Comprobacion del Arrange.
            var emailEncontrado = await _userRepository.GetByEmailAsync("nuevo@test.com");
            if (emailEncontrado is not null)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el email nuevo@test.com tiene que estar libre.");
            }

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("nuevo");
            if (userNameEncontrado is not null)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el nombre de usuario 'nuevo' tiene que estar libre.");
            }

            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1)
            {
                throw new InvalidOperationException(
                    $"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");
            }

            SignUpDto signUpDto = NewSignUpDto(email: "nuevo@test.com", userName: "nuevo");

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: ademas del Response se comprueba el mapeo real de DTO a entidad.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(usuarioRegistrado);
            Assert.Equal("nuevo@test.com", usuarioRegistrado.Email);
            Assert.Equal("nuevo", usuarioRegistrado.UserName);
        }

        //El limite transaccional vive en el caso de uso, no en el repositorio: es el pendiente #1, ya
        //corregido. El repositorio registra el alta y UserAuthApplication confirma, una sola vez.
        //Que UserRepository no vuelva a confirmar por su cuenta lo vigila el test de Infrastructure
        //CreateUserAsync_RegistraElAltaPeroNoConfirma; aqui el repositorio es un doble.
        [Fact]
        public async Task SignUpAsync_RegistraElAltaYConfirmaUnaSolaVez()
        {
            //Arrange
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Comprobacion del Arrange.
            var filasEscritas = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (filasEscritas != 1)
            {
                throw new InvalidOperationException(
                    $"Arrange mal montado: el commit deberia escribir 1 fila y escribe {filasEscritas}.");
            }

            //Necesario SOLO aqui y en el test del token: consultar el doble para comprobarlo cuenta
            //como llamada recibida, y mas abajo hay un Received(1).SaveChangesAsync que contaria dos
            //(la del Arrange y la del caso de uso). En el resto de tests no hace falta, porque o no
            //cuentan llamadas o cuentan las de un metodo distinto del que se consulta arriba.
            _unitOfWork.ClearReceivedCalls();

            SignUpDto signUpDto = NewSignUpDto();

            //Act
            await _auth.SignUpAsync(signUpDto);

            //Assert: el alta se registra una vez y el commit lo pide el caso de uso, tambien una vez.
            await _userRepository.Received(1).CreateUserAsync(Arg.Any<User>());
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
                throw new InvalidOperationException(
                    $"Arrange mal montado: el commit no deberia escribir ninguna fila y escribe {filasEscritas}.");
            }

            SignUpDto signUpDto = NewSignUpDto();

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: aqui un 0 SI es un fallo, al contrario que en el PUT idempotente de Customer:
            //un alta que no escribe nada no ha dado de alta a nadie.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Failed to create user", response.Message);
        }

        //Cada fila rompe una regla distinta de SignUpDtoValidator, sobre la propiedad que se nombra
        //en el ultimo parametro. Todas comprueban lo mismo: la validacion corta el caso de uso
        //antes de tocar la base de datos.
        //Aqui no hay nada que comprobar en el Arrange: no se configura ningun doble a proposito,
        //porque el caso de uso no debe llegar a consultarlos.
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
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SignUpAsync_RechazaElAltaCuandoElEmailYaEstaRegistrado()
        {
            //Arrange: ya hay un usuario con ese email.
            User usuarioExistente = NewUser(email: "ruben@test.com");
            _userRepository.GetByEmailAsync("ruben@test.com").Returns(usuarioExistente);

            //Comprobacion del Arrange: tiene que devolver ESA instancia. Si el email del stub y el
            //del DTO no coincidieran, devolveria null y el test probaria un alta correcta.
            var emailEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (emailEncontrado != usuarioExistente)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            SignUpDto signUpDto = NewSignUpDto(email: "ruben@test.com");

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: un duplicado es un 400, no un 500. Y nada llega a la base de datos.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>());
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
                throw new InvalidOperationException(
                    "Arrange mal montado: el email otro@test.com tiene que estar libre.");
            }

            var userNameEncontrado = await _userRepository.GetByUserNameAsync("ruben");
            if (userNameEncontrado != usuarioExistente)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el nombre de usuario 'ruben' tiene que estar cogido.");
            }

            SignUpDto signUpDto = NewSignUpDto(email: "otro@test.com", userName: "ruben");

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Equal("User already exists", response.Message);
            await _userRepository.DidNotReceive().CreateUserAsync(Arg.Any<User>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SignUpAsync_TraduceLaExcepcionAFalloInesperado()
        {
            //Arrange: el commit revienta, como haria un fallo real de base de datos.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                       .ThrowsAsync(new InvalidOperationException("fallo de base de datos"));

            //Comprobacion del Arrange: aqui lo que se espera del doble es justamente que reviente.
            //Se captura el mensaje en una variable y se valida aparte, igual que en el resto.
            var mensajeDelDoble = string.Empty;
            try
            {
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
            catch (InvalidOperationException ex)
            {
                mensajeDelDoble = ex.Message;
            }

            if (mensajeDelDoble != "fallo de base de datos")
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el commit deberia reventar con 'fallo de base de datos'.");
            }

            SignUpDto signUpDto = NewSignUpDto();

            //Act
            var response = await _auth.SignUpAsync(signUpDto);

            //Assert: el caso de uso no propaga la excepcion, la traduce a Response.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);

            //Aqui NO se comprueba que se haya escrito el log, y es a proposito: LogError no es un metodo
            //de ILogger<T>, es un metodo de extension estatico. NSubstitute solo intercepta los metodos
            //de la interfaz, asi que la llamada nunca llega al doble, los Arg.Any se quedan sin consumir
            //y salta RedundantArgumentMatcherException. La unica forma de comprobarlo seria espiar
            //ILogger.Log con sus cinco argumentos, que ata el test a como se escribe el log en vez de a
            //lo que el test tiene que fijar: que la excepcion se traduce a Unexpected, ya cubierto arriba.
        }

        // ---------- SingInAsync ----------

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
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            var contrasenaValida = _userRepository.CheckPass(usuario, ValidPassword);
            if (!contrasenaValida)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: la contrasena del test tiene que darse por valida.");
            }

            var tokenGenerado = _jwt.GenerateToken(usuario);
            if (tokenGenerado.Item1 != "token-firmado" || tokenGenerado.Item2 != 3600)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: JwtApplication no devuelve el token de prueba.");
            }

            //Igual que en SignUpAsync_RegistraElAltaYConfirmaUnaSolaVez: abajo hay un
            //Received(1).GenerateToken y la consulta de aqui arriba ya cuenta como una llamada.
            //Al repositorio no hace falta borrarle nada: no se cuentan sus llamadas en este test.
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

        //Mismo planteamiento que en el alta, con las reglas de SignInValidator: se valida ANTES
        //de ir a la base de datos y antes de firmar nada. Tampoco hay dobles que comprobar.
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

            //Comprobacion del Arrange: si el usuario no se encontrara, el caso de uso saldria por la
            //rama de "no existe" y el test se quedaria verde sin haber probado la contrasena.
            var usuarioEncontrado = await _userRepository.GetByEmailAsync("ruben@test.com");
            if (usuarioEncontrado != usuario)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia encontrar al usuario de ruben@test.com.");
            }

            var contrasenaValida = _userRepository.CheckPass(usuario, "OtraPassword1!");
            if (contrasenaValida)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: la contrasena del test tiene que darse por invalida.");
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

        // ---------- Caracterizacion de defectos de seguridad ----------
        // Los cuatro tests siguientes NO describen el comportamiento deseado: fijan el ACTUAL para que
        // la deuda sea visible y para que, al arreglarla, el rojo diga exactamente que ha cambiado. Ese
        // rojo sera el arreglo, no una regresion. Se agrupan aparte porque son los unicos que hay que
        // reescribir despues; el resto del fichero seguira valiendo igual.

        //DEFECTO: enumeracion de usuarios en el login.
        //Un email no registrado responde NotFound (404) y uno registrado con la contrasena mal responde
        //Validation (400). Con esa diferencia se puede recorrer una lista de correos y averiguar cuales
        //tienen cuenta sin acertar ni una contrasena. Las dos ramas deberian dar la MISMA respuesta:
        //un unico "credenciales invalidas", con el mismo ErrorType y el mismo Message. Cuando se
        //arregle, este test pasa a comprobar que las dos respuestas son iguales en vez de distintas.
        [Fact]
        public async Task SingInAsync_HoyDistingueEmailInexistenteDeContrasenaIncorrecta_DefectoDeSeguridad()
        {
            //Arrange: dos intentos fallidos que solo se diferencian en si el email existe o no.
            User registrado = NewUser(email: "registrado@test.com");
            _userRepository.GetByEmailAsync("fantasma@test.com").Returns((User?)null);
            _userRepository.GetByEmailAsync("registrado@test.com").Returns(registrado);
            _userRepository.CheckPass(registrado, ValidPassword).Returns(false);

            //Comprobacion del Arrange: este test compara dos escenarios, asi que si uno de los dos
            //estuviera mal montado la comparacion final no significaria nada.
            var emailInexistenteEncontrado = await _userRepository.GetByEmailAsync("fantasma@test.com");
            if (emailInexistenteEncontrado is not null)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: fantasma@test.com no deberia estar registrado.");
            }

            var emailRegistradoEncontrado = await _userRepository.GetByEmailAsync("registrado@test.com");
            if (emailRegistradoEncontrado != registrado)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: registrado@test.com si deberia estar registrado.");
            }

            var contrasenaValida = _userRepository.CheckPass(registrado, ValidPassword);
            if (contrasenaValida)
            {
                throw new InvalidOperationException(
                    "Arrange mal montado: la contrasena del test tiene que darse por invalida.");
            }

            //Act
            var emailInexistente = await _auth.SingInAsync(NewSignInDto(email: "fantasma@test.com"));
            var contrasenaIncorrecta = await _auth.SingInAsync(NewSignInDto(email: "registrado@test.com"));

            //Assert: comportamiento ACTUAL. Los dos fallan, pero de forma distinguible desde fuera.
            Assert.False(emailInexistente.IsSuccess);
            Assert.False(contrasenaIncorrecta.IsSuccess);
            Assert.Equal(ErrorType.NotFound, emailInexistente.ErrorType);
            Assert.Equal(ErrorType.Validation, contrasenaIncorrecta.ErrorType);
            Assert.NotEqual(emailInexistente.Message, contrasenaIncorrecta.Message);
        }

        //DEFECTO: el alta tambien filtra quien esta registrado.
        //"User already exists" ante un email ajeno confirma que ese email tiene cuenta. La salida
        //habitual es responder siempre lo mismo y resolver el conflicto por correo.
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

        //Pendiente #6 del README, ya corregido: el detalle de la excepcion no cruza el borde de la API.
        //El Message de una excepcion de base de datos puede llevar el nombre del servidor, el del usuario
        //o el de la tabla. Dentro el log, fuera un texto generico. Pasa en los dos casos de uso, asi que
        //hay un test para cada uno.
        //
        //No se fija el texto exacto del mensaje generico a proposito: lo que tiene que cumplirse es que
        //NO lleve el detalle interno. Atarlo a una cadena concreta convierte un cambio de redaccion, o
        //del idioma del mensaje, en un test rojo que no avisa de ningun defecto.
        [Fact]
        public async Task SignUpAsync_NoDevuelveElDetalleDeLaExcepcion()
        {
            //Arrange: una excepcion con detalle de infraestructura, del estilo de las de SQL Server.
            var detalleInterno = "Login failed for user 'sa'. Server=prod-sql-01;Database=EcommerceDb";
            _userRepository.GetByEmailAsync(Arg.Any<string>())
                           .ThrowsAsync(new InvalidOperationException(detalleInterno));

            //Comprobacion del Arrange: lo que se espera del doble es que reviente con ESE mensaje,
            //que es justo el que despues NO tiene que aparecer en la respuesta.
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

            //Los tres fragmentos son los que de verdad duelen: la frase entera, el nombre del servidor
            //y el de la base de datos. Se comprueban por separado para que el fallo diga cual se escapo.
            Assert.DoesNotContain(detalleInterno, response.Message);
            Assert.DoesNotContain("prod-sql-01", response.Message);
            Assert.DoesNotContain("EcommerceDb", response.Message);
        }

        //Que no salga el texto literal no basta: si el mensaje CAMBIARA segun la excepcion, seguiria
        //siendo un canal de informacion para quien sondea la API (distinguir "no hay conexion" de
        //"la tabla no existe" ya es saber demasiado). Este test fija que la respuesta es la misma.
        //Ademas es la red que caza una vuelta atras a ex.Message sin depender de como este redactado
        //el texto generico.
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
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia reventar con el fallo de credenciales.");
            }

            SignUpDto signUpDto = NewSignUpDto();

            //Act: primera llamada.
            var primeraResponse = await _auth.SignUpAsync(signUpDto);

            //Arrange: se reconfigura el mismo doble con una excepcion de otro tipo y otro mensaje.
            //La configuracion nueva sustituye a la anterior para esa llamada.
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
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia reventar ahora con el timeout.");
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
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia reventar con el detalle interno.");
            }

            SignInDto signInDto = NewSignInDto();

            //Act
            var response = await _auth.SingInAsync(signInDto);

            //Assert: el caso de uso traduce la excepcion a un fallo inesperado y no filtra el detalle.
            //En el login importa el doble: el mensaje viaja en la respuesta que mas mira quien esta
            //probando credenciales desde fuera.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Null(response.Data);
            Assert.NotEmpty(response.Message);
            Assert.DoesNotContain(detalleInterno, response.Message);
            Assert.DoesNotContain("prod-sql-01", response.Message);
            Assert.DoesNotContain("EcommerceDb", response.Message);
        }

        //El detalle que ya no sale por la respuesta tiene que seguir estando en el log: si no, el fallo
        //desaparece sin rastro y la correccion cambia una fuga por una ceguera. Se espia ILogger.Log,
        //que es el metodo de la interfaz al que acaba llamando la extension LogError; por eso los cinco
        //argumentos y el Arg.AnyType del estado, que es un tipo interno de Microsoft.Extensions.Logging.
        //Es el unico sitio del fichero donde se comprueba el log, y se hace aqui porque escribirlo es
        //parte de la correccion, no un detalle de implementacion.
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
                throw new InvalidOperationException(
                    "Arrange mal montado: el repositorio deberia reventar con ESA misma excepcion.");
            }

            //Consultar el doble del repositorio no cuenta como llamada al logger, pero se limpia igual
            //para que el Received(1) de abajo solo pueda contar lo que escriba el caso de uso.
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
