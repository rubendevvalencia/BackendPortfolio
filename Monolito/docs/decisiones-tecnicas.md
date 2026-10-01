# Decisiones técnicas

[← Volver al README](../README.md)

Las que tienen algo que explicar. El resto del código intenta ser aburrido a propósito.

### `Response<T>` para los fallos esperados, middleware para los inesperados

Que un cliente no exista, que un DTO no valide o que un cliente ya esté registrado **no es excepcional**:
es un resultado previsto del caso de uso. Modelarlo con excepciones sale caro, esconde el flujo de control
y obliga a un `try/catch` en cada llamador. Aquí el fallo esperado forma parte del valor de retorno.

**`Application` dice *qué* falló, `Api` decide *cómo* se comunica.** Por eso existe un `enum ErrorType` y
no un `int statusCode` — la capa de aplicación no sabe qué es un 404:

| `ErrorType` | HTTP |
|---|---|
| `Validation` | 400 |
| `NotFound` | 404 |
| `Duplicated` | 409 |
| `Unexpected` | 500 |

Si mañana estos casos de uso se expusieran por gRPC o por una cola, `Application` no se toca; solo se
escribe otro traductor.

Lo que **sí** es excepcional —una caída de la base de datos, un timeout— ya no se captura en cada caso de
uso. v1 y v2 dejaron de tener `try/catch`: la excepción sube hasta `GlobalExceptionHandler`, un
`IMiddleware` que la registra con Serilog y devuelve un 500. Los tests lo fijan explícitamente
(`*_PropagaLaExcepcion*`): el caso de uso **no** debe tragarse la excepción. Quedan puntos por cerrar en
este middleware — ver [*Estado actual*](limitaciones.md).

**La excepción a la regla es v4**, y está hecha a propósito: su validación sí viaja como excepción
(`ValidationExceptionCustom`), que el middleware traduce a 400 en un `catch` propio y sin registrarla en el
log. El motivo y lo que cuesta están en [*v3 → v4*](evolucion-v1-v4.md#v3--v4-la-validación-sale-del-handler).

### Unit of Work: quién decide cuándo se confirma

El patrón no sirve para "agrupar repositorios" — eso es una consecuencia, no el objetivo. Sirve para
**mover el límite transaccional desde la capa de persistencia hasta el caso de uso**, que es la única que
sabe qué conjunto de cambios forma una unidad indivisible.

La pieza que lo hace posible no está en el código del `UnitOfWork`, sino en el registro de servicios: el
`DbContext` está registrado como **Scoped**, así que `UnitOfWork` y todos los repositorios comparten la
*misma* instancia durante toda la petición. Ese contexto compartido es lo que permite que un único
`SaveChangesAsync` confirme en bloque los cambios registrados desde varios repositorios, dentro de una sola
transacción.

De ahí sale una regla que atraviesa todo el diseño: **si el repositorio no confirma, tampoco puede
informar del resultado.** Por eso `Update` y `Delete` devuelven `void` y `AddAsync` devuelve `Task`.

Y una consecuencia contraintuitiva: **cuando `SaveChangesAsync` devuelve 0, no ha fallado nada.** Significa
que EF no encontró diferencias que escribir — actualizar un registro con los mismos valores es el caso
típico. Con la existencia comprobada antes por el caso de uso, un 0 es una operación correcta sin efecto.
*(El código actual todavía no lo trata así: es el pendiente nº 4.)*

### Repositorio de lectura separado (v3)

`ICustomerReadRepository` hereda de `IBaseReadQueriesRepository<T>`, que solo declara `GetByIdAsync` y
`GetAllAsync`. Separar el contrato hace que la intención sea verificable por el compilador: los handlers de
consulta reciben una dependencia **sin métodos de escritura**, y el listado se lee con `AsNoTracking()`
porque nadie va a modificar esas entidades — EF se ahorra el coste de rastrearlas.

### Duplicados como resultado de negocio (409)

v2 y v3 comprueban si el cliente ya existe antes de darlo de alta y responden `ErrorType.Duplicated` →
**409 Conflict**, en lugar de dejar que el alta prospere o que acabe en un 500. Es un caso previsto, así
que viaja por `Response<T>` como el resto de fallos esperados. v1 no lo hace, y esa diferencia también
forma parte de la comparación.

### Autenticación con JWT

La firma y la validación comparten la sección `Jwt` de la configuración, para que el `Issuer`, la
`Audience` y la clave no puedan desincronizarse entre quien emite el token y quien lo valida.
`ClockSkew = TimeSpan.Zero` elimina el margen de 5 minutos que .NET añade por defecto a la caducidad.

`Issuer` y `Audience` **no son secretos** y viven en `appsettings.json`: viajan dentro del propio token.
Solo la clave firma, y por eso es la única que sale del control de versiones.

El hash de contraseñas usa `IPasswordHasher<T>` de ASP.NET Core Identity — PBKDF2 con salt por usuario,
sin escribir criptografía a mano.

### Auditoría con un interceptor de `SaveChanges`

`CreatedAt`, `CreatedBy`, `LastUpdatedAt` y `LastUpdatedBy` los rellena un `SaveChangesInterceptor` que
recorre el `ChangeTracker`. Como *todos* los cambios pasan por `SaveChanges`, hay un punto único donde
resolver una preocupación que afecta a todas las entidades — en lugar de repetir las mismas cuatro líneas
en cada repositorio.

### Mapeo manual en la actualización

El alta usa AutoMapper, pero la actualización usa un mapeo escrito a mano (`ManualMappingCustomer.MapInto`),
y es deliberado: copia el DTO o el command **encima** de la entidad ya cargada, que sigue *connected* en el
`ChangeTracker`. EF genera entonces un `UPDATE` solo con las columnas que de verdad cambiaron. Mapear a una
instancia nueva la dejaría *detached* y perdería esa detección.

### CORS fuera del `if (IsDevelopment())`

`UseCors()` se registra siempre, no solo en desarrollo: si estuviera dentro del `if`, en producción las
respuestas saldrían sin la cabecera `Access-Control-Allow-Origin`. El fallo es difícil de diagnosticar
porque **CORS lo aplica el navegador, no el servidor** — la API responde 200, los logs se ven bien y desde
curl o Postman funciona, pero el navegador oculta la respuesta al JavaScript.

Va después de `UseHttpsRedirection()` y antes de `UseAuthorization()`, para que el preflight `OPTIONS` se
responda antes de que nadie exija autenticación. Y va precedido de `UseForwardedHeaders()`, porque cuando el
TLS termina en un balanceador la aplicación recibe HTTP plano y `UseHttpsRedirection()` respondería 307
incluso a los preflight.

### Logging con Serilog

Tres destinos con criterios distintos: consola para desarrollo, fichero rotado a diario (7 días) para el
histórico local, y **SQL Server solo a partir de `Warning`** — la tabla de logs no debe llenarse con el
tráfico normal.

| Destino | Recibe | Retención |
|---|---|---|
| Consola | `Information` o más | Ninguna |
| `Logs/log-.txt` | `Information` o más | 7 días |
| Tabla SQL `Logs` | Solo `Warning` o más (`restrictedToMinimumLevel`) | **Indefinida**: nadie la purga |

**Cada llamada al logger es una entrada independiente**, y cada destino la acepta o la descarta entera. Dos
llamadas seguidas sobre el mismo caso de uso —una en `Information` con detalle y otra en `Warning`— no se
fusionan: la tabla SQL recibe solo la segunda, con sus propias propiedades y nada de la primera.

Qué guarda una fila de la tabla: `Message` (ya renderizado), `MessageTemplate`, `Level`, `TimeStamp`,
`Exception` si la hay, y `Properties` en XML con **todas las propiedades estructuradas de esa entrada** —las
del mensaje más las del contexto: `Application` y las de la petición HTTP (`RequestId`, `RequestPath`).
Por eso lo que se escribe en `Warning` o `Error` hay que tratarlo como dato persistido: cualquier propiedad
del mensaje acaba en una tabla que no caduca.

`UseSerilogRequestLogging()` va antes de la autenticación en el pipeline, a propósito: así registra también
los intentos que acaban en 401.

### Quién escribe los logs: interceptor o call site

*Dónde* van los logs es la parte fácil. La decisión está en *quién los escribe*, y hay dos formas que no
compiten entre sí.

**`LoggingBehaviour<TRequest,TResponse>` — cobertura automática.** Un `IPipelineBehavior` de MediatR que
envuelve la ejecución del handler y deja rastro de entrada y salida sin que el handler sepa que existe.
Cero código por caso de uso, a cambio de ver solo el borde: request y response, nunca el interior de la
decisión. Cubre lo que pasa por MediatR como `IRequest` — hoy, v3 y v4.

Aquí el diseño de `Response<T>` paga un dividendo que no se buscaba: como los fallos esperados viajan
*dentro* de la respuesta en lugar de lanzarse, el behaviour puede leer el motivo y **elegir el nivel del log
según el `ErrorType`**. En un diseño que devolviera un DTO pelado, solo vería un `null`. El obstáculo
técnico —dentro del behaviour la respuesta es un `TResponse` genérico— lo resuelve `IResponse`, una cara no
genérica de `Response<T>` que expone `IsSuccess`, `Message` y `ErrorType`.

| Resultado | Nivel | Por qué |
|---|---|---|
| Éxito · `Validation` · `NotFound` | `Information` | Tráfico normal; los detalles ya viajan en el body y un escaneo llenaría la tabla SQL |
| `Duplicated` | `Warning` | Conflicto de negocio que interesa conservar |
| `Unexpected` | `Warning` | Hoy es `SaveChangesAsync` devolviendo 0 — ver [pendiente nº 4](limitaciones.md#corrección) |

El nivel decide el destino: desde `Warning` la entrada también llega a la tabla SQL. `Unexpected` no sube a
`Error` a propósito, porque las excepciones reales ya las registra `GlobalExceptionHandler`, y así "no se
guardó nada" no se confunde con "se cayó la base de datos". El behaviour **observa, pero no altera**:
devuelve exactamente la respuesta que obtuvo de `next()`.

**Por qué no se registra el payload.** La primera versión volcaba request y response completos con
`JsonSerializer.Serialize`. Se retiró porque **la fuga no viene del formato, sino del contenido**: al
serializar el objeto entero se escriben todos sus campos, y un `SignUpDto` lleva la contraseña y un
`TokenDto` el JWT. Las dos salidas aparentes no lo arreglan — pasar el objeto con `{@Payload}` solo cambia
el formato, porque `Password` es una propiedad más, y bajarlo a `Debug` deja la fuga lista para reaparecer
el día que alguien active `Debug` para investigar un fallo. Hoy no se filtra nada porque solo `Customer`
pasa por MediatR; el riesgo se materializa cuando auth migre a commands, y entonces habrá que decidir **qué
campos no se escriben nunca** —una marca como `ISensitiveRequest`, o `[NotLogged]` sobre la propiedad con
`Destructurama`—, no solo en qué nivel.

**Por qué solo sale en v3 y v4: es MediatR, no CQRS.** Lo que activa el behaviour es *cómo llama el
controller*, no el patrón: `_mediator.Send(...)` busca el handler y lo envuelve con los `IPipelineBehavior`
registrados, mientras que `_customerApplication.AddAsync(...)` es una llamada directa sin intermediario
donde engancharse. Por eso el behaviour se registra *dentro* de `AddMediatR(cfg => ...)`, y por eso
`UserAuthController` no saca traza aunque declare la `3.0`: la versión cambia la ruta, no el mecanismo.

Dicho con precisión, **tiene traza todo `Send` de un request con respuesta, siempre que nada por debajo
lance**. De ahí tres huecos, el último ya activo:

- **`Publish` no pasa por el pipeline**: el día que se publique un evento de dominio, no tendrá traza.
- **Un request sin respuesta se salta el behaviour en silencio**, porque no cumple
  `where TRequest : IRequest<TResponse>`. Los diez actuales devuelven `Response<T>`.
- **Si algo por debajo lanza, no se registra nada visible**: `await next()` no está dentro de un `try`.
  **En v4 esto incluye la validación**, porque `ValidationBehaviour` va *dentro* y lanza; el middleware
  responde el 400 sin registrarlo, así que el único rastro es la línea de `UseSerilogRequestLogging()`. Es
  una diferencia real con v3, donde sí queda una línea `Information`. Lo cubriría un
  `UnhandledExceptionBehaviour` (pendiente nº 10).

**`IApiLogger<T>` — logs con intención.** Se inyecta y se llama a mano, en el punto donde se sabe qué regla
se ha incumplido: es lo que el interceptor no puede dar —un código de estado no dice *por qué*— y **el
único enfoque posible fuera de MediatR**, que es donde viven v1, v2 y `UserAuthApplication`.

La regla de reparto, para que los dos no produzcan ruido duplicado: **a mano se registra solo lo que el
código de estado no dice ya.** Con `UseSerilogRequestLogging()` activo cada petición deja ya una entrada con
su status, así que repetir un 404 desde `Application` no aporta nada; sí lo aportan el 409 —el status no
dice con qué dato chocó— y el `SaveChangesAsync` que devuelve 0, que es el caso más opaco del diseño.

**Estado actual:** el behaviour está registrado y activo para v3 y v4. `IApiLogger` y `AppLogger<T>` se
conservan **sin registrar**, como ejemplo del enfoque manual, y `UserAuthApplication` usa hoy `ILogger<T>`
directo. Cablearlo en v1 y v2 es el siguiente paso.

### Rate limiting: versión simplificada, a modo de prueba

> **Aviso:** sirve para ver cómo se registra y se aplica el rate limiter nativo de ASP.NET Core
> (`Microsoft.AspNetCore.RateLimiting`), no como protección lista para producción — ver
> [pendiente nº 23](limitaciones.md#seguridad).

Dos políticas de **ventana fija**, ambas particionadas por IP, en
[`Modules/RateLimiter/RateLimiterExtensions.cs`](../Ecommerce/Modules/RateLimiter/RateLimiterExtensions.cs):
`AddRateLimiting(configuration)` las registra, `app.UseRateLimiter()` las aplica —después de `UseCors()` y
antes de `UseAuthentication()`— y cada controller o acción decide cuál le afecta con `[EnableRateLimiting]`.
`/health` se mapea fuera de los controllers, así que no está limitado.

- **`user-limited`** — la política general, en todos los controllers de `Customer` y por defecto en
  `UserAuthController`. Lee la sección `RateLimiting` de `appsettings.json`.
- **`auth-limited`** — más estricta y **sin cola** (`QueueLimit` 0: en fuerza bruta interesa rechazar rápido,
  no retener la conexión esperando a la siguiente ventana). Pisa a `user-limited` en `SignIn` y `SignUp` vía
  `[EnableRateLimiting("auth-limited")]` a nivel de acción. Lee `SignInRateLimiting`, con su propia clase de
  validación (`SignInRateLimiterConfiguration`), separada a propósito de la de la política general.

```json
"RateLimiting":        { "PermitLimit": 4, "Window": "00:00:30", "QueueLimit": 2 },
"SignInRateLimiting":  { "PermitLimit": 3, "Window": "00:01:00", "QueueLimit": 0 }
```

En ambas, `RateLimitPartition.GetFixedWindowLimiter` usa `httpContext.Connection.RemoteIpAddress` como
clave: cada IP tiene su propio contador, así que un cliente insistente ya no agota el cupo de los demás.

- **La ventana se escribe `"00:00:30"`, no `30`**, y se lee con `TimeSpan.TryParseExact`: `TimeSpan`
  interpreta un entero suelto como **días**, así que un `30` pensado como segundos daría una ventana de 30
  días y el limitador no frenaría nunca. Exigir el formato convierte un error silencioso en uno al arrancar.
- **Configuración inválida, arranque fallido.** Si un valor falta, no parsea o queda fuera de rango,
  `AddRateLimiting` lanza al arrancar nombrando la clave concreta, para las dos políticas. Mejor no arrancar
  que arrancar con un limitador que bloquea todo o no bloquea nada. *(Es el criterio que la caché todavía no
  sigue: nº 25.)*
- **429 en lugar de 503.** Por defecto el middleware responde `503`, que dice "el servidor está caído"
  cuando lo que pasa es que el cliente se ha pasado; `RejectionStatusCode` lo corrige.
- **Delante de la autenticación**, para rechazar sin gastar en validar el JWT y cubrir `SignIn` aunque sea
  `[AllowAnonymous]`. Cuando se agotan los permisos, hasta `QueueLimit` peticiones esperan en cola FIFO
  —salvo en `auth-limited`, que rechaza directamente.

**Lo que la simplificación deja fuera.** El 429 no lleva la cabecera `Retry-After`: el cliente sabe que se ha
pasado, pero no cuánto tiene que esperar para reintentar.

### Caché distribuida con Redis: ejercicio del patrón *cache-aside*

> **Aviso:** montado **a modo de ejercicio del patrón**, no como respuesta a un problema medido. No hay
> carga, ni métricas, ni paginación: el endpoint cacheado devuelve hoy un puñado de filas.

*Cache-aside* —mirar la caché; si no está, ir a la base de datos y guardar— sobre una sola consulta,
`GetAllCustomers`. Todo vive en `Infrastructure`: `AddStackExchangeRedisCache` registra `IDistributedCache`,
[`CustomerReadRepository.GetAllAsync`](../Ecommerce.Infrastructure/Repository/CustomerReadRepository.cs)
implementa el patrón, [`CacheConfiguration`](../Ecommerce.Infrastructure/Data/Cache/CacheConfiguration.cs)
traduce las caducidades de `appsettings.json`, `eCacheKey` guarda las claves como `enum` para que no viajen
como *string* suelto, y `.AddRedis(...)` mete Redis en `/health`.

```json
"Cache": {
  "Default":  { "AbsoluteExpiration": "02:00:00", "SlidingExpiration": "01:00:00" },
  "Policies": { "GetAllCustomers": { "AbsoluteExpiration": "01:00:00", "SlidingExpiration": "00:12:00" } }
}
```

Mismo formato `"hh:mm:ss"` con `TryParseExact` que el rate limiter, y por el mismo motivo.

**Solo se cachea el listado, y es deliberado.** `GetByIdAsync` va contra la clave primaria: SQL Server lo
resuelve con un *seek* y el salto de red hasta Redis puede costar más que la consulta que ahorra, a cambio
de multiplicar la superficie de invalidación. El criterio no es "devuelve muchas filas", sino la
**proporción lectura/escritura**, la **concurrencia sobre la misma clave** y la **tolerancia a datos
viejos**.

**Y "devuelve muchas filas" es el síntoma de otra cosa:** `GetAll` no está paginado (nº 18). Con 100.000
clientes cada acierto traería varios MB por red para deserializarlos y mapearlos enteros —el mapeo se paga
igual, se acierte o no—, lo que puede salir **más caro** que la consulta paginada que debería existir. El
orden sensato es **paginar primero y volver a hacerse la pregunta después**, porque entonces se cachearía
por página y un alta invalidaría todas, no una.

**Por qué la caché vive en `Infrastructure`.** La alternativa era un `ICacheService` declarado en
`Application` con el adaptador en `Infrastructure` —el patrón que pide el pendiente nº 13 para el JWT—, que
cachearía el DTO y ahorraría el mapeo en los aciertos. Se descartó a propósito: **Redis es un almacén de
datos, y aquí ningún almacén asoma por encima de `Infrastructure`**, ni siquiera detrás de una interfaz. La
contrapartida conviene decirla en voz alta: leyendo `DeleteCustomerCommandHandle` no hay **ninguna** pista
de que exista una caché.

**Dónde tiene que ir la invalidación.** De ahí sale un problema que no es evidente: **el repositorio no
puede invalidar.** `CustomerRepositoryUoW` no confirma, así que cuando ejecuta `Delete(customer)` todavía no
ha pasado nada en la base de datos; desalojar ahí y que el `SaveChangesAsync` falle después tiraría una
caché válida, y desalojar antes del commit abre una ventana para que otra petición la repueble con el estado
viejo. La invalidación tiene que ocurrir **después de un commit con éxito**. Manteniendo la caché fuera de
`Application`, la salida es un segundo `SaveChangesInterceptor` —hermano del de auditoría— que en
`SavedChangesAsync` mire si entre los cambios confirmados había alguna entidad `Customer`. Con un matiz: para
entonces el `ChangeTracker` ya las ha dejado en `Unchanged`, así que el estado hay que capturarlo antes y
consumirlo después.

**Lo que le falta al ejemplo.** Un *cache-aside* sin invalidación demuestra la mitad fácil del patrón:

| | Falta | Por qué importa |
|---|---|---|
| 1 | **Invalidar tras el commit** (nº 24) | Hoy un alta, una edición o un borrado no tocan la caché: el listado devuelve el estado anterior hasta que caduca |
| 2 | **Un modelo de caché propio** (nº 27) | Ahora se serializa la entidad `Customer` entera, auditoría incluida: el dominio acaba siendo el contrato de Redis |
| 3 | **Clave versionada** (`customers:v1:all`) | Convierte un cambio de forma en un *miss* limpio, no en una deserialización a medias sin error visible |
| 4 | **Validar la configuración al arrancar** (nº 25) | Hoy lanza en el primer *miss*, dentro de una petición: lo contrario del criterio del rate limiter |
| 5 | **Degradar si Redis cae** (nº 26) | Una caché es *best-effort*: si el almacén no responde se va a la base de datos, no se responde 500 |

### Health checks: el registro baja a `Infrastructure`, la exposición se queda en `Api`

Los health checks nacieron enteros en `Api`, con un `AddHealthCheck(configuration)` que leía las cadenas de
conexión de SQL Server y de Redis. Funcionaba, pero obligaba a `Ecommerce.Api.csproj` a referenciar
**`AspNetCore.HealthChecks.SqlServer` y `.Redis`** —dos paquetes que hablan de almacenes de datos—, justo lo
que la regla *ningún almacén asoma por encima de `Infrastructure`* dice que no debe pasar: la capa que ni
siquiera sabe que existe una base de datos declaraba cómo se comprueba que responde.

El reparto ahora sigue la misma línea que el resto de la solución: **quien abre la conexión, la vigila.**

| Pieza | Dónde vive | Por qué ahí |
|---|---|---|
| `AddHealthChecks()` con `.AddSqlServer(...)`, `.AddRedis(...)` y `.AddCheck<HealthCheckCustome>(...)` | [`Infrastructure/ConfigureServices.cs`](../Ecommerce.Infrastructure/ConfigureServices.cs) | Las sondas comprueban las dos dependencias que **esta misma capa** registra tres líneas más arriba, con las mismas cadenas de conexión |
| `MapHealthChecks("/health")` y `/health/ui` | [`Program.cs`](../Ecommerce/Program.cs) | Las rutas y los códigos HTTP son cosa de `Api` |
| `HealthHtmlUi` | [`Modules/HealthCheck`](../Ecommerce/Modules/HealthCheck/HealthHtmlUi.cs) | Presentación pura: convierte el `HealthReport` en una tabla HTML |

Se lee en los `.csproj`, que es donde estas cosas se demuestran: los paquetes de sonda están ahora en
`Ecommerce.Infrastructure.csproj` y de `Ecommerce.Api.csproj` ha desaparecido cualquier referencia a SQL
Server o Redis por esta vía —queda `AspNetCore.HealthChecks.UI.Client`, que no sonda nada: solo serializa el
informe—. Y `Program.cs` pierde su línea `AddHealthCheck(...)`: añadir mañana una sonda a un almacén nuevo
no obliga a tocar la capa de API.

**Sobre `HealthCheckCustome`.** Es el hueco donde enchufar una comprobación propia, y hoy está relleno con
un `Random` que devuelve `Healthy`, `Degraded` o `Unhealthy`. Está así **a propósito**: es la única forma de
ver los tres estados en `/health/ui` sin tirar de verdad una dependencia. No es una comprobación real y no
debe confundirse con una.

### Arranque que falla de forma visible

Todo `Program.cs` está envuelto en un `try/catch` que escribe en `stderr`, registra con `Log.Fatal` y fija
`Environment.ExitCode = 1`. Sin eso, un fallo de arranque —un puerto ocupado, los User Secrets sin
configurar— acababa en un proceso que salía con código 0 y sin rastro en la consola.
