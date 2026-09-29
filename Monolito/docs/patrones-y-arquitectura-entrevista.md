# Patrones y decisiones de arquitectura (guion de entrevista)

[← Volver al README](../README.md)

Guion para explicar el proyecto en voz alta. El detalle y los matices están en
[decisiones técnicas](decisiones-tecnicas.md), [evolución v1 → v4](evolucion-v1-v4.md),
[versionado](versionado-api.md), [tests](tests.md), [integración continua](integracion-continua.md) y
[limitaciones](limitaciones.md). Aquí va lo que se cuenta en unos minutos, **dónde está en el código** para poder
enseñarlo, y las preguntas que suelen venir después.

Índice: [1 · Discurso](#1-el-discurso-de-30-segundos-y-el-de-2-minutos) ·
[2 · Arquitectura](#2-arquitectura-general) · [3 · Recorrido de una petición](#3-recorrido-de-una-petición-v4) ·
[4 · Patrones](#4-patrones-de-diseño) · [5 · Principios SOLID](#5-dónde-se-ve-solid) ·
[6 · Transversales](#6-preocupaciones-transversales) · [7 · Seguridad](#7-seguridad) ·
[8 · Testing](#8-testing) · [9 · CI/CD y contenedores](#9-cicd-y-contenedores) ·
[10 · Trade-offs](#10-decisiones-que-conviene-defender-y-su-contrapartida) · [11 · Deuda](#11-deuda-conocida) ·
[12 · Microservicios](#12-hacia-dónde-va-microservicios) · [13 · Preguntas](#13-preguntas-probables)

---

## 1. El discurso de 30 segundos (y el de 2 minutos)

**30 segundos.**

> Es un monorepo en migración de monolito a microservicios con el patrón *Strangler Fig*. El monolito es una API
> de ecommerce en .NET con Clean Architecture y lo usé como laboratorio: el mismo recurso, `Customer`, está
> implementado en **cuatro versiones que conviven**, cada una con una decisión distinta —de un repositorio que
> confirma por su cuenta (v1) a Unit of Work (v2), MediatR con comandos y consultas separados (v3) y validación
> en el pipeline (v4)—. Así puedo comparar qué gana y qué cuesta cada cambio, no solo afirmarlo.

**2 minutos: añadir el hilo conductor.** Todas las decisiones responden a la misma pregunta: *¿quién sabe qué?*

- `Application` sabe **qué** falló (`ErrorType`), no cómo se comunica (HTTP). Lo decide `Api`.
- El **caso de uso** sabe qué cambios forman una unidad indivisible; por eso decide cuándo se confirma
  (Unit of Work), no la persistencia.
- Solo `Infrastructure` sabe que existen SQL Server y Redis; ninguna capa por encima los ve.
- El **handler** no sabe que se valida ni que se loguea: lo hace el pipeline.

Y cierro con honestidad: *hay una lista priorizada de lo que sé que falta* (sección 11), porque prefiero decir
dónde está la deuda a que se descubra leyendo.

---

## 2. Arquitectura general

### 2.1 Estructura de la solución

```
Ecommerce.Domain          ← entidades, contratos de repositorio, ICurrentUser. No depende de nada.
Ecommerce.Transversal     ← Response<T>, ErrorType, logging. Tipos comunes a varias capas.
Ecommerce.Application     ← casos de uso (v1–v4), MediatR, validadores, mapeos. Ref: Domain + Transversal.
Ecommerce.Infrastructure  ← EF Core, Redis, repositorios, interceptores, health checks. Ref: Domain.
Ecommerce (Api)           ← controllers, middleware, auth, versionado, rate limiting. Ref: Application + Infrastructure.
Ecommerce.Test            ← tests unitarios.
Ecommerce.IntegrationTest ← test de integración contra SQL Server real.
```

Las dependencias apuntan hacia dentro y **se demuestran en los `.csproj`**, no solo en un diagrama: `Application`
referencia solo `Domain` y `Transversal`; `Infrastructure` referencia solo `Domain`; nadie referencia `Api`.

### 2.2 Decisiones estructurales

| Decisión | Qué hay | Por qué |
|---|---|---|
| **Clean Architecture** | Capas concéntricas, dependencias hacia el dominio | El caso de uso no depende de EF ni de ASP.NET; se puede probar doblando solo los límites |
| **Ningún almacén por encima de `Infrastructure`** | EF Core, Redis y las sondas de health check viven en `Infrastructure`; `Api` no referencia paquetes de SQL ni de Redis | *Quien abre la conexión, la vigila.* El registro de health checks bajó a `Infrastructure`; en `Api` solo quedan las rutas y el HTML |
| **Inversión de dependencias con puertos en `Domain`** | `IUnitOfWork`, `ICustomerRepositoryUoW`, `ICustomerReadRepository`, `ICurrentUser` | `Domain` declara lo que necesita; `Infrastructure` y `Api` lo implementan. Ej.: `CurrentUser` vive en `Api` porque es la única capa que conoce `HttpContext` |
| **Composition root por capa** | `AddInfrastructureServices`, `AddApplicationServices`, `AddTransversalServices` y un `AddXxx` por módulo de `Api` | `Program.cs` se lee como una lista de capacidades, y cada capa registra lo suyo |
| **Monorepo + Strangler Fig** | `Monolito/` sigue vivo; cada pieza extraída nace en `Microservicios/` (primero `Identity`) | Sin reescritura de golpe: el sistema viejo funciona mientras el nuevo crece |
| **Migraciones al arrancar** | `Database.Migrate()` en `Program.cs`, antes de configurar Serilog | El sink SQL de Serilog necesita que la base exista. *Contrapartida: con varias réplicas arrancando a la vez las migraciones compiten; en producción se haría en un paso de despliegue aparte* |
| **Arranque que falla de forma visible** | Todo `Program.cs` en `try/catch` con `Log.Fatal`, `stderr` y `ExitCode = 1` | Sin eso, un fallo de arranque salía con código 0 y sin rastro |

---

## 3. Recorrido de una petición (v4)

Es la mejor forma de enseñar cómo encajan las piezas. `POST /api/v4/Customer/Create`:

```
Cliente
  │
  ▼
UseMiddlewares()  → GlobalExceptionHandler      (primero: envuelve TODO lo de abajo)
UseForwardedHeaders                              (TLS terminado en el balanceador → X-Forwarded-Proto)
UseSerilogRequestLogging                         (antes de auth: registra también los 401)
UseHttpsRedirection
UseCors                                          (antes de auth: el preflight OPTIONS no lleva token)
UseRateLimiter                                   (antes de auth: rechaza sin gastar en validar el JWT)
UseRequestTimeouts                               (1,5 s por defecto; 2 s la política "CustomPolicy")
UseAuthentication → UseAuthorization             ([Authorize] a nivel de controller)
  │
  ▼
CustomerController.Create  →  _mediator.Send(command, cancellationToken)
  │
  ▼  Pipeline de MediatR (del más externo al más interno)
LoggingBehaviour        ← deja rastro de entrada y de salida, nivel según ErrorType
  ValidationBehaviour   ← ejecuta los IValidator<TRequest>; si fallan lanza ValidationExceptionCustom (no llama a next)
    CreateCustomerCommandHandle
      • AutoMapper: command → Customer
      • CompareInfoInDb → ¿duplicado? → Response.Fail(..., Duplicated)
      • _unitOfWork._customersUoW.AddAsync(customer)      ← solo marca la intención
      • _unitOfWork.SaveChangesAsync(ct)                   ← un único commit
            └─ AuditableEntitySaveChangesInterceptor      (CreatedAt/By, LastUpdatedAt/By)
  │
  ▼
ToActionResult(response)  →  ErrorType → 200 / 404 / 409 / 504 / 500
```

Qué se puede decir mirando este diagrama:

- **Tres puntos de traducción de errores, cada uno con su responsabilidad.** Fallos esperados → `Response<T>` +
  `ToActionResult` del controller. Validación de v4 → excepción traducida a 400 por `GlobalExceptionHandler`.
  Fallos inesperados → mismo middleware, 500 con mensaje fijo, detalle solo en el log.
- **El orden del pipeline es semántica, no estética.** El middleware de excepciones va primero para capturar
  incluso lo que falle en CORS o autenticación; el rate limiter va delante de la autenticación para no gastar
  CPU validando tokens de quien ya está pasado de cupo; Serilog va antes de auth para ver los 401.
- **El handler solo tiene `IUnitOfWork` y `IMapper`.** No hay `IValidator`, ni logger, ni caché, ni HTTP.

---

## 4. Patrones de diseño

Para cada uno: **problema → solución → dónde está → matiz que suele preguntarse.**

### 4.1 Repository + Unit of Work (v1 → v2)

- **Problema.** En v1 cada método del repositorio hace su `SaveChanges`: el límite transaccional vive en
  persistencia. Funciona mientras cada caso de uso toque una tabla; deja de funcionar cuando hay que escribir en
  dos de forma atómica, porque la primera ya está confirmada cuando falla la segunda.
- **Solución.** v2 mueve el límite al caso de uso, que es quien sabe qué cambios son una unidad indivisible.
- **Cómo funciona.** `DbContext` es **Scoped**: `UnitOfWork` y todos los repositorios comparten la misma
  instancia durante la petición, así que un único `SaveChangesAsync` confirma todo en una transacción.
- **Se ve en las firmas** (`IBaseRepositoryUoW`): `AddAsync` devuelve `Task`, `Update` y `Delete` son `void`.
  *Si el repositorio no confirma, tampoco puede informar del resultado.* `Delete` recibe la **entidad**, no el
  id: comprobar si existe es decisión del caso de uso, y así se separa el 404 del 500.
- **Dónde.** `UnitOfWork.cs`, `CustomerRepositoryUoW.cs`, `CustomerApplicationUoW.cs`.
- **Matiz.** *"¿Por qué UoW si EF ya lo es?"* → Porque el valor no es agrupar repositorios sino **decidir quién
  marca el límite transaccional**. Y una consecuencia contraintuitiva: `SaveChangesAsync` = 0 no es un fallo (EF
  no encontró diferencias); hoy el código lo trata como error (pendiente nº 4, fijado con un test de
  caracterización).

### 4.2 Mediator + separación comandos/consultas (v3)

- **Problema.** El controller dependía de un servicio de aplicación concreto y cada operación nueva obligaba a
  tocar su constructor.
- **Solución.** El controller solo conoce `IMediator`; construye un command o una query y lo envía.
- **Lectura y escritura por caminos distintos.** Los commands usan `IUnitOfWork`; las queries usan
  `ICustomerReadRepository`, un contrato **sin métodos de escritura** y con `AsNoTracking()`. Una query no puede
  modificar estado aunque quiera: no tiene con qué.
- **Un validador por command**, y el `CancellationToken` recorre controller → mediator → handler → repositorio.
- **Matiz.** *"¿Es CQRS?"* → Es el mecanismo (MediatR) más una separación de contratos. **No** hay modelos ni
  almacenes de lectura/escritura distintos, y el lado de lectura aún devuelve entidades (pendiente nº 11). Yo lo
  llamo "CQRS ligero".

### 4.3 Pipeline / Chain of Responsibility (v4)

- **Problema.** En v3 cada handler repite las mismas líneas: inyectar `IValidator`, validar, devolver
  `Response.Invalid`. La comprobación de `id <= 0` vivía además en el controller.
- **Solución.** `ValidationBehaviour<TRequest,TResponse>` (un `IPipelineBehavior`) resuelve todos los
  `IValidator<TRequest>`, los ejecuta y, si hay errores, lanza `ValidationExceptionCustom` **sin llamar a
  `next()`**: el handler no llega a ejecutarse y recibe siempre una petición válida.
- **Marker interface.** `where TRequest : IValidatableRequest` (interfaz vacía, solo en v4). El contenedor omite
  un behaviour genérico cuando la petición no cumple la restricción, así que la marca **aísla v3 de v4**: sin
  ella v3 cambiaría de contrato sin que nadie la tocara. `ValidationBehaviourRegistrationTests` lo fija.
- **Orden.** Los behaviours se ejecutan en el orden de registro: `Logging` envuelve a `Validation`, y este al handler.
- **Matiz (la pregunta trampa).** *"Usas una excepción para un fallo esperado, ¿no contradice tu diseño?"* →
  Sí, y es deliberado. Dentro del behaviour `TResponse` es genérico y no hay forma directa de construir un
  `Response<X>.Invalid(...)` sin conocer `X`. Coste: el flujo ya no se lee en la firma, `Application` depende de
  que exista un middleware que traduzca, y el fallo ya no llega a `LoggingBehaviour` como `Response`.
  **Alternativa anotada**: un miembro estático abstracto en una interfaz (C# 11) que `Response<T>` implemente,
  con `where TResponse : IInvalidResponse<TResponse>` para llamar a `TResponse.Invalid(errors)`.

### 4.4 Result pattern (`Response<T>`)

- **Problema.** Que un cliente no exista o esté duplicado no es excepcional; modelarlo con excepciones esconde el
  flujo de control y obliga a `try/catch` en cada llamador.
- **Solución.** El fallo esperado forma parte del valor de retorno: `Response<T>.Success`, `Fail`, `NotFound`,
  `Invalid`. Lo **inesperado** (caída de BD, timeout) sube como excepción hasta el middleware.
- **`ErrorType` en vez de `int statusCode`.** `Application` dice *qué* falló y `Api` decide *cómo* se comunica:
  `Validation`→400, `NotFound`→404, `Duplicated`→409, `TimeOut`→504, `Unauthorized`→401, `Unexpected`→500.
  Mañana se podría exponer por gRPC o una cola sin tocar `Application`.
- **Detalle de diseño.** `ErrorType` empieza en 1, no en 0: por debajo es un `int` y un `default` no debe pasar
  por "operación correcta" sin querer.
- **Dividendo no buscado.** `IResponse` (cara no genérica de `Response<T>`) permite a `LoggingBehaviour` leer el
  motivo del fallo y elegir el nivel del log; con un DTO pelado solo vería un `null`.
- **Deuda.** `Response<T>` tiene dos colecciones para errores (`Errors` y `Error`, nº 20) por la convivencia de
  v1–v3 con v4.

### 4.5 Interceptor sobre `SaveChanges` (auditoría)

- **Problema.** `CreatedAt/By` y `LastUpdatedAt/By` afectan a todas las entidades; repetir cuatro líneas en cada
  repositorio es fácil de olvidar.
- **Solución.** `AuditableEntitySaveChangesInterceptor` recorre el `ChangeTracker` en `SavingChangesAsync` y
  rellena los campos de las entidades que heredan de `BaseAuditEntity`. Como **todos** los cambios pasan por
  `SaveChanges`, hay un punto único.
- **Quién es el usuario.** Lo da `ICurrentUser` (puerto en `Domain`, implementación en `Api` leyendo el claim del
  JWT). Sin token o fuera de una petición HTTP queda `"System"`. No hace ninguna consulta a la tabla `Users`.
- **Detalle.** El parámetro es opcional a propósito: la factoría de diseño y los tests lo crean con `new()`.

### 4.6 Cache-aside (Redis)

- **Qué es.** Mirar la caché; si no está, ir a la BD y guardar. Implementado en `CustomerReadRepository.GetAllAsync`
  con `IDistributedCache`, claves como `enum` (`eCacheKey`) y caducidades en `appsettings.json` (absoluta + deslizante).
- **Cómo lo cuento.** *Como ejercicio del patrón, no como respuesta a un problema medido.* No hay carga ni
  métricas ni paginación.
- **Por qué solo `GetAll`.** `GetById` va por clave primaria: el salto a Redis puede costar más que el *seek*. El
  criterio no es "devuelve muchas filas" sino proporción lectura/escritura, concurrencia sobre la misma clave y
  tolerancia a datos viejos. Y "muchas filas" es el síntoma de que falta paginar: **paginar primero, cachear después**.
- **Por qué en `Infrastructure`.** Redis es un almacén y ninguno asoma por encima de esa capa. Coste: leyendo un
  handler no hay ninguna pista de que exista caché.
- **Por qué el repositorio no puede invalidar.** `CustomerRepositoryUoW` no confirma: desalojar ahí y que el
  `SaveChanges` falle después tiraría una caché válida; desalojar antes abre una ventana para repoblarla con el
  estado viejo. Debe ocurrir **tras un commit con éxito**: un segundo `SaveChangesInterceptor` (`SavedChangesAsync`),
  capturando el estado antes porque después el `ChangeTracker` ya lo deja en `Unchanged`.
- **Lo que falta (y lo sé).** Invalidación (nº 24), modelo de caché propio en lugar de la entidad (nº 27), clave
  versionada, validar configuración al arrancar (nº 25) y degradar si Redis cae (nº 26).

### 4.7 Configuración validada y fallo rápido

- Rate limiter con dos políticas de ventana fija, particionadas por IP: `user-limited` (general) y
  `auth-limited` (`SignIn`/`SignUp`, **sin cola**: en fuerza bruta se rechaza rápido en vez de retener la conexión).
- Las ventanas se escriben `"00:00:30"` y se leen con `TryParseExact`: **`TimeSpan` interpreta un `30` suelto
  como 30 días**, y el limitador no frenaría nunca. Exigir el formato convierte un error silencioso en uno al arrancar.
- **Configuración inválida ⇒ el arranque falla nombrando la clave.** Mejor no arrancar que arrancar con un
  limitador que bloquea todo o nada. (Es el criterio que la caché aún no sigue.)
- Devuelve **429** en lugar del 503 por defecto: 503 dice "el servidor está caído" y aquí el cliente se pasó.

### 4.8 Mapeo dual (AutoMapper y manual)

El alta usa AutoMapper; la actualización usa un mapeo manual (`ManualMappingCustomer.MapInto`) que copia el
DTO/command **encima** de la entidad ya cargada, que sigue *connected* en el `ChangeTracker`. EF genera un
`UPDATE` solo con las columnas que cambiaron. Mapear a una instancia nueva la dejaría *detached* y perdería esa
detección. Además `ApplicationTestBase` llama a `AssertConfigurationIsValid()` para que un mapa incompleto rompa un test.

### 4.9 Dependency Injection y ciclos de vida

- `DbContext`, repositorios, `UnitOfWork` y casos de uso: **Scoped** (una instancia por petición, compartida).
- Un test construye el contenedor real con `BuildServiceProvider(validateScopes: true)` y resuelve el grafo:
  detecta la clase de error que no rompe la compilación pero sí el arranque, como un `AddScoped` olvidado o una
  *captive dependency* (un singleton que retiene un scoped).
- Los validadores se registran por escaneo de ensamblado (`AddValidatorsFromAssembly`); los handlers también
  (`RegisterServicesFromAssembly`). Añadir un command no obliga a tocar el registro.

### 4.10 Versionado de API por segmento de URL

- Cuatro versiones del mismo recurso **conviven**: `api/v1/…` a `api/v4/…`. Elegido por visibilidad (se prueba
  pegando una URL) frente a cabecera o query string; las tres alternativas están comentadas en el código a
  propósito para dejar constancia de que la elección fue consciente.
- **Swagger por versión sin código a mano**: `ConfigureSwaggerOptions` recorre `IApiVersionDescriptionProvider`;
  v3 y v4 se añadieron sin tocar la configuración de Swagger.
- Cuando el contrato **no** cambia no se duplica nada: se declaran varias versiones sobre la misma clase
  (`UserAuthController` con `[ApiVersion("1.0", Deprecated = true)]`, `2.0`, `3.0`, `4.0`). Las versiones obsoletas
  viajan a Swagger y a la cabecera `api-deprecated-versions`.
- **Deuda:** `DefaultApiVersion` apunta a 1.0 (obsoleta) (nº 6).

---

## 5. Dónde se ve SOLID

| Principio | Ejemplo concreto en el proyecto |
|---|---|
| **S**ingle Responsibility | Un handler por caso de uso; un validador por command; el interceptor solo audita; `CurrentUser` solo lee el claim |
| **O**pen/Closed | Añadir una operación = nuevo command + handler, sin tocar el controller ni el registro. Añadir Swagger para una versión nueva = cero cambios. Un behaviour nuevo se engancha al pipeline sin tocar handlers |
| **L**iskov | `ICustomerReadRepository` hereda de `IBaseReadQueriesRepository<T>` y es sustituible en los handlers de consulta por un doble en tests |
| **I**nterface Segregation | Contrato de **solo lectura** separado del de escritura; `IResponse` como cara mínima no genérica de `Response<T>` |
| **D**ependency Inversion | `Application` y `Domain` declaran puertos (`IUnitOfWork`, `ICurrentUser`); `Infrastructure`/`Api` los implementan. Las flechas de los `.csproj` lo prueban. **Excepción conocida:** JWT y hash de contraseñas viven en `Application` (nº 13) |

---

## 6. Preocupaciones transversales

### 6.1 Logging (Serilog) y quién escribe los logs

- **Tres destinos con criterios distintos:** consola (desarrollo), fichero rotado a diario 7 días, y **SQL Server
  solo desde `Warning`** para que la tabla no se llene con tráfico normal.
- **Dos formas de escribir logs que no compiten:**
  - `LoggingBehaviour` — **cobertura automática**, cero código por caso de uso, a cambio de ver solo el borde.
    Nivel según `ErrorType`: éxito/`Validation`/`NotFound` → `Information`; `Duplicated`/`Unexpected`/`TimeOut`
    → `Warning`. **Observa pero no altera**: devuelve exactamente lo que le dio `next()`. `Unexpected` no sube a
    `Error` a propósito, para no confundir "no se guardó" con "se cayó la BD".
  - `IApiLogger<T>` — **logs con intención**, llamados a mano donde se sabe qué regla se incumplió. Es el único
    enfoque posible fuera de MediatR (v1, v2, auth). Regla de reparto: *a mano se registra solo lo que el
    código de estado no dice ya.*
- **No se registra el payload** de request/response. La primera versión lo volcaba con `JsonSerializer.Serialize`
  y se retiró: **la fuga viene del contenido, no del formato** (`SignUpDto` lleva la contraseña, `TokenDto` el
  JWT). Bajar el nivel a `Debug` solo deja la fuga lista para reaparecer el día que alguien lo active.
- **Se sabe por qué solo sale en v3/v4:** es MediatR, no CQRS. `_mediator.Send` envuelve al handler con los
  behaviours; una llamada directa a un servicio de aplicación no tiene dónde engancharse.
- **Huecos conocidos:** `Publish` no pasa por el pipeline; un request sin respuesta se salta el behaviour en
  silencio; si algo por debajo lanza (incluida la validación de v4) no queda traza del behaviour.

### 6.2 Manejo de errores

- `GlobalExceptionHandler` (un `IMiddleware`) es el **primero** del pipeline. Dos `catch`: el de
  `ValidationExceptionCustom` (400, sin log) y el genérico (500, log en `Error`, **mensaje fijo al cliente**).
- Los tests fijan `*_PropagaLaExcepcion*`: el caso de uso **no** debe tragarse la excepción. v1 y v2 ya no tienen `try/catch`.
- Resuelto en la auditoría: antes estaba al final del pipeline y devolvía `ex.Message` al cliente.

### 6.3 Resiliencia y tiempos

- **`EnableRetryOnFailure()`** en EF: reintenta ante fallos transitorios de SQL Server.
- **Request timeouts** de ASP.NET Core: 1,5 s por defecto y política `CustomPolicy` (2 s) por endpoint. Devuelven
  **504** con cuerpo con forma de `Response<T>` (`ErrorType.TimeOut`) escrito en la propia política, porque el
  middleware corta antes de que llegue al controller y `ToActionResult` nunca se ejecuta.
- **Cancelación:** el `CancellationToken` recorre toda la cadena; si el cliente aborta, la consulta se cancela.
  (Excepción anotada: `GetAsync` de la caché aún no lo recibe, nº 30.)

### 6.4 Health checks

- `/health` (JSON compatible con el formato de UI, con código HTTP correcto para Azure y balanceadores) y
  `/health/ui` (HTML propio).
- Sondas de SQL Server y Redis registradas en `Infrastructure`, junto a la conexión que vigilan.
- `HealthCheckCustome` devuelve un estado aleatorio **a propósito**, para ver `Healthy/Degraded/Unhealthy` sin
  tirar una dependencia real. No es una comprobación real y lo digo explícitamente.

### 6.5 CORS y proxies

- `UseCors()` está **fuera** del `if (IsDevelopment())`: CORS lo aplica el navegador, no el servidor, así que si
  falta en producción la API responde 200, los logs se ven bien, curl y Postman funcionan… y el navegador oculta
  la respuesta. Difícil de diagnosticar, por eso está comentado.
- Va tras `UseForwardedHeaders()`: con TLS terminado en el balanceador, la app recibe HTTP plano y
  `UseHttpsRedirection()` respondería 307 incluso a los preflight.
- Los orígenes se cambian sin tocar código (`Config__OrinCors`).

---

## 7. Seguridad

| Área | Decisión |
|---|---|
| **JWT** | `Issuer`/`Audience`/clave en la misma sección `Jwt` para que emisor y validador no se desincronicen. `ClockSkew = 0` (elimina el margen de 5 min por defecto). `Issuer`/`Audience` no son secretos; solo la clave sale del control de versiones |
| **Contraseñas** | `IPasswordHasher<T>` de ASP.NET Core Identity: PBKDF2 con salt por usuario, sin criptografía propia |
| **Enumeración de usuarios** | `SignIn` responde el mismo mensaje y el mismo 401 exista o no el email |
| **Fuga de errores** | El middleware responde un texto fijo; el detalle va solo al log. Se retiró el endpoint de prueba `UserAuth/boom` |
| **Datos sensibles en logs** | `EnableSensitiveDataLogging` solo en Development; el payload no se registra |
| **Fuerza bruta / abuso** | Rate limiting por IP, política estricta y sin cola en `SignIn`/`SignUp`; rate limiter delante de la autenticación |
| **Secretos** | Fuera del repo: User Secrets en desarrollo, fichero montado como *secret* de Docker en contenedor. `gitleaks` en CI |
| **Contenedor** | Usuario sin privilegios (`app`), imagen solo con runtime, TLS asumido terminado delante |

**Nota honesta (la auditoría):** una revisión de seguridad propia produjo 7 hallazgos priorizados; los más
graves ya están corregidos (middleware primero y sin `ex.Message`, sensitive logging por entorno, 401 unificado).
Pendientes: `Retry-After` en el 429 (nº 23) y emails persistidos en la tabla de logs (nº 19). Además, un secreto
antiguo quedó en el historial de git: la contraseña ya está **rotada**, el repo es privado, y se decidió no
reescribir el historial. Es el tipo de incidente que conviene saber contar.

---

## 8. Testing

- **271 tests unitarios + 1 de integración** contra SQL Server real. La suite unitaria corre en CI **sin
  dependencias externas** (ni SQL Server, ni Redis, ni secretos): propiedad que hoy sale gratis y que hay que
  decidir cómo preservar cuando crezcan los tests de integración.
- **Qué se sustituye y qué no.** Se doblan los **límites** de cada capa (repositorios, `IUnitOfWork`, con
  NSubstitute); mapper y validadores se usan **reales**, porque forman parte de lo probado.
- **Repositorios** con EF InMemory y **dos contextos**: se escribe con uno y se lee con otro, para que la lectura
  venga del almacén y no del `ChangeTracker` (probar que *persiste* frente a que *se quedó en memoria*).
- **Lo que solo demuestran los tests:** `SaveChangesAsync` se llama **una sola vez** y en el momento correcto, y
  **ninguna** si falla la validación, el cliente está duplicado o no existe.
- **Validadores con `[Theory]`**: campo vacío, longitud exacta en el límite (válida) y un carácter por encima (inválida).
- **Tests de caracterización**: fijan a propósito comportamientos defectuosos conocidos (sufijos
  `_DefectoDeSeguridad`, `_PendienteDeCorregir`) para que al arreglarlos el rojo diga exactamente qué cambió.
- **Tests del contenedor de DI** y de registro del behaviour (v4 lo recibe, v3 no).
- **Estilo:** tests planos, con el *Arrange* entero en cada uno, sin helpers privados.
- **Falta:** tests de la frontera HTTP recorriendo SignUp → SignIn → CRUD contra v1–v4 (nº 8), y de
  `LoggingBehaviour` y `CustomerReadRepository` (nº 29, 31).

---

## 9. CI/CD y contenedores

- **Tres workflows independientes** en `.github/workflows/`, sobre `ubuntu-latest` y .NET 10:
  `ci-monolito` (restore → build **Release** → test), `docker-monolito` (build de la imagen sin publicarla) y
  `secret-scan` (gitleaks, siempre).
- **Filtro `paths` por proyecto**: es propiedad del *workflow*, no del *job*, y por eso el escaneo de secretos
  va en un fichero aparte. Cuando `Identity` tenga código, su `ci-identity.yaml` se añade igual.
- **Build en `Release`**, la configuración con la que se publicaría: compilar en `Debug` y desplegar en `Release`
  deja fuera justo las diferencias que importan.
- **Punto de entrada = la solución** (`Ecommerce.slnx`), no cada proyecto: añadir un proyecto no obliga a tocar el workflow.
- **Permisos mínimos**: `contents: read` en raíz; solo `secret-scan` sube a `pull-requests: write`.
- **Dockerfile multi-stage**: los `.csproj` se copian primero para que `restore` sea su propia capa cacheable; los
  proyectos de test se quedan fuera; runtime ligero y usuario sin privilegios.
- **docker-compose** para local: API + SQL Server (imagen fijada a CU14 porque una build posterior rompía el login
  de `sa`) + Redis; secretos como archivos montados en `/run/secrets`; `depends_on` con health check.
- **Lo que la CI aún no hace** (para que no se lea como descuido): caché de NuGet y de capas Docker, publicar
  artefactos o cobertura, fijar acciones por SHA, branch protection, y desplegar (Azure pendiente).

---

## 10. Decisiones que conviene defender (y su contrapartida)

| Decisión | Por qué | Contrapartida que reconozco |
|---|---|---|
| Cuatro versiones conviviendo | Comparar decisiones sobre el mismo problema | Nombres con sufijo `UoW` solo por convivencia; un tipo no debería nombrarse por el patrón interno |
| v4 valida lanzando excepción | Sacar la validación del handler | Excepción para un fallo esperado; alternativa sin excepción anotada |
| `Response<T>` para fallos esperados | El flujo se lee en la firma | Dos colecciones de errores conviviendo (nº 20) |
| Caché en `Infrastructure` | Ningún almacén asoma por encima | Ninguna pista de la caché en el handler; hay que invalidar con interceptor |
| Actualización solo por `POST` con `Id` en el body (v3/v4) | El body *es* el command | Se aleja de la convención REST del `PUT` (decisión, no desalineación) |
| Migraciones al arrancar | Simple y necesario para el sink de Serilog | No apto para varias réplicas en producción |
| Rate limit de ventana fija por IP | Ejemplo claro del rate limiter nativo | Sin `Retry-After`; no es protección lista para producción |
| Segmento de URL para versionar | Visible y probable desde el navegador | La URI identifica una representación; en API interna con clientes generados iría en cabecera |
| `LoggingBehaviour` solo en MediatR | Cobertura automática sin tocar handlers | v1/v2/auth necesitan logging manual (`IApiLogger`, aún sin cablear) |

---

## 11. Deuda conocida

Priorizada en [limitaciones](limitaciones.md) (los números son los de esa lista). Las que más conviene mencionar:

| Nº | Qué | Solución prevista |
|---|---|---|
| 24 | **La caché no se invalida nunca**: el fallo funcional más grave, afecta a v3 y v4 | Interceptor en `SavedChangesAsync` + modelo de caché propio + clave versionada |
| 18 | **Sin paginación en `GetAll`**, que además es el endpoint cacheado | Paginar antes de afinar la caché |
| 12 | **Dominio anémico**: reglas repartidas, tres validadores idénticos | Factory methods, value objects, invariantes en la entidad |
| 13 | **JWT y hash de contraseñas** son infraestructura pero viven en `Application` | Puertos en `Application`, implementación en `Infrastructure` |
| 4 | **Actualizar con los mismos datos devuelve 500** | Tratar `SaveChangesAsync` = 0 como éxito sin efecto |
| 8 | **La frontera HTTP no tiene tests de integración completos** | `WebApplicationFactory` recorriendo SignUp → SignIn → CRUD en las cuatro versiones |
| 11 | **El lado de lectura devuelve entidades** | Proyectar directo a DTO con `AsNoTracking()` |
| 22 | **`ValidationBehaviour` ejecuta validadores en paralelo** con `Task.WhenAll` | Ejecutar en secuencia si algún validador usa el `DbContext` |
| 7 | **`Jwt:Key` se valida al emitir, no al arrancar** | `JwtOptions` con `ValidateOnStart()` |

Cómo contarlo: *"Esto lo sé, esto es por qué está así, y esto es lo que haría."* Demuestra criterio más que una lista de virtudes.

---

## 12. Hacia dónde va (microservicios)

**Motivo real de extraer `Identity`:** reutilizarlo en otro proyecto, no separar por separar.

**Fases:**

1. Cerrar v0 (auditoría, tests de integración) y migrar `Users` al patrón v4.
2. **Monolito modular**: módulos `Identity` y `Sales`, con `DbContext` y esquema por módulo.
3. **DDD táctico**: value objects, factorías, setters privados; `Identity` primero.
4. **JWT de HMAC a RS256 + JWKS**, con `sub` = Id y roles.
5. **Extraer `Identity.Api`** con base de datos propia.
6. Refresh tokens, *outbox* y gateway **solo si hacen falta**.

**Acoplamientos detectados *antes* de separar** (no después):

- **`Jwt:Key` simétrica compartida.** `AuthenticationExtension` valida con la misma clave que `JwtApplication`
  firma. Separados, o se distribuye el secreto entre servicios (mala idea) o se pasa a clave asimétrica + JWKS.
- **`IUnitOfWork` agrupa `ICustomerRepositoryUoW` e `IUserRepository`** en la misma transacción física, aunque
  hoy ningún handler de `Customer` use el lado de usuarios. Hay que partirlo o darle a `Identity` el suyo.
- **Un único `DbContext` con `Customer` y `User`.** Sin FK entre ambos, separar el esquema es limpio, pero se
  pierde la atomicidad "gratis" entre los dos `SaveChanges`.
- **Ya bien resuelto:** la auditoría lee el claim del JWT vía `ICurrentUser` (sin consultar `Users`); no hay
  dependencias cruzadas de negocio entre `UserRepository` y los handlers; CORS y rate limiter son autocontenidos.

**Estado a día de hoy:** `Microservicios/Identity/` es una carpeta reservada con un README; el `docker-compose`
lo tiene preparado y comentado. Decirlo tal cual: el monolito está construido, Identity está planificado.

---

## 13. Preguntas probables

**Sobre el enfoque**

- **¿Por qué cuatro versiones del mismo recurso?** Para comparar decisiones sobre el mismo problema. Cada versión
  cambia una cosa y los tests fijan qué cambia.
- **¿Por qué monolito y microservicios a la vez?** Strangler Fig: no reescribir de golpe, extraer pieza a pieza.
  Y el motivo de `Identity` es la reutilización real entre proyectos.
- **¿Qué es lo que más te enorgullece y qué lo que peor te sale?** Orgullo: cada decisión está escrita con su
  contrapartida y los defectos conocidos están fijados con tests de caracterización. Peor: el dominio es anémico
  y la caché quedó a medias.

**Sobre patrones**

- **¿Por qué Unit of Work si EF ya lo es?** El valor no es agrupar repositorios, sino decidir **quién** marca el
  límite transaccional: el caso de uso, no la persistencia.
- **¿Por qué `Response<T>` y no excepciones?** Un cliente que no existe no es excepcional. Con excepciones el
  flujo se esconde y cada llamador necesita `try/catch`. Lo inesperado sí sube como excepción.
- **¿Por qué MediatR? ¿Es CQRS?** MediatR es el mecanismo; aquí separa lectura y escritura en contratos y
  caminos distintos. No hay modelos ni bases separadas.
- **¿Qué inconveniente tiene MediatR?** Indirección: el controller no dice qué handler ejecuta, y hay que
  seguir el tipo del command. A cambio, controllers finos y un pipeline donde enganchar preocupaciones transversales.
- **¿Por qué la validación de v4 lanza excepción si dices que no las usas para lo esperado?** Es el precio de
  sacarla del handler sin resolver `TResponse` genérico. Alternativa anotada con miembros estáticos abstractos.
- **¿Cómo garantizas que v3 no cambia al añadir v4?** La marker interface `IValidatableRequest` y un test que
  comprueba qué peticiones reciben el behaviour.

**Sobre datos y rendimiento**

- **¿Por qué cacheas solo `GetAll`?** `GetById` va por clave primaria y el salto a Redis puede costar más que el
  seek. `GetAll` sin paginar es el síntoma: se pagina primero y luego se replantea la caché.
- **¿Por qué el repositorio no invalida la caché?** Porque no confirma: aún no ha pasado nada en la BD.
  Invalidar antes del commit o tira una caché válida o permite repoblarla con estado viejo.
- **¿Qué pasa si Redis cae?** Hoy el endpoint devuelve 500 (nº 26). Una caché debe ser *best-effort*: degradar a
  la base de datos.
- **¿Por qué mapeo manual en la actualización?** Para mantener la entidad *connected* y que EF genere un `UPDATE`
  solo con las columnas cambiadas.

**Sobre seguridad**

- **¿Cómo evitas fugar datos sensibles en logs?** No se vuelca el payload; se registra el borde con nivel según
  `ErrorType`, y a mano solo lo que el código de estado no dice. Emails en logs es deuda anotada (nº 19).
- **¿Cómo evitas la enumeración de usuarios?** Mismo 401 y mismo mensaje para email inexistente y contraseña incorrecta.
- **¿Qué hiciste con el secreto que se coló en el historial?** Rotar la contraseña, repo privado, y añadir
  `gitleaks` a CI. No reescribí el historial: con la credencial ya rotada el riesgo es bajo.
- **¿Por qué el rate limiter va antes de la autenticación?** Para rechazar sin gastar en validar el JWT y para
  cubrir `SignIn` aunque sea anónimo.

**Sobre operación**

- **¿Cómo lo desplegarías?** Imagen multi-stage desde CI, secretos montados o en un gestor, TLS terminado en el
  balanceador (ya soportado con `ForwardedHeaders`), migraciones en un paso de despliegue y no al arrancar.
- **¿Cómo escalarías?** Sin estado en la app (caché distribuida, JWT), así que réplicas detrás de un balanceador.
  Antes: paginación, invalidación de caché y mover las migraciones fuera del arranque.
- **¿Qué cambiarías primero?** Invalidación de caché tras el commit, paginación, y mover JWT y hash a puertos en
  `Infrastructure`.
