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
decisión. Y cubre únicamente lo que pasa por MediatR como `IRequest` — hoy eso es **v3 y v4**.

Aquí el diseño de `Response<T>` paga un dividendo que no estaba buscado: como los fallos esperados viajan
*dentro* de la respuesta en lugar de lanzarse como excepción, el behaviour puede leer el motivo del fallo y
**elegir el nivel del log según el `ErrorType`**. Sabe que un alta se rechazó por duplicada sin que nadie se
lo cuente. En un diseño que devolviera un DTO pelado, solo vería un `null`.

El obstáculo técnico es que dentro del behaviour la respuesta es un `TResponse` genérico, y no se puede
preguntar si es un `Response<T>` sin conocer el `T` concreto. Lo resuelve `IResponse`, una cara no genérica
de `Response<T>` que expone `IsSuccess`, `Message` y `ErrorType`, y que vive en `Transversal` junto a él
porque ese proyecto no puede referenciar `Application`. Con eso, cada petición deja una única línea con el
nivel que le corresponde:

| Resultado | Nivel | Por qué |
|---|---|---|
| Éxito | `Information` | La traza normal |
| `Validation` | `Information` | Error del cliente; los detalles ya viajan en el body |
| `NotFound` | `Information` | Tráfico normal; un escaneo llenaría la tabla SQL |
| `Duplicated` | `Warning` | Conflicto de negocio que interesa conservar |
| `Unexpected` | `Warning` | Hoy es `SaveChangesAsync` devolviendo 0 — ver [pendiente nº 4](limitaciones.md#corrección) |

El nivel decide el destino: desde `Warning` la entrada también llega a la tabla SQL. `Unexpected` no sube a
`Error` a propósito, porque las excepciones reales ya las registra `GlobalExceptionHandler` como `Error`, y
así "no se guardó nada" no se confunde con "se cayó la base de datos".

```
[Information] CreateCustomerCommand -> None:
[Warning]     CreateCustomerCommand -> Duplicated: Customer is already registered
[Information] GetCustomerQuery -> NotFound: Customer with ID 7 not found.
```

El behaviour **observa, pero no altera**: devuelve exactamente la respuesta que obtuvo de `next()`. Registrar
es un efecto secundario; si la sustituyera, el controller recibiría otra cosa distinta de lo que produjo el
handler.

**Por qué no se registra el payload.** La primera versión volcaba request y response completos con
`JsonSerializer.Serialize`, y es tentador para depurar. Se retiró, y conviene dejar claro el motivo porque es
fácil equivocarse con él: **la fuga no viene del formato JSON, sino del contenido.** Al serializar el objeto
entero se escriben todos sus campos, y un `SignUpDto` lleva la contraseña y un `TokenDto` el JWT.

De ahí salen dos trampas:

- **Pasar el objeto con `{@Payload}` no lo arregla.** El operador `@` hace que Serilog descomponga el objeto
  en propiedades, y `Password` es una más. Cambia el formato, no lo que se escribe.
- **Bajarlo a `Debug` tampoco.** Reduce la exposición, porque con el nivel mínimo actual no se escribe, pero
  la fuga reaparece el día que alguien active `Debug` para investigar un fallo.

Hoy no se filtra nada: solo Customer pasa por MediatR y sus requests no llevan secretos. El riesgo es de
diseño y se materializa el día que auth migre a commands. Si entonces hace falta el payload, hay que decidir
qué campos no se escriben nunca, no solo en qué nivel:

- **Excluir los requests sensibles**: una interfaz vacía (`ISensitiveRequest`) que implementan los commands
  de auth, y el behaviour registra solo el nombre cuando la encuentra. Explícito y fácil de revisar.
- **Ocultar campos concretos**: con `Destructurama.Attributed`, `[NotLogged]` o `[LogMasked]` en la
  propiedad del DTO. La protección viaja con el dato y funciona desde cualquier sitio que lo registre.

Las dos exigen pasar el **objeto** a Serilog con `@`. Si se serializa antes a mano, Serilog recibe un string
ya cerrado y los atributos no se aplican.

**Por qué solo sale en v3 y v4: es MediatR, no CQRS.** Es fácil atribuirlo al patrón porque aquí van juntos, pero
CQRS solo separa lecturas de escrituras y no ejecuta nada. Lo que activa el behaviour es *cómo llama el
controller*:

```csharp
// v2 — llamada directa a un método: no hay ningún intermediario donde engancharse
var response = await _customerApplication.AddAsync(customerDto, cancellationToken);

// v3 y v4 — Send busca el handler y, antes de invocarlo, lo envuelve con los IPipelineBehavior registrados
var response = await _mediator.Send(command, cancellationToken);
```

Por eso el behaviour se registra *dentro* de `AddMediatR(cfg => ...)`: es configuración de MediatR, y el
resto del contenedor no sabe que existe. Las combinaciones cruzadas lo confirman: CQRS con los handlers
llamados a mano no sacaría el log, y MediatR con un único request que lee y escribe sí. Y por lo mismo
`UserAuthController` no lo saca aunque declare la `3.0`: la versión cambia la ruta, no el mecanismo.

Dicho con precisión, **tiene traza todo `Send` de un request con respuesta, siempre que nada por debajo
lance**. Tres matices; los dos primeros hoy no afectan a ningún caso, el tercero ya afecta a v4:

- **`Publish` no pasa por el pipeline.** Las notificaciones de MediatR (`INotification`) no atraviesan
  `IPipelineBehavior`; el día que se publique un evento de dominio, no tendrá traza automática.
- **Un request sin respuesta se salta el behaviour en silencio.** La restricción
  `where TRequest : IRequest<TResponse>` no la cumple un command declarado como `IRequest` a secas, y el
  contenedor simplemente no lo aplica —sin error ni aviso—. Los diez requests actuales (cinco en v3, cinco
  en v4) devuelven `Response<T>`; si aparece uno sin retorno, basta con quitar la restricción, de la que
  nada más depende.
- **Si algo por debajo lanza, el behaviour no registra nada visible.** `await next()` no está dentro de un
  `try`, así que la excepción sube sin pasar por la clasificación; solo queda la línea de entrada, que va
  a `Debug` y con el nivel mínimo actual no se escribe. Para un error inesperado no se pierde nada —lo
  registra `GlobalExceptionHandler` como `Error`—, pero el behaviour no lo ve. Es el hueco que cubriría un
  `UnhandledExceptionBehaviour` (pendiente nº 10).

  **En v4 esto incluye la validación.** `ValidationBehaviour` va *dentro* de `LoggingBehaviour` y lanza
  `ValidationExceptionCustom`, así que un fallo de validación de v4 nunca llega a clasificarse como
  `Validation`. Y el `catch` de esa excepción en el middleware responde el 400 **sin registrarla**. El único
  rastro es la línea de `UseSerilogRequestLogging()` con el status. Encaja con la regla de reparto —un fallo
  de validación es tráfico del cliente y el status ya lo dice—, pero es una diferencia real con v3, donde
  sí queda una línea `Information` con el caso de uso.

**`IApiLogger<T>` — logs con intención.** Se inyecta en la clase y se llama a mano, en el punto donde se
sabe qué regla se ha incumplido. Es lo que el interceptor no puede darte —un código de estado no dice
*por qué*— y, sobre todo, **es el único enfoque posible fuera de MediatR**: v1, v2 y `UserAuthApplication`
no pasan por `Send`, así que no entran al pipeline.

La regla de reparto que se sigue aquí, para que los dos no produzcan ruido duplicado: **a mano se registra
solo lo que el código de estado no dice ya.** Con `UseSerilogRequestLogging()` activo, cada petición deja
ya una entrada con su status, así que repetir un 404 desde `Application` no aporta nada.

| Rama del caso de uso | v3 y v4 (behaviour) | v1 y v2 (a mano) |
|---|---|---|
| Validación falla | v3: `Information`, automático · v4: sin entrada, solo el status del request log | No — los errores ya viajan en el body |
| `NotFound` | `Information`, automático | No — tráfico normal |
| `Duplicated` → 409 | `Warning`, automático | **Sí**, `Warning` — el status no dice con qué dato chocó |
| `SaveChangesAsync` devuelve 0 | `Warning`, automático | **Sí**, `Warning` — el caso más opaco del diseño |

Lo único que el behaviour no da es el *sujeto*: registra que el alta chocó, no con qué email, porque para
eso tendría que volcar el request. Si hace falta, se inyecta logger en ese handler concreto.

**Estado actual:** el behaviour está registrado y activo para v3 y v4. `IApiLogger` y su implementación
`AppLogger<T>` se conservan **sin registrar**, como ejemplo del enfoque manual, y `UserAuthApplication` usa
hoy `ILogger<T>` directo. Cablear `IApiLogger` en v1 y v2 es el siguiente paso: cuando esté, el mismo
recurso en cuatro versiones también servirá para contrastar las dos formas de loguear.

### Rate limiting: versión simplificada, a modo de prueba

> **Aviso:** lo que hay montado es una **versión simplificada, a modo de prueba**. Sirve para ver cómo se
> registra y se aplica el rate limiter nativo de ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`), no
> como protección lista para producción. Sus límites están descritos al final de esta sección y en el
> [pendiente nº 23](limitaciones.md#seguridad).

Una única política de **ventana fija** (`fixedWindow`), registrada en
[`Modules/RateLimiter/RateLimiterExtensions.cs`](../Ecommerce/Modules/RateLimiter/RateLimiterExtensions.cs):

| Pieza | Dónde | Qué hace |
|---|---|---|
| `AddRateLimiting(configuration)` | `Program.cs`, al registrar servicios | Lee la sección `RateLimiting` y registra la política |
| `app.UseRateLimiter()` | `Program.cs`, después de `UseCors()` y antes de `UseAuthentication()` | Aplica el limitador en el pipeline |
| `[EnableRateLimiting("fixedWindow")]` | `UserAuthController` y los cuatro `CustomerController` | Decide a qué endpoints afecta la política |

Los valores salen de la configuración, no del código:

```json
"RateLimiting": {
  "PermitLimit": 4,          // peticiones permitidas por ventana
  "Window": "00:00:30",      // duración de la ventana, en formato "hh:mm:ss"
  "QueueLimit": 2            // peticiones que esperan a la siguiente ventana cuando no quedan permisos
}
```

La ventana se escribe **`"00:00:30"`, no `30`**, y no es cosmético: `RateLimiterConfiguration` la lee con
`TimeSpan.TryParseExact` y no con `TryParse` justo por eso — `TimeSpan` interpreta un entero suelto como
**días**, así que un `30` pensado como segundos daría una ventana de 30 días y el limitador no frenaría
nunca. Exigir el formato convierte ese error silencioso en un fallo al arrancar.

- **Cola FIFO** (`QueueProcessingOrder.OldestFirst`): cuando se agotan los permisos, hasta `QueueLimit`
  peticiones esperan sin respuesta a que se abra la siguiente ventana; se atienden de la más antigua a la
  más nueva.
- **429 en lugar de 503.** Por defecto el middleware rechaza con `503 Service Unavailable`, que dice "el
  servidor está caído" cuando lo que pasa es que el cliente se ha pasado. `RejectionStatusCode` lo cambia a
  **`429 Too Many Requests`**.
- **Configuración inválida, arranque fallido.** Si alguno de los tres valores falta, no tiene el formato
  esperado o queda fuera de rango, `AddRateLimiting` lanza `InvalidOperationException` **al arrancar**, con
  un mensaje que nombra la clave concreta, y el `try/catch` de `Program.cs` lo hace visible (ver *Arranque
  que falla de forma visible*). Mejor no arrancar que arrancar con un limitador que bloquea todo o no
  bloquea nada. *(Este criterio es el que todavía no sigue la caché: ver pendiente nº 25.)*
- **Delante de la autenticación.** Al ir antes de `UseAuthentication()`, una petición que sobra se rechaza
  sin gastar en validar el JWT, y `SignIn` queda cubierto aunque sea `[AllowAnonymous]`.
- **`/health` no está limitado**: se mapea con `MapHealthChecks`, fuera de los controllers, y no lleva la
  política.

**Lo que la simplificación deja fuera.** El limitador **no está particionado**: no hay un contador por
cliente, sino **un único contador compartido por toda la API**. Con los valores actuales, entre todos los
usuarios y todos los endpoints marcados caben 4 peticiones cada 30 segundos, así que un solo cliente
insistente agota el cupo de los demás. Para una prueba local es justo lo que se quiere —se provoca el 429 en
cuatro clics—, pero en un despliegue real convierte el limitador en una forma sencilla de tumbar el servicio.

La versión completa pasaría por `RateLimitPartition.GetFixedWindowLimiter` con una clave por cliente —el
usuario del JWT si está autenticado, la IP si no, teniendo en cuenta `UseForwardedHeaders()` detrás de un
balanceador—, una política más estricta y separada para `SignIn`/`SignUp` contra fuerza bruta, y la cabecera
`Retry-After` en el 429.

### Caché distribuida con Redis: ejercicio del patrón *cache-aside*

> **Aviso:** igual que el rate limiter, esto está montado **a modo de ejercicio del patrón**, no como
> respuesta a un problema de rendimiento medido. No hay carga, ni métricas, ni paginación: el endpoint
> cacheado devuelve hoy un puñado de filas. La decisión de si compensa de verdad está aplazada a propósito.
> Lo que sigue explica qué hay, dónde vive, por qué vive ahí y qué le falta al ejemplo para estar completo.

**Qué hay montado.** Una caché *cache-aside* —mirar la caché; si no está, ir a la base de datos y
guardar— sobre una sola consulta, `GetAllCustomers`:

| Pieza | Dónde | Qué hace |
|---|---|---|
| `AddStackExchangeRedisCache` | [`Infrastructure/ConfigureServices.cs`](../Ecommerce.Infrastructure/ConfigureServices.cs) | Registra `IDistributedCache` contra Redis |
| `CustomerReadRepository.GetAllAsync` | [`Infrastructure/Repository`](../Ecommerce.Infrastructure/Repository/CustomerReadRepository.cs) | Lee de Redis; si no hay acierto, consulta con EF y guarda |
| `CacheConfiguration` | [`Infrastructure/Data/Cache`](../Ecommerce.Infrastructure/Data/Cache/CacheConfiguration.cs) | Traduce las caducidades de `appsettings.json` a `TimeSpan` |
| `eCacheKey` | [`Infrastructure/Data/Cache`](../Ecommerce.Infrastructure/Data/Cache/eCacheKey.cs) | Las claves como `enum`, para que no viajen como *string* suelto |
| `.AddRedis(...)` | [`Infrastructure/ConfigureServices.cs`](../Ecommerce.Infrastructure/ConfigureServices.cs) | Mete Redis en `/health` con la etiqueta `caché`, junto al resto de health checks |

Las caducidades salen de la configuración, por política y no del código:

```json
"Cache": {
  "Default":  { "AbsoluteExpiration": "02:00:00", "SlidingExpiration": "01:00:00" },
  "Policies": {
    "GetAllCustomers": { "AbsoluteExpiration": "01:00:00", "SlidingExpiration": "00:12:00" }
  }
}
```

Mismo criterio de formato que en el rate limiter, y por el mismo motivo: `TimeSpan.TryParseExact` con
`"hh:mm:ss"` en lugar de `TryParse`, porque un entero suelto se leería como días.

**Solo se cachea el listado, y es deliberado.** `GetByIdAsync` va contra la clave primaria: SQL Server lo
resuelve con un *seek* sobre el índice agrupado, y el salto de red hasta Redis puede costar más que la
consulta que ahorra. A cambio multiplicaría la superficie de invalidación —una clave por cliente en lugar
de una sola— por una ganancia dudosa. El criterio para decidir qué se cachea no es "devuelve muchas filas",
sino la **proporción lectura/escritura**, la **concurrencia sobre la misma clave** y la **tolerancia a datos
viejos**. El patrón de persistencia no entra en la ecuación: tener Unit of Work y confirmar una sola vez por
caso de uso no hace que una lectura se beneficie más o menos de una caché.

**Y "devuelve muchas filas" es, en realidad, el síntoma de otra cosa.** `GetAll` no está paginado
(pendiente nº 18). Con la tabla pequeña el blob cabe de sobra en Redis; con 100.000 clientes, cada acierto
significa traer varios MB por red, deserializarlos enteros y mapearlos enteros con AutoMapper —el mapeo se
paga igual, se acierte o no—, lo que puede salir **más caro** que la consulta paginada que debería existir.
La caché no arregla un `SELECT` sin límite: lo encarece a medida que la tabla crece. El orden sensato es
**paginar primero y volver a hacerse la pregunta después**, porque una vez paginado el problema cambia de
forma: se cachearía por página, y entonces un alta invalida todas las páginas, no una.

**Por qué la caché vive en `Infrastructure` y no detrás de un puerto en `Application`.** La alternativa
considerada era un `ICacheService` declarado en `Application` con el adaptador de Redis en
`Infrastructure` —el mismo patrón que pide el pendiente nº 13 para el JWT—. Tiene dos ventajas reales:
cachearía el DTO, que es el contrato ya estabilizado por el versionado, y ahorraría el mapeo en cada
acierto. Se ha descartado a propósito: **Redis es un almacén de datos, y aquí la regla es que ningún
almacén asome por encima de `Infrastructure`**, ni siquiera detrás de una interfaz. Elegir la caché
transparente tiene una contrapartida que conviene decir en voz alta: leyendo `DeleteCustomerCommandHandle`
no hay **ninguna** pista de que exista una caché.

**Dónde tiene que ir la invalidación.** De esa decisión sale un problema que no es evidente: **el
repositorio no puede invalidar.** `CustomerRepositoryUoW` no confirma —esa es justamente la regla que
define el Unit of Work aquí—, así que cuando ejecuta `Delete(customer)` todavía no ha pasado nada en la
base de datos. Desalojar ahí y que el `SaveChangesAsync` falle después tiraría una caché válida; desalojar
antes del commit abre una ventana en la que otra petición puede repoblarla con el estado viejo. La
invalidación tiene que ocurrir **después de un commit con éxito**, y quien confirma es el caso de uso.

Manteniendo la caché fuera de `Application`, la salida es un segundo `SaveChangesInterceptor` —hermano del
de auditoría— que en `SavedChangesAsync` mire si entre los cambios confirmados había alguna entidad
`Customer` y, en ese caso, desaloje la clave. Dos detalles lo hacen menos trivial que el de auditoría: el
que corre *después* del commit es `SavedChangesAsync` y no `SavingChangesAsync`, y para entonces el
`ChangeTracker` ya ha dejado las entidades en `Unchanged` — así que el estado hay que capturarlo antes y
consumirlo después.

**Lo que le falta al ejemplo para estar completo.** Un *cache-aside* sin invalidación demuestra la mitad
fácil del patrón; la invalidación es la parte difícil, y es la que no está. Por orden:

| | Falta | Por qué importa |
|---|---|---|
| 1 | **Invalidar tras el commit** (nº 24) | Hoy un alta, una edición o un borrado no tocan la caché: el listado devuelve el estado anterior hasta que la entrada caduca |
| 2 | **Un modelo de caché propio** (nº 27) | Ahora se serializa la entidad `Customer` entera, auditoría incluida: el modelo de dominio acaba siendo el contrato de Redis |
| 3 | **Clave versionada** (`customers:v1:all`) | Convierte un cambio de forma en un *miss* limpio, en lugar de una deserialización a medias sin error visible |
| 4 | **Validar la configuración al arrancar** (nº 25) | Hoy `CacheConfiguration` lanza en el primer *miss*, dentro de una petición: lo contrario del criterio aplicado en el rate limiter |
| 5 | **Degradar si Redis cae** (nº 26) | Una caché es *best-effort*: si el almacén no responde se va a la base de datos, no se responde 500 |

### Health checks: el registro baja a `Infrastructure`, la exposición se queda en `Api`

Los health checks nacieron enteros en la capa de API: un `Modules/HealthCheck/HealthCheckExtensions.cs`
con `AddHealthCheck(configuration)` que leía las cadenas de conexión de SQL Server y de Redis y registraba
las sondas. Funcionaba, pero colocaba la decisión en el sitio equivocado y se notaba en el `.csproj`:
`Ecommerce.Api` tenía que referenciar **`AspNetCore.HealthChecks.SqlServer` y `AspNetCore.HealthChecks.Redis`**
—dos paquetes que hablan de almacenes de datos— solo para poder registrarlos, justo lo que la regla *ningún
almacén de datos asoma por encima de `Infrastructure`* dice que no debe pasar. La capa que ni siquiera sabe
que existe una base de datos estaba declarando cómo se comprueba que esa base de datos responde.

El reparto ahora sigue la misma línea que el resto de la solución: **quien abre la conexión, la vigila.**

| Pieza | Dónde vive | Por qué ahí |
|---|---|---|
| `AddHealthChecks()` con `.AddSqlServer(...)`, `.AddRedis(...)` y `.AddCheck<HealthCheckCustome>(...)` | [`Infrastructure/ConfigureServices.cs`](../Ecommerce.Infrastructure/ConfigureServices.cs) | Las sondas comprueban las dos dependencias que **esta misma capa** registra tres líneas más arriba (`AddDbContext` y `AddStackExchangeRedisCache`), leyendo las mismas cadenas de conexión |
| `HealthCheckCustome` | [`Infrastructure/HealthCheck`](../Ecommerce.Infrastructure/HealthCheck/HealthCheckCustome.cs) | Un `IHealthCheck` propio, con la etiqueta `custom` |
| `MapHealthChecks("/health")` y `MapHealthChecks("/health/ui")` | [`Program.cs`](../Ecommerce/Program.cs) | Las rutas y los códigos HTTP son HTTP: eso es de `Api` |
| `HealthHtmlUi` | [`Modules/HealthCheck`](../Ecommerce/Modules/HealthCheck/HealthHtmlUi.cs) | Presentación pura: convierte el `HealthReport` en una tabla HTML |

El resultado se lee en los `.csproj`, que es donde estas cosas se demuestran: los dos paquetes de sonda se
han movido a `Ecommerce.Infrastructure.csproj` —junto con
`Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`, que es solo el contrato `IHealthCheck`— y de
`Ecommerce.Api.csproj` ha desaparecido cualquier referencia a SQL Server o Redis por esta vía. En `Api`
queda `AspNetCore.HealthChecks.UI.Client`, que no sonda nada: solo serializa el informe en el JSON que
esperan servicios como Azure. Con el registro dentro de `AddInfrastructureServices`, `Program.cs` pierde
también su línea `AddHealthCheck(...)`: añadir mañana una sonda nueva a un almacén nuevo no obliga a tocar
la capa de API.

**Sobre `HealthCheckCustome`.** Es el hueco donde enchufar una comprobación propia —un servicio externo,
una cola, una API de terceros—, y hoy está relleno con un `Random` que devuelve `Healthy`, `Degraded` o
`Unhealthy` según un número entre 1 y 300 ms. Está así **a propósito**: es la única forma de ver los tres
estados en `/health/ui` sin tirar de verdad una dependencia. No es una comprobación real y no debe
confundirse con una; el día que haya un servicio externo que vigilar, la lógica sustituye al `Random` sin
tocar nada más. *(Le sobra un campo `_htmlFormat` sin usar, residuo de cuando la clase vivía junto al HTML.)*

### Arranque que falla de forma visible

Todo `Program.cs` está envuelto en un `try/catch` que escribe en `stderr`, registra con `Log.Fatal` y fija
`Environment.ExitCode = 1`. Sin eso, un fallo de arranque —un puerto ocupado, los User Secrets sin
configurar— acababa en un proceso que salía con código 0 y sin rastro en la consola.
