# Decisiones técnicas de Ticketing.Query

[← Volver al README](../../README.md) · [Decisiones generales](../decisiones-tecnicas.md)

Las del servicio de consulta que tienen algo que explicar.

### PostgreSQL con EF Core para el modelo de lectura

`Ticketing.Query` guarda el estado ya proyectado en tablas relacionales (`tickets`, `employees` y la tabla
intermedia `ticket_employees`), mientras que `Command` mantiene la secuencia de eventos en MongoDB. Cada lado
usa el almacén que encaja con su trabajo: eventos inmutables añadidos al final frente a consultas con
relaciones. Las dos bases son independientes: `Query` nunca lee la de `Command`.

- **Esquema por migraciones:** `Program.cs` ejecuta `Migrate()` al arrancar; la migración vive en
  `Infrastructure`, y `Api` es el proyecto de inicio para `dotnet ef`.
- **Value object `Address` como `ComplexType`:** sus columnas van en `employees`, sin tabla ni `Id` propios.
- **Cadena de conexión fuera del repo:** `PostgresConnection` va vacía en `appsettings.json` y se aporta por
  secretos.

### El consumidor como `IHostedService` con el bucle en una tarea aparte

El consumidor de Kafka es de vida larga y su `Consume()` **bloquea el hilo**. El host de .NET espera a que
`StartAsync` termine antes de seguir arrancando, así que el bucle no puede vivir dentro de `StartAsync`: la API
no terminaría de arrancar. `StartAsync` lanza el bucle con `Task.Run` y vuelve; `StopAsync` lo cancela con un
`CancellationTokenSource` y espera a que cierre el consumer.

La alternativa, `BackgroundService`, hace esa fontanería sola (y además detiene el host si el bucle lanza una
excepción). Se eligió `IHostedService` para ver explícito qué ocurre al arrancar y al parar. El coste: como nadie
espera la tarea del bucle hasta el apagado, una excepción lo detiene **sin parar la API**; por eso el bucle
registra el error en el log antes de relanzarlo.

### Commit manual del offset, después de proyectar

`EnableAutoCommit = false`: el offset se confirma con `Commit` solo cuando la proyección ha terminado bien. Si
se confirmara antes y el handler fallara, el evento se perdería. A cambio, la entrega es "al menos una vez": un
fallo entre proyectar y confirmar reprocesa el mensaje, y la proyección tendría que tolerarlo (idempotencia, aún
sin hacer).

### Un `IServiceScope` por mensaje

El consumidor se registra como singleton, pero `TicketDbContext`, `IUnitOfWork` y los repositorios son
`Scoped`. Se crea un scope por mensaje y de él se resuelve `IEventHandler`: cada evento trabaja con un contexto
limpio y no se arrastra estado de un mensaje al siguiente.

### Un fallo de proyección se propaga como excepción

`ProjectionTicketCreatedCommandHandler` devuelve un `Response` (`NotFound`, `Fail`, `Success`) y no lleva
`try/catch`. `EventHandler` comprueba ese `Response` y, si no es correcto, **lanza** una excepción. Así el
consumidor no ejecuta el `Commit` y el evento no se pierde en silencio. Es una política deliberadamente simple:
un mensaje que no se puede proyectar detiene el consumidor y el error queda en el log. Reintentos acotados o un
topic de mensajes fallidos (*dead-letter*) serían el siguiente paso, y no están decididos.

### `ConsumerConfig` enlazado a una instancia y registrado como singleton

El consumidor pide `ConsumerConfig` directamente en el constructor. Se enlaza la sección `ConsumerConfig` de
`appsettings` a una instancia con `Bind`, se valida (`BootstrapServers` y `GroupId` obligatorios) y se registra
con `AddSingleton`, igual que `Command` hace con `ProducerConfig`. Registrarlo con `Configure<ConsumerConfig>`
habría expuesto solo `IOptions<ConsumerConfig>`, y el constructor no se habría podido resolver.

### Los empleados no llegan por Kafka

El diseño de partida solo mueve eventos de ticket. Los empleados ya existen en PostgreSQL y el consumidor se
limita a **añadirles tickets**, buscándolos por email. Un empleado inexistente es un fallo de proyección. No hay
ningún evento ni flujo que cree empleados; si en el futuro hiciera falta, implicaría un evento nuevo en
`Ticketing.Common`, su agregado en `Command` y su proyección aquí, además de pensar el orden entre eventos de
empleados y de tickets.

### El nombre del topic es una constante compartida

`KafkaTopics.TicketEvents` vive en `Ticketing.Common` y la usan el productor de `Command` y el consumidor de
`Query`. Ver [la decisión general](../decisiones-tecnicas.md#el-nombre-del-topic-es-una-constante-compartida).

Estructura y modelo completo en [*Arquitectura de Ticketing.Query*](arquitectura.md); el flujo del consumidor,
en [*El consumidor de Kafka y la proyección*](consumidor-kafka.md).
