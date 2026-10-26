# Decisiones técnicas

[← Volver al README](../README.md)

Las que tienen algo que explicar. El resto del código intenta ser aburrido a propósito.

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

`eAggregateType` y `eEventType` hacen que en el documento solo puedan aparecer valores conocidos, y se
guardan como texto para que sea legible. El valor se obtiene con `Enum.Parse` a partir del nombre de la
clase del agregado y del `Type` del evento, lo que evita un mapeo a mano por cada uno; a cambio, un
agregado o evento nuevo sin su valor en el enum falla al guardar, en tiempo de ejecución.

### `Response<T>` y `ErrorType` compartidos en `Common`

El contrato de respuesta se reutiliza del monolito: los fallos esperados viajan como valor y `Api` los
traduce a HTTP. Está en `Ticketing.Common` y no en `Application` para que sea un contrato compartido entre
servicios.

### Validación de configuración al arrancar

`AddInfrastructureServices` comprueba la cadena de conexión, el nombre de la base de datos y el host y puerto
de Kafka antes de registrar nada, y `Program.cs` crea los índices antes de `app.Run()`. Un error de
configuración o un Mongo caído se ven al arrancar, no en la primera petición.

Dos huecos conocidos: `KafkaSettings:Topic` se lee al guardar, no al arrancar, y no se comprueba que el
broker responda (el productor se conecta en el primer envío).

### Kafka para publicar los eventos; Mongo sigue siendo la fuente de verdad

Cada evento guardado en el event store se publica también en un topic de Kafka, para que otros servicios (el
de consulta, a futuro) reaccionen sin leer la base de datos del servicio de escritura. Kafka no sustituye al
event store: si hubiera que reconstruir un agregado, se hace desde Mongo.

- **Un broker con Zookeeper**, en el compose. Basta para aprender el flujo; un cluster pide más brokers y
  réplicas.
- **Clave del mensaje = id del agregado**, para que con varias particiones los eventos de un mismo ticket
  mantengan el orden.
- **`IEventProducer` en `Common` y no en `Domain`**: a diferencia de los demás puertos, queda a la vista de
  los dos servicios. A cambio, `Common` referencia `Confluent.Kafka`.
- **La publicación va dentro del bucle de guardado, antes del `Commit`.** Es lo más simple y hace que un
  fallo de Kafka aborte la transacción de Mongo, pero deja una ventana: si falla el `Commit` después de
  enviar, hay un evento en Kafka que Mongo no tiene. La salida habitual es un *outbox*; está sin decidir.

Detalle y limitaciones en [*Mensajería con Kafka*](mensajeria-kafka.md).

### PostgreSQL con EF Core para el modelo de lectura

`Ticketing.Query` guarda el estado ya proyectado en tablas relacionales (`tickets`, `employees`,
`ticket_types` y la tabla intermedia `ticket_employees`), mientras que `Command` mantiene la secuencia de
eventos en MongoDB. Cada lado usa el almacén que encaja con su trabajo: eventos inmutables añadidos al final
frente a consultas con relaciones. Las dos bases son independientes: `Query` nunca lee la de `Command`.

- **Esquema por migraciones:** `Program.cs` ejecuta `Migrate()` al arrancar; la migración vive en
  `Infrastructure`, y `Api` es el proyecto de inicio para `dotnet ef`.
- **Value object `Address` como `ComplexType`:** sus columnas van en `employees`, sin tabla ni `Id` propios.
- **Cadena de conexión fuera del repo:** `PostgresConnection` va vacía en `appsettings.json` y se aporta por
  secretos.

Estructura y modelo completo en [*Estructura de Ticketing.Query*](query-estructura.md).

### Un servicio por responsabilidad, una solución

`Ticketing.slnx` agrupa en carpetas lógicas `Ticketing.Command` y `Ticketing.Query`, con `Ticketing.Common`
en la raíz. Cada API es un proyecto independiente con su propio `Program.cs`, puerto y `appsettings`, de
modo que pueden desplegarse y escalar por separado aunque compartan solución.

| Servicio | HTTP | HTTPS |
|---|---|---|
| `Ticketing.Command.Api` | `5166` | `7249` |
| `Ticketing.Query.Api` | `5270` | `7005` |
