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
lleva `?replicaSet=rs0` por la misma razón.

### Unit of Work sobre las sesiones de Mongo

Mismo razonamiento que en el monolito: el límite transaccional lo decide el **caso de uso**, no el
repositorio. El handler abre la transacción, los repositorios operan dentro de ella y el handler confirma o
deshace.

Lo que cambia respecto a EF Core es que aquí no hay un `DbContext` que haga de Unit of Work: hay que
construirlo. De ahí las tres piezas (`IMongoSession`, `IMongoSessionContext`, `UnitOfWork`) y que los
repositorios lean la sesión del contexto en vez de recibirla. Detalle en
[*Arquitectura*](arquitectura.md#cómo-se-comparten-la-sesión-y-los-repositorios).

> La transacción envuelve **un único `InsertOne`**, que ya es atómico por sí solo; está para que el patrón
> esté montado cuando un caso de uso escriba en más de un documento. Es un ejercicio de la técnica, igual que la caché del monolito, no una necesidad medida.

### Los mapeos BSON fuera del dominio

`EventModel` y los eventos son clases limpias, sin atributos de MongoDB. Todo (`_id` como `ObjectId`, enums
como texto, discriminadores) se configura en `MongoClassMaps`, en `Infrastructure`.

Dos razones: `Domain` y `Common` no deben depender del driver, y el mismo contrato de eventos tiene que
poder consumirlo el servicio de consulta sin arrastrar decisiones de persistencia.

Por la misma razón el nombre de la colección se declara con un atributo propio, `[BsonCollection]`, que solo
lleva un `string` y lo interpreta `MongoDbContext` por reflexión.

### Eventos con discriminador explícito

`EventData` es de tipo `EventBase`, pero lo que se guarda es el evento concreto. Para poder reconstruirlo al
leer, `EventBase` es clase raíz con `AddKnownType` y cada evento fija su discriminador con
`SetDiscriminator(nameof(...))`.

Así el valor de `_t` queda **fijado en el mapeo**, y no depende del nombre de la clase en C#: renombrar una
clase sin querer no deja sin poder leer los eventos ya guardados.

### `Response<T>` y `ErrorType` compartidos en `Common`

El contrato de respuesta se reutiliza del monolito: los fallos esperados viajan como valor y `Api` los
traduce a HTTP. Está en `Ticketing.Common` y no en `Application` para que el servicio de consulta pueda
usar el mismo.

### GUID v7 para el identificador del agregado

`AggregateIdentifier` usa `Guid.CreateVersion7`. Es un GUID cuyos primeros bits son la marca de tiempo, por
lo que se ordena cronológicamente: es mejor para índices que un GUID v4 aleatorio y no necesita coordinación
entre servicios para generarse.

### Un servicio por responsabilidad, una solución

`Ticketing.slnx` agrupa en carpetas lógicas `Ticketing.Command` y `Ticketing.Query`. Cada API es un proyecto
independiente con su propio `Program.cs`, puerto y `appsettings`, de modo que pueden desplegarse y escalar
por separado aunque compartan solución.

| Servicio | HTTP | HTTPS |
|---|---|---|
| `Ticketing.Command.Api` | `5166` | `7249` |
| `Ticketing.Query.Api` | `5270` | `7005` |
