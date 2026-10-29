# Decisiones técnicas de Ticketing.Query

[← Volver al README](../../README.md) · [Decisiones generales](../decisiones-tecnicas.md)

Las del servicio de consulta que tienen algo que explicar.

### PostgreSQL con EF Core para el modelo de lectura

`Query` guarda el estado proyectado en tablas relacionales (`tickets`, `employees`, `ticket_employees`); `Command`
guarda la secuencia de eventos en MongoDB. Cada lado usa el almacén que encaja con su trabajo, y `Query` nunca lee
la base de `Command`.

- **Esquema por migraciones:** `Program.cs` ejecuta `Migrate()` al arrancar; la migración vive en
  `Infrastructure` y `Api` es el proyecto de inicio para `dotnet ef`.
- **`Address` como `ComplexType`:** sus columnas van en `employees`, sin tabla ni `Id` propios.
- **Cadena de conexión fuera del repo:** `PostgresConnection` va vacía en `appsettings.json` y se aporta por
  secretos.

### El consumidor como `IHostedService` con el bucle en una tarea aparte

`Consume()` **bloquea el hilo** y el host espera a `StartAsync` antes de seguir arrancando, así que el bucle no
puede vivir dentro: `StartAsync` lo lanza con `Task.Run` y vuelve; `StopAsync` lo cancela con un
`CancellationTokenSource` y espera a que cierre el consumer. `BackgroundService` haría esa fontanería solo (y
detendría el host si el bucle lanzara); se eligió `IHostedService` para ver explícito qué ocurre al arrancar y
parar. Coste: como nadie espera la tarea hasta el apagado, una excepción detiene el bucle **sin parar la API**; por
eso el bucle registra el error antes de relanzarlo.

### Un solo consumidor secuencial

El bucle ocupa un hilo del *thread pool* toda la vida de la app y procesa los mensajes de uno en uno: se conserva el
orden de los eventos de un ticket y no hay concurrencia sobre el `DbContext`. Coste: un mensaje lento retrasa a los
siguientes.

### Commit manual del offset, después de proyectar

`EnableAutoCommit = false`: el offset se confirma solo cuando la proyección ha terminado bien. Si fuera antes y el
handler fallara, el evento se perdería. A cambio, la entrega es "al menos una vez" y la proyección tendría que
tolerar el reproceso (idempotencia, sin hacer).

### Un `IServiceScope` por mensaje

El consumidor es singleton, pero `TicketDbContext`, `IUnitOfWork` y los repositorios son `Scoped`. Se crea un scope
por mensaje y de él se resuelve `IEventHandler`: cada evento trabaja con un contexto limpio.

### Un fallo de proyección se propaga como excepción

Los handlers de proyección devuelven un `Response` (`NotFound`, `Fail`, `Success`) sin `try/catch`, y `EventHandler`
**lanza** si no es correcto. Así el consumidor no ejecuta el `Commit` y el evento no se pierde en silencio. Es una
política deliberadamente simple: un mensaje que no se puede proyectar detiene el consumidor y el error queda en el
log. Reintentos acotados o un topic de mensajes fallidos (*dead-letter*) serían el siguiente paso, sin decidir.

### `ConsumerConfig` enlazado a una instancia y registrado como singleton

El consumidor pide `ConsumerConfig` en el constructor. Se enlaza la sección de `appsettings` con `Bind`, se valida
(`BootstrapServers` y `GroupId`) y se registra con `AddSingleton`, como `Command` hace con `ProducerConfig`.
`Configure<ConsumerConfig>` solo habría expuesto `IOptions<ConsumerConfig>` y el constructor no se habría resuelto.

### Los empleados no llegan por Kafka

Los empleados ya existen en PostgreSQL y el consumidor solo les **añade tickets**, buscándolos por email; uno
inexistente es un fallo de proyección. Crear empleados implicaría un evento nuevo en `Ticketing.Common`, su agregado
en `Command` y su proyección aquí, además de pensar el orden entre eventos de empleados y de tickets.

### Las rutas de proyección no se exponen por HTTP

`TicketCreatedCommand` y `TicketUpdatedCommand` escriben en el modelo de lectura y eso solo debe hacerlo el
consumidor. Los endpoints `ReadTicket` y `UpdateTicket` se quitaron: por HTTP se saltarían el event store y el modelo
de lectura dejaría de reflejar los eventos reales. `TicketController` solo expone `AllTickets`, una consulta.

### El nombre del topic es una constante compartida

`KafkaTopics.TicketEvents` vive en `Ticketing.Common`. Ver
[la decisión general](../decisiones-tecnicas.md#el-nombre-del-topic-es-una-constante-compartida).
