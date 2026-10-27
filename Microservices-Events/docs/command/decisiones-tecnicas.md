# Decisiones técnicas de Ticketing.Command

[← Volver al README](../../README.md) · [Decisiones generales](../decisiones-tecnicas.md)

Las del servicio de escritura que tienen algo que explicar. Las que afectan a los dos servicios (Kafka, el
contrato común, la independencia entre ellos) están en las [decisiones generales](../decisiones-tecnicas.md).

### MongoDB como replica set de un solo nodo

Las transacciones de MongoDB **solo existen en un replica set** (o en un cluster con *sharding*); en un
servidor suelto, `StartTransaction` falla. Como el proyecto quiere practicar sesiones y transacciones, el
compose arranca `mongod` con `--replSet rs0` aunque tenga un único miembro, que además se parece más a
producción.

El *healthcheck* del contenedor hace el trabajo de inicialización: ejecuta `rs.status()` y, si el replica
set no existe todavía, lo crea con `rs.initiate(...)`. El contenedor pasa a *healthy* cuando el replica set
ya responde, sin un paso manual.

El miembro se anuncia como `host.docker.internal:27017` (con `extra_hosts` hacia `host-gateway`), de modo
que la API, ejecutándose en el host, y el contenedor se alcancen por el mismo nombre. La cadena de conexión
lleva `?replicaSet=rs0` por la misma razón: con ella el driver se conecta al miembro que anuncia el replica
set, no a la dirección que se le dio.

### Unit of Work sobre las sesiones de Mongo

Mismo razonamiento que en el monolito: el límite transaccional lo decide el **caso de uso**, no el
repositorio. El handler abre la transacción, los repositorios operan dentro de ella y el handler confirma o
deshace.

Lo que cambia respecto a EF Core es que aquí no hay un `DbContext` que haga de Unit of Work: hay que
construirlo. De ahí las dos piezas (`IMongoSessionContext` y `UnitOfWork`) y que los repositorios lean la
sesión del contexto en vez de recibirla. Detalle en
[*Arquitectura*](arquitectura.md#cómo-se-comparten-la-sesión-y-los-repositorios).

`UnitOfWork` habla directamente con `IMongoClient`, y `Application` no conoce el driver.

> Al crear un ticket, la transacción envuelve una lectura del historial y **un único `AddAsync`**
> (un `insertOne`), que ya es atómico por sí solo. Es un ejercicio de la técnica, igual que la caché del monolito, no una necesidad
> medida.

### Agregado inmutable con las reglas en un servicio de dominio

El enfoque habitual en event sourcing es un agregado mutable con sus propios `Apply` y una lista interna de
cambios. Aquí el agregado es un **valor inmutable** (todo de solo lectura, fijado en el constructor) y las
transiciones viven en `AggregateService<T>` y su implementación por agregado, `TicketAggregateService`.
Cada operación devuelve un agregado nuevo.

Lo que se obtiene:

- Lo que es igual para cualquier agregado (subir versión, acumular cambios pendientes, reproducir,
  marcar como confirmado) está escrito **una vez**, en `AggregateService<T>`. Cada agregado solo dice cómo
  le afecta cada evento (`ApplyEvent`), cómo se copia (`CopyWith`) y cómo empieza (`CreateEmpty`).
- Un agregado no puede quedar a medio cambiar: o se obtiene el nuevo, o se conserva el anterior.
- Todo el dominio son funciones de entrada y salida sin dependencias, fácil de probar sin dobles.

Lo que cuesta: cada propiedad de estado nueva obliga a tocar el constructor, `CopyWith` y cada `Apply`, y el
agregado por sí solo no tiene comportamiento: las reglas se buscan en el servicio. Detalle en
[*Agregados*](agregados.md).

### `VersionBeforeChanges` en el agregado

El control de concurrencia necesita saber qué versión tenía el agregado **en el almacén** al cargarse, no
cuántos eventos lleva en memoria. Podría calcularse restando (`Version - UncommittedChanges.Count`); aquí el
agregado lo lleva como dato propio y cada transición lo fija explícitamente, según la tabla de
[*Agregados*](agregados.md#las-tres-transiciones).

### `EventStore` en `Infrastructure`; `EventModel` y los puertos en `Domain`

`EventStore` empezó en `Domain/Services` y se movió a `Infrastructure/Persistence`. El criterio: la lógica
de negocio es la de los tickets; gestionar y guardar eventos (envolverlos en `EventModel`, numerar
versiones, comprobar la concurrencia) es infraestructura. `Application` solo coordina casos de uso.

`IEventStore` se queda en `Domain` como puerto. `EventModel`, `[BsonCollection]` e `IEventModelRepository`
también se quedan en `Domain`, por decisión explícita, aunque solo los use `Infrastructure`.

### Concurrencia optimista con índice único

Comprobar la versión antes de escribir no basta: dos operaciones simultáneas pueden leer la misma versión y
pasar las dos la comprobación. Lo que lo impide de verdad es el índice único (`AggregateIdentifier`,
`Version`): la base de datos solo deja entrar un evento por versión. La comprobación previa se mantiene
porque da un error claro en el caso normal. Las dos acaban en la misma `ConcurrencyException` de `Domain`.

El índice se crea al arrancar, fuera de cualquier transacción, para que exista antes de la primera
escritura: sin él no hay garantía. Detalle en [*Event store*](event-store.md#control-de-concurrencia).

### El id del agregado lo genera el dominio, con GUID v7

`TicketAggregateService.Create` genera el id con `Guid.CreateVersion7` y lo mete en el evento. El id existe
antes de tocar la base de datos, así que el caso de uso puede devolverlo, y el evento lleva consigo el id del
agregado.

Un GUID v7 lleva la marca de tiempo en sus primeros bits, por lo que se ordena cronológicamente: es mejor
para índices que un GUID v4 aleatorio y no necesita coordinación entre servicios para generarse.

El `_id` del documento (`ObjectId`) es otra cosa: identifica el registro en la colección, no el ticket.

### Los mapeos BSON fuera del dominio

`EventModel` y los eventos son clases limpias, sin atributos de MongoDB. Todo (`_id` como `ObjectId`, enums
como texto, discriminadores) se configura en `MongoClassMaps`, en `Infrastructure`.

Dos razones: `Domain` y `Common` no deben depender del driver, y el contrato de eventos de `Common` no debe
arrastrar decisiones de persistencia.

Por la misma razón el nombre de la colección se declara con un atributo propio, `[BsonCollection]`, que solo
lleva un `string` y lo interpreta `MongoDbContext` por reflexión.

### Eventos con discriminador explícito

`EventData` es de tipo `EventBase`, pero lo que se guarda es el evento concreto. Para poder reconstruirlo al
leer, `EventBase` es clase raíz con `AddKnownType` y cada evento fija su discriminador con
`SetDiscriminator(nameof(...))`.

Así el valor de `_t` queda **fijado en el mapeo**, y no depende del nombre de la clase en C#: renombrar una
clase sin querer no deja sin poder leer los eventos ya guardados.

### Tipos de agregado y de evento como enums ligados por nombre

`eAggregateType` y `eEventType` hacen que en el documento solo puedan aparecer valores conocidos, y
`EventModel` los guarda como texto para que sea legible. `eAggregateType` (de `Command`) se obtiene con
`Enum.Parse` a partir del nombre de la clase del agregado, lo que evita un mapeo a mano por cada uno; a
cambio, un agregado nuevo sin su valor en el enum falla al guardar, en tiempo de ejecución.

`eEventType` vive en `Ticketing.Common` porque lo usan los dos servicios, y `EventBase.Type` es ya un
`eEventType`: el evento lo trae puesto y no hace falta convertirlo. Viaja como **número** en el JSON de Kafka y
`Query` lo usa para elegir la clase del evento; por eso sus valores son un contrato y no se cambian.

### `Response<T>` y `ErrorType` compartidos en `Common`

El contrato de respuesta se reutiliza del monolito: los fallos esperados viajan como valor y `Api` los
traduce a HTTP. Está en `Ticketing.Common` y no en `Application` para que sea un contrato compartido entre
servicios.

### Validación de configuración al arrancar

`AddInfrastructureServices` comprueba la cadena de conexión, el nombre de la base de datos y el host y puerto
de Kafka antes de registrar nada, y `Program.cs` crea los índices antes de `app.Run()`. Un error de
configuración o un Mongo caído se ven al arrancar, no en la primera petición.

Un hueco conocido: no se comprueba que el broker responda (el productor se conecta en el primer envío). El
nombre del topic ya no se lee de configuración: es la constante `KafkaTopics.TicketEvents` de
`Ticketing.Common`.
