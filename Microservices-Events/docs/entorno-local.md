# Entorno local

[← Volver al README](../README.md)

Cómo levantar el proyecto y probar el único endpoint que existe hoy.

## Requisitos

- .NET SDK 10
- Docker (Docker Desktop en Windows)

## 1. MongoDB

Desde la raíz del repo:

```bash
docker compose -f Microservices-Events/Docker/docker-compose.yml up -d
```

Crea el contenedor `mongodb` en el puerto `27017`, dentro de la red `microserviceNetwork`, con dos volúmenes
con nombre (`mongodb_data`, `mongodb_config`) para que los datos sobrevivan a un reinicio.

La primera vez, el contenedor tarda unos segundos en pasar a *healthy*: es el *healthcheck* inicializando el
replica set `rs0` (ver [*Decisiones técnicas*](decisiones-tecnicas.md#mongodb-como-replica-set-de-un-solo-nodo)).
Hasta entonces las transacciones fallarán.

Comprobación:

```bash
docker ps --filter name=mongodb          # STATUS debe ser "healthy"
docker exec mongodb mongosh --quiet --eval "rs.status().ok"   # 1
```

**Para parar sin perder los contenedores**, usa `docker compose -f ... stop`. `down` los elimina.

## 2. Configuración de la API

[`appsettings.json`](../Ticketing/Ticketing.Command/Ticketing.Command.Api/appsettings.json) ya apunta al
compose:

| Clave | Valor | Uso |
|---|---|---|
| `ConnectionStrings:MongoDbConnection` | `mongodb://localhost:27017/?replicaSet=rs0` | Conexión al replica set |
| `MongoSettings:Database` | `ticketingApp` | Base de datos donde vive `eventStore` |

Si falta cualquiera de las dos, `AddInfrastructureServices` lanza `InvalidOperationException` al arrancar
(falla rápido, no en la primera petición).

> La cadena de conexión no lleva credenciales porque MongoDB del compose arranca sin autenticación. Es solo
> para desarrollo local; si algún día se añade usuario, va a *User Secrets*, no a `appsettings.json`.

## 3. Ejecutar

```bash
dotnet run --project Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.Api
```

Swagger (solo en `Development`): `http://localhost:5166/swagger`.

## 4. Probar el endpoint

```http
POST http://localhost:5166/api/v1/TicketCreate/CreateTicket
Content-Type: application/json

{
  "userName": "ruben",
  "errorType": "Timeout",
  "errorDetail": "La pasarela de pago no respondió"
}
```

Respuesta esperada (200):

```json
{ "data": true, "isSuccess": true, "message": "", "errors": {}, "errorType": 1 }
```

Y el documento en Mongo:

```bash
docker exec mongodb mongosh --quiet ticketingApp --eval "db.eventStore.find().pretty()"
```

## Tests y CI

```bash
dotnet test Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.UnitTest
```

`ci-micro-events.yaml` se dispara solo si cambia algo dentro de `Microservices-Events/` (o el propio
workflow), en `push` y `pull_request` hacia `dev` y `main`: restaura, compila `Ticketing.slnx` en `Release`
y lanza ese proyecto de tests.
