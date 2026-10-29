# Decisiones técnicas generales

[← Volver al README](../README.md)

Las que afectan a los dos microservicios. Las propias de cada uno: [`Command`](command/decisiones-tecnicas.md) y
[`Query`](query/decisiones-tecnicas.md).

### Dos microservicios independientes, una solución

Son servicios distintos, no dos capas de una aplicación: cada uno tiene sus proyectos, `Program.cs`,
`appsettings`, puerto y base de datos, y ninguno referencia al otro. Comparten `Ticketing.slnx`, el compose y el
CI por comodidad del proyecto de aprendizaje, no porque el diseño lo exija. Detalle en
[*Arquitectura general*](arquitectura.md#qué-es-independiente-y-qué-se-comparte).

### Una base de datos por servicio

`Command` guarda eventos en MongoDB y `Query` el estado proyectado en PostgreSQL. Ningún servicio lee la base del
otro: si `Query` necesita un dato, lo recibe como evento. A cambio, la consistencia es eventual.

### `Response<T>` y `ErrorType` compartidos en `Common`

El contrato de respuesta se reutiliza del monolito: los fallos esperados viajan como valor y `Api` los traduce a
HTTP. Está en `Ticketing.Common` para que sea compartido entre servicios.

### Kafka publica; Mongo sigue siendo la fuente de verdad

Cada evento guardado se publica también en un topic para que `Query` reaccione sin leer la base de `Command`.
Si hay que reconstruir un agregado, se hace desde Mongo.

- **Un broker con Zookeeper**: basta para aprender el flujo.
- **Clave del mensaje = id del agregado**: con varias particiones, los eventos de un ticket mantienen el orden.
- **`IEventProducer` en `Common`**, no en `Domain`: lo ven los dos servicios, a costa de que `Common` referencie
  `Confluent.Kafka`.
- **La publicación va dentro del bucle de guardado, antes del `Commit`**: un fallo de Kafka aborta la
  transacción, pero si falla el `Commit` tras enviar, Kafka tiene un evento que Mongo no. La salida habitual es un
  *outbox*, sin decidir.

### El nombre del topic es una constante compartida

`KafkaTopics.TicketEvents` está en `Ticketing.Common`: es parte del contrato, como `EventBase` o `eEventType`. Si
cada servicio lo leyera de su `appsettings`, podrían desincronizarse sin error (el consumidor se suscribiría a un
topic vacío). Costes: no se cambia por entorno sin recompilar y renombrarlo obliga a redesplegar ambos. Un
"gestor de topics" con DI se descartó: con un topic basta una clase estática.

### `eEventType` viaja como número

`EventBase.Type` es un enum que el JSON lleva como entero, y el `EventConverter` del consumidor lo busca así. Los
valores **no deben cambiarse ni reordenarse**; un evento nuevo recibe el siguiente número libre.
