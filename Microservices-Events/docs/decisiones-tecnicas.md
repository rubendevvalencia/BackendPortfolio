# Decisiones técnicas generales

[← Volver al README](../README.md)

Las que afectan a los dos microservicios. Las propias de cada uno están en su carpeta:
[`Ticketing.Command`](command/decisiones-tecnicas.md) y [`Ticketing.Query`](query/decisiones-tecnicas.md).

### Dos microservicios independientes, una solución

`Ticketing.Command` y `Ticketing.Query` son servicios distintos, no dos capas de la misma aplicación. Cada uno
tiene sus cuatro proyectos, su `Program.cs`, sus `appsettings`, su puerto y su base de datos, y ninguno
referencia proyectos del otro: pueden desplegarse y escalar por separado.

Comparten `Ticketing.slnx`, el `docker-compose.yml` y el workflow de CI por comodidad del proyecto de
aprendizaje, no porque el diseño lo exija.

| Servicio | HTTP | HTTPS |
|---|---|---|
| `Ticketing.Command.Api` | `5166` | `7249` |
| `Ticketing.Query.Api` | `5270` | `7005` |

Qué es independiente y qué se comparte, con sus acoplamientos actuales, en
[*Arquitectura general*](arquitectura.md#qué-es-independiente-y-qué-se-comparte).

### Una base de datos por servicio

`Command` guarda eventos en MongoDB y `Query` guarda el estado proyectado en PostgreSQL. Cada lado usa el
almacén que encaja con su trabajo, y ningún servicio lee la base del otro: si `Query` necesita un dato de
`Command`, lo recibe como evento. A cambio, la consistencia entre los dos es eventual.

### `Response<T>` y `ErrorType` compartidos en `Common`

El contrato de respuesta se reutiliza del monolito: los fallos esperados viajan como valor y `Api` los
traduce a HTTP. Está en `Ticketing.Common` y no en `Application` para que sea un contrato compartido entre
servicios.

### Kafka para publicar los eventos; Mongo sigue siendo la fuente de verdad

Cada evento guardado en el event store se publica también en un topic de Kafka, para que otros servicios (el
de consulta) reaccionen sin leer la base de datos del servicio de escritura. Kafka no sustituye al event store:
si hubiera que reconstruir un agregado, se hace desde Mongo.

- **Un broker con Zookeeper**, en el compose. Basta para aprender el flujo; un cluster pide más brokers y
  réplicas.
- **Clave del mensaje = id del agregado**, para que con varias particiones los eventos de un mismo ticket
  mantengan el orden.
- **`IEventProducer` en `Common` y no en `Domain`**: a diferencia de los demás puertos, queda a la vista de
  los dos servicios. A cambio, `Common` referencia `Confluent.Kafka`.
- **El consumidor vive en `Query`** y proyecta cada evento en PostgreSQL, con commit manual del offset después
  de proyectar. Detalle en [*El consumidor de Kafka*](query/consumidor-kafka.md).
- **La publicación va dentro del bucle de guardado, antes del `Commit`.** Es lo más simple y hace que un
  fallo de Kafka aborte la transacción de Mongo, pero deja una ventana: si falla el `Commit` después de
  enviar, hay un evento en Kafka que Mongo no tiene. La salida habitual es un *outbox*; está sin decidir.

### El nombre del topic es una constante compartida

`KafkaTopics.TicketEvents` está en `Ticketing.Common`, junto a los eventos, y lo usan el productor de `Command` y
el consumidor de `Query`. El topic es parte del contrato entre los dos servicios, igual que `EventBase` o
`eEventType`: si cada uno lo leyera de su `appsettings`, se podrían desincronizar sin ningún error, porque el
consumidor se suscribiría a un topic vacío. Dos costes: no se puede cambiar por entorno sin recompilar, y
renombrarlo obliga a redesplegar los dos servicios a la vez. Se descartó un "gestor de topics" con interfaz y
DI: con un solo topic, una clase estática con constantes basta.

### `eEventType` viaja como número

`EventBase.Type` es el enum `eEventType` de `Common`, y el JSON lo lleva como entero (`1`, `2`). El
`EventConverter` del consumidor busca la propiedad `Type` con esa capitalización y como número. Por eso **los
valores del enum no deben cambiarse ni reordenarse**: ya viajan por Kafka. Un evento nuevo recibe el siguiente
número libre.

Detalle y limitaciones en [*Mensajería con Kafka*](mensajeria-kafka.md).
