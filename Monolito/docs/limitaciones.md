# Estado actual y limitaciones conocidas

[← Volver al README](../README.md)

Este es un proyecto de aprendizaje en construcción, y prefiero decir dónde está el trabajo pendiente a que
se descubra leyendo. Lo que sigue no son descuidos que se hayan escapado: es la lista de lo que sé que falta,
con el motivo de cada fallo. Tenlo en cuenta al leer el código: varias piezas funcionan, pero **no están
listas para producción**, y aquí se explica por qué.

Lo que sé que falta, por orden de prioridad:

### Seguridad

| | Pendiente |
|---|---|
| ~~1~~ | ~~**El middleware de excepciones está al final del pipeline y devuelve `ex.Message`.** Registrado después de `MapControllers()`, solo envuelve a los endpoints: lo que falle en autenticación o CORS no lo captura. Y el mensaje crudo de la excepción llega al cliente. Debe ir el primero, responder un texto genérico y dejar el detalle solo en el log. `UserAuthApplication` conserva además sus `try/catch` con la misma fuga, y el endpoint de prueba `UserAuth/boom` sigue publicado.~~ **Resuelto.** `UseMiddlewares()` es ahora el primer middleware del pipeline, antes de CORS y de la autenticación. `GlobalExceptionHandler` y los `catch` de `UserAuthApplication` responden un mensaje fijo ("Unhandle exception") y el detalle de la excepción solo llega al log. `UserAuth/boom` se retiró del controller. Queda como matiz menor, no de seguridad: la respuesta sigue siendo el `Response<T>` propio del proyecto en lugar de `ProblemDetails`. |
| ~~2~~ | ~~**`EnableSensitiveDataLogging()` está activo en todos los entornos** (`DbContextEF.OnConfiguring`). En producción registraría los valores de los parámetros, incluido `PasswordHash`. Y como `GlobalExceptionHandler` registra en `Error`, una excepción de EF con esos valores en el mensaje acabaría en la columna `Exception` de la tabla SQL, que no se purga.~~ **Resuelto.** La llamada se movió de `DbContextEF.OnConfiguring` a `AddInfrastructureServices`, condicionada a `environment.IsDevelopment()`: en producción no se registran valores de parámetros. `OnConfiguring` ya no tiene esa rama. |
| ~~3~~ | ~~**`SignIn` permite enumerar usuarios.** Un email inexistente y una contraseña incorrecta dan respuestas distinguibles. Debe ser un único 401 genérico.~~ **Resuelto.** `UserAuthApplication.SingInAsync` responde el mismo mensaje ("Invalid credentials") y el mismo `ErrorType.Unauthorized` tanto si el email no existe como si la contraseña es incorrecta; el controller lo traduce siempre a **401**. |
| 19 | **Emails de usuario persistidos en la tabla de logs.** `UserAuthApplication` registra `"User already exists: {Email}"` y `"Failed to create user: {Email}"` en `Warning`, y el error de alta en `Error` con el mismo dato: los tres llegan a SQL, en `Message` y en `Properties`, sin fecha de caducidad. No es un secreto, pero es un dato personal guardado indefinidamente sin necesidad. Registrar un identificador en lugar del email, o bajar a `Information` los dos avisos para que se queden en el fichero rotado. |
| 23 | **Al 429 le falta la cabecera `Retry-After`.** El rate limiter ya está particionado por IP (`RateLimitPartition.GetFixedWindowLimiter` con la IP del cliente como clave) y `SignIn`/`SignUp` tienen su propia política, más estricta y sin cola (`auth-limited`: 3 peticiones por minuto, `QueueLimit` 0, frente a las 4 cada 30 s de la política general). Lo que queda es decirle al cliente cuánto esperar: la respuesta 429 no lleva hoy `Retry-After`. |

### Corrección

| | Pendiente |
|---|---|
| 24 | **La caché no se invalida nunca.** `GetAllCustomers` se guarda en Redis con una hora de caducidad absoluta y doce minutos deslizantes, y **no hay un solo `RemoveAsync` en toda la solución**: crear, actualizar o borrar un cliente no toca la caché, así que el listado sigue devolviendo el estado anterior hasta que la entrada expira sola. Es el fallo funcional más grave que hay ahora mismo, y afecta a v3 y v4 a la vez. La invalidación va en un `SaveChangesInterceptor` sobre `SavedChangesAsync`, por lo explicado en [*Caché distribuida*](decisiones-tecnicas.md#caché-distribuida-con-redis-ejercicio-del-patrón-cache-aside). |
| 4 | **Actualizar con los mismos datos devuelve 500** en v2, v3 y v4: EF no escribe nada, `SaveChangesAsync` devuelve 0 y el caso de uso lo traduce a error. Contradice lo explicado en *Unit of Work*. Lo fijan tres tests de caracterización, uno por versión. |
| 5 | **`CustomerDto` no expone `Id`**: el listado devuelve clientes que luego no se pueden identificar para actualizar o borrar. |
| 6 | **`DefaultApiVersion` apunta a `1.0`, obsoleta.** Debe apuntar a la vigente. *(`UserAuthController` ya declara la `4.0`.)* |
| 7 | **La validación de `Jwt:Key` es asimétrica**: se comprueba al emitir el token, no al arrancar. La comprobación debe estar en el arranque (`JwtOptions` con `ValidateOnStart()`). |
| 25 | **`CacheConfiguration` valida tarde y parsea dos veces.** Lanza `InvalidOperationException` en el primer *cache miss* —es decir, en runtime, dentro de una petición de usuario, que acabará en 500— en lugar de al arrancar, que es justo el criterio que sí se aplicó en `RateLimiterConfiguration`. Además `CustomerReadRepository` la llama **dos veces seguidas** para leer `[0]` y `[1]`, reparseando la configuración entera cada vez. Y devolver un `TimeSpan[]` de dos posiciones pide ser un tipo con nombres, como ya lo es `RateLimiterSettings`. |
| 26 | **Si Redis no responde, el endpoint se cae.** `AbortOnConnectFail = false` está comentado y la lectura no tiene red de seguridad, así que una caída de la caché se convierte en un 500 en `GetAllAsync` en lugar de en una consulta a la base de datos. El health check lo *reporta*, pero no protege la petición: una caché debe ser *best-effort*. |
| 20 | **v4 responde los errores de validación con otro contrato.** `Response<T>` tiene ahora dos colecciones para lo mismo, con nombres que se diferencian en una letra: `Errors` (diccionario por propiedad, v1–v3) y `Error` (lista de `BaseError`, v4). Las dos se serializan en todas las respuestas —`Error: null` en v1–v3, `Errors: {}` en v4—, `Error` no tiene inicializador, y el 400 de v4 sale con `ErrorType = None`, que el propio enum define como *operación correcta*. Además `BaseError.PropertyMessage` guarda el nombre de la propiedad, no un mensaje, y el comentario de `ValidationExceptionCustom` dice que los errores van "agrupados por propiedad, igual que `Response.Errors`", cuando son una lista plana. |

### Tests

| | Pendiente |
|---|---|
| 8 | **La frontera HTTP no tiene ni un test.** Falta un test de integración con `WebApplicationFactory` que recorra SignUp → SignIn → CRUD, y que ejecute **los mismos casos contra v1, v2, v3 y v4** para demostrar que comparten contrato —o, en el caso del 400 de v4, dónde deja de compartirlo—. |
| ~~9~~ | ~~**Handlers de v3 y v4 sin cubrir.**~~ **Resuelto.** |
| 31 | **`ConfigureServicesTest` no incluye `ICustomerReadRepository` ni la resolución de los handlers de MediatR**, y `LoggingBehaviour` no tiene tests: su clasificación por `ErrorType` es lógica pura y fácil de fijar. |
| 29 | **La caché no tiene ni un test.** `CustomerReadRepository` es la única clase de `Infrastructure` sin cubrir, y su lógica no es trivial: acierto, fallo, guardado con las caducidades correctas y —cuando exista— desalojo después del commit. `IDistributedCache` es una interfaz, así que se dobla con NSubstitute y no hace falta levantar Redis para probarlo. |
| ~~21~~ | ~~**Un test en rojo**: `SignUpAsync_TraduceLaExcepcionAFalloInesperado`.~~ **Resuelto.** Se retiró la aserción `_logger.Received(1).LogError(...)`: en `ILogger<T>`, `LogError` es un método de extensión estático que NSubstitute no puede interceptar —los `Arg.Any` se quedaban sin consumir y saltaba `RedundantArgumentMatcherException`—. El test conserva lo que tenía que fijar, que la excepción se traduce a `Unexpected`, y el porqué de la aserción ausente queda escrito en el propio test, para que no vuelva a aparecer. |

### Diseño

| | Pendiente |
|---|---|
| 10 | **Pipeline con dos behaviors, y la validación resuelta con excepción.** `LoggingBehaviour` (v3 y v4) y `ValidationBehaviour` (v4) están montados. Queda pendiente la versión sin excepción de la validación —miembro estático abstracto para construir `TResponse.Invalid(...)`, ver [*v3 → v4*](evolucion-v1-v4.md#v3--v4-la-validación-sale-del-handler)—. `UnhandledExceptionBehavior` duplicaría a `GlobalExceptionHandler`, y `TransactionBehavior` contradiría la decisión de que el caso de uso fije el límite transaccional: solo tendrían sentido como decisiones explícitas, no por completar el catálogo. |
| 11 | **El lado de lectura devuelve entidades**, y `GetByIdAsync` usa `FindAsync` con tracking. En CQRS la consulta debería proyectar directamente al DTO con `AsNoTracking()`. |
| 27 | **Se cachea la entidad de dominio y la clave no está versionada.** `CustomerReadRepository` serializa `Customer` entero —los cuatro campos de auditoría incluidos— bajo `eCacheKey.GetAllCustomers`. El modelo de dominio acaba siendo el contrato de serialización de Redis: el día que `Customer` cambie de forma, las entradas ya guardadas deserializarán a medias y **sin error visible**. Hace falta un modelo de caché propio de `Infrastructure` —plano, con solo los campos que se usan— y una clave versionada (`customers:v1:all`) que convierta un cambio de forma en un *miss* limpio. Consecuencia colateral de cachear la entidad: el mapeo a DTO se paga entero también en los aciertos, así que lo único que se ahorra es el viaje a la base de datos. |
| 28 | **La caché alteró el contrato comparativo de v3.** v3 y v4 comparten `ICustomerReadRepository`, así que la caché entró en las dos a la vez. La tabla comparativa del [README](../README.md#las-cuatro-versiones) presentaba v3 → v4 como "solo cambia dónde vive la validación", y desde entonces ya no es cierto: ambas comparten además una caché sin invalidar. Lo que hay que decidir no es solo si se cachea, sino **en qué versiones**; el lado de lectura compartido hace que cualquier añadido ahí se propague sin pedir permiso. |
| 12 | **El dominio es anémico** y las reglas de `Customer` están repartidas: tres validadores con reglas idénticas, una configuración de EF que admite nulos que los validadores rechazan, y un mapeo que contempla nulos que nunca llegan. Faltan invariantes en la entidad (factory methods, value objects). La detección de duplicados compara los diez campos y no se apoya en un índice único. |
| 13 | **Generar el JWT y hashear contraseñas son infraestructura**, pero viven en `Application` (`JwtApplication` lee `IConfiguration`) y en el repositorio (`UserRepository.CreateUserAsync`). Deberían ser puertos con la implementación en `Infrastructure`. |
| 14 | **Duplicación que no forma parte de la comparación entre versiones**: `ToActionResult` repetido en los cuatro controllers y tres sobrecargas idénticas de `ManualMappingCustomer` (DTO, command de v3 y command de v4). |
| 22 | **`ValidationBehaviour` ejecuta los validadores en paralelo** con `Task.WhenAll`. Hoy es inocuo porque todas las reglas son síncronas, pero el día que un validador use `MustAsync` contra el `DbContext` —comprobar un duplicado, por ejemplo— dos validadores de la misma petición lo usarían a la vez, y EF Core no admite operaciones concurrentes sobre la misma instancia. Validar en secuencia elimina el riesgo sin coste apreciable. |

### Higiene

| | Pendiente |
|---|---|
| 15 | `DbContextEF` depende de `IConfiguration` para una rama muerta de `OnConfiguring`. `IUnitOfWork` expone `_customersUoW` y `_user` con prefijo de campo privado. `Customer.Id` es `int?`. |
| 16 | 13 warnings de nulabilidad enterrados bajo los `CS1591` de documentación XML, y avisos del analizador de `Asp.Versioning` (`AV0013`, `AV0016`, `AV0029`). |
| 17 | `Microsoft.AspNetCore.Identity` 2.3 es el paquete heredado de ASP.NET Core 2.x; `PasswordHasher<T>` está en `Microsoft.Extensions.Identity.Core`. `Transversal` mezcla tipos puros con la configuración de Serilog, y arrastra ASP.NET Core hasta `Application` por dependencia transitiva. |
| 18 | Rutas con el verbo en la URL, sin refresh token ni roles y caducidad del token fijada en código. **Sin paginación en `GetAll`**, que además es el endpoint cacheado: cachear un `SELECT` sin límite lo encarece según crece la tabla, así que paginar va **antes** que afinar la caché. *(El healthcheck ya está: hecho en el hito 16.)* |
| 30 | `_distributedCache.GetAsync(...)` no recibe el `CancellationToken` que sí se pasa al `SetAsync` tres líneas más abajo, así que una petición cancelada sigue esperando a Redis. |

Menores, anotados para no perderlos: `Discount` y `DiscountStatus` son código muerto; el sink de Serilog a
SQL Server apunta a la misma base de datos que la aplicación; con seis proyectos repitiendo
`TargetFramework` y versiones de paquetes ya compensa un `Directory.Build.props` y *Central Package
Management*; y la densidad de comentarios —deliberada, y explicada en el [README](../README.md)— mezcla dos cosas que no
valen lo mismo: los que explican el *porqué* (CORS fuera del `if`, `TryParseExact`, `IValidatableRequest`)
y los que repiten lo que el código ya dice, como `CustomerConfiguration`, donde diez comentarios describen
lo que declaran diez llamadas a `HasMaxLength`. Los segundos envejecen mal y sobran.

**Build y tests:** compila sin errores; **271 de 271 tests en verde** — el rojo del pendiente nº 21 está
resuelto.
