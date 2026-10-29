# Decisiones técnicas de Ticketing.Command

[← Volver al README](../../README.md) · [Decisiones generales](../decisiones-tecnicas.md)

Las del servicio de escritura que tienen algo que explicar. Kafka, el contrato común y la independencia entre
servicios están en las [decisiones generales](../decisiones-tecnicas.md).

### MongoDB como replica set de un solo nodo

Las transacciones de MongoDB **solo existen en un replica set**; en un servidor suelto, `StartTransaction` falla.
Como el proyecto quiere practicar sesiones y transacciones, el compose arranca `mongod` con `--replSet rs0`
aunque tenga un único miembro. El *healthcheck* inicializa el replica set (`rs.status()` y, si no existe,
`rs.initiate(...)`), así que el contenedor pasa a *healthy* sin pasos manuales. El miembro se anuncia como
`host.docker.internal:27017` y la cadena de conexión lleva `?replicaSet=rs0`, para que la API (en el host) y el
contenedor se alcancen por el mismo nombre.

### Unit of Work sobre las sesiones de Mongo

Como en el monolito, el límite transaccional lo decide el **caso de uso**: el handler abre la transacción, los
repositorios operan dentro y el handler confirma o deshace. Aquí no hay un `DbContext` que haga de UoW, así que
se construye: `IMongoSessionContext` y `UnitOfWork`, y los repositorios leen la sesión del contexto en vez de
recibirla (ver [*Sesión y repositorios*](arquitectura.md#sesión-y-repositorios)). `Application` no conoce el
driver.

> Al crear un ticket, la transacción envuelve una lectura y **un único `insertOne`**, que ya es atómico por sí
> solo. Es un ejercicio de la técnica, como la caché del monolito, no una necesidad medida.

### Agregado inmutable con las reglas en un servicio de dominio

El agregado es un **valor inmutable** y las transiciones viven en `AggregateService<T>` y su implementación por
agregado; cada operación devuelve uno nuevo. Ganancia: lo común (subir versión, acumular cambios, reproducir,
confirmar) está escrito **una vez**; un agregado no queda a medio cambiar; y el dominio son funciones sin
dependencias, fáciles de probar. Coste: cada propiedad nueva obliga a tocar el constructor, `CopyWith` y cada
`Apply`, y las reglas se buscan en el servicio. Ver [*Agregados*](agregados.md#las-tres-transiciones).

### `VersionBeforeChanges` en el agregado

El control de concurrencia necesita la versión **en el almacén** al cargarse, no cuántos eventos lleva en memoria.
Podría calcularse (`Version - UncommittedChanges.Count`); aquí es un dato propio que cada transición fija
explícitamente.

### `EventStore` en `Infrastructure`; `EventModel` y los puertos en `Domain`

`EventStore` se movió de `Domain/Services` a `Infrastructure/Persistence`: la lógica de negocio es la de los
tickets; guardar eventos (envolverlos, numerar versiones, comprobar concurrencia) es infraestructura.
`IEventStore`, `EventModel` e `IEventModelRepository` se quedan en `Domain`, por decisión explícita, aunque solo
los use `Infrastructure`.

### Concurrencia optimista con índice único

Comprobar la versión antes de escribir no basta: dos operaciones simultáneas pueden pasar la comprobación. Lo que
lo impide es el índice único (`AggregateIdentifier`, `Version`); la comprobación se mantiene porque da un error
claro en el caso normal. Ambas acaban en `ConcurrencyException`. El índice se crea al arrancar, fuera de
transacción. Ver [*Event store*](event-store.md#control-de-concurrencia).

### El id del agregado lo genera el dominio, con GUID v7

`TicketAggregateService.Create` genera el id con `Guid.CreateVersion7` y lo mete en el evento: existe antes de
tocar la base de datos, así que el caso de uso puede devolverlo. Un GUID v7 se ordena cronológicamente (mejor para
índices que un v4) y no necesita coordinación entre servicios. El `_id` del documento (`ObjectId`) es otra cosa.

### Los mapeos BSON fuera del dominio

`EventModel` y los eventos son clases limpias, sin atributos de MongoDB: `_id` como `ObjectId`, enums como texto y
discriminadores se configuran en `MongoClassMaps`, en `Infrastructure`. `Domain` y `Common` no deben depender del
driver ni arrastrar decisiones de persistencia. Para que `_t` no dependa del nombre de la clase, `EventBase` es
clase raíz con `AddKnownType` y cada evento fija su discriminador con `SetDiscriminator(nameof(...))`.

### La colección se registra por su nombre en DI

Antes `EventModel` llevaba un atributo propio, `[BsonCollection("eventStore")]`, que `MongoDbContext` leía por
reflexión. Con una sola colección era maquinaria de más y se eliminó. Ahora `AddInfrastructureServices` registra
`IMongoCollection<EventModel>` como *singleton* (es seguro entre hilos) con
`GetCollection<T>("eventStore")`, y `EventModelRepository`, `MongoRepository<T>` y `EnsureMongoIndexesAsync` la
reciben ya resuelta. Coste: `MongoRepository<T>` solo se resuelve para tipos con su `IMongoCollection<T>`
registrada.

### Tipos de agregado y de evento como enums ligados por nombre

`eAggregateType` y `eEventType` limitan los valores del documento (se guardan como texto). `eAggregateType` se
obtiene con `Enum.Parse` del nombre de la clase del agregado: sin mapeo a mano, pero un agregado sin valor en el
enum falla al guardar, en ejecución. `eEventType` vive en `Ticketing.Common` y `EventBase.Type` ya lo trae
puesto; viaja como número, así que sus valores son contrato.

### `Response<T>` y `ErrorType` compartidos en `Common`

Los fallos esperados viajan como valor y `Api` los traduce a HTTP. Está en `Ticketing.Common` para ser compartido.

### Validación de configuración al arrancar

`AddInfrastructureServices` comprueba la cadena de conexión, la base de datos y el host/puerto de Kafka antes de
registrar nada, y `Program.cs` crea los índices antes de `app.Run()`: un error de configuración o un Mongo caído
se ven al arrancar. Hueco conocido: no se comprueba que el broker responda.

### Validación en el pipeline de MediatR, solo para las peticiones marcadas

`ValidationBehaviour<TRequest, TResponse>` ejecuta los `IValidator<TRequest>` antes del handler y, si hay fallos,
lanza `ValidationExceptionCustom`; el handler nunca ve datos inválidos. Exige `TRequest : IValidatableRequest`,
una interfaz vacía que sirve de marca, por dos motivos:

- Con `where TRequest : IRequest` (sin `<T>`) no entraba nunca: los comandos implementan
  `IRequest<Response<string>>`, que es otra interfaz.
- Sin marca se aplicaría a **todas** las peticiones. La v3 del monolito valida dentro del handler y devuelve
  `Response.Invalid`; un behaviour global lanzaría antes y cambiaría ese contrato. Con la marca, la DI salta el
  behaviour para las peticiones que no la llevan.

Consecuencia: un comando nuevo **solo se valida si implementa `IValidatableRequest`**; olvidarlo no da error.

### `LoggingBehaviour` clasifica el resultado

Tras el handler, mira el `Response` vía `IResponse` y elige el nivel según su `ErrorType`:

| Resultado | Nivel | Motivo |
|---|---|---|
| Éxito, `Validation`, `NotFound` | `Information` | Tráfico normal o error del cliente; un escaneo llenaría el registro persistente |
| `Duplicated`, `Unexpected`, `TimeOut`, sin tipo | `Warning` | Interesa conservarlo |

`Unexpected` no sube a `Error` a propósito: las excepciones reales ya las registra `GlobalExceptionHandler` como
`Error`, y así no se confunde "no se guardó nada" con "se cayó la base de datos". Si el handler no devuelve un
`Response<T>`, no hay nada que clasificar. Se registra después de `ValidationBehaviour`; MediatR ejecuta los
behaviours en orden de registro, así que una petición que no valida no llega a este registro.

### Un middleware global para las excepciones

`GlobalExceptionHandler` (`IMiddleware`, en `Api/Modules/MiddlwareAndExceptions`) envuelve el pipeline HTTP y
traduce a JSON con el contrato `Response<T>`:

| Excepción | HTTP | Cuerpo |
|---|---|---|
| `ValidationExceptionCustom` | 400 | `Response.Invalid(errors)`, errores agrupados por propiedad |
| Cualquier otra | 500 | `Response.Fail("Unhandle exception")`; el detalle va solo al log (`LogError`) |

El mensaje y el tipo de la excepción **no se devuelven al cliente** (hay un test). Se registra con
`AddMiddleWareService()` (transient) y se activa con `UseMiddlewares()`, el primero del pipeline. Detalles:

- Serializa con `JsonSerializer` por defecto, mientras que los controllers usan el formateador de MVC: si las
  opciones difieren (mayúsculas), un 400 y un 200 no tendrían el mismo formato. No lo he comprobado en ejecución.
- La carpeta se llama `MiddlwareAndExceptions` (sin la `e`) y el namespace `MiddlewareAndExceptions`; el fichero
  `GlobalExceptions.cs` contiene la clase `GlobalExceptionHandler`.

Los fallos esperados del dominio siguen viajando como valor; el middleware cubre lo que sale como excepción.
