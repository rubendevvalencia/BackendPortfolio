# Patrones y decisiones de arquitectura (guion de entrevista)

[← Volver al README](../README.md)

Resumen para explicar el proyecto en voz alta. El detalle y los matices están en
[decisiones técnicas](decisiones-tecnicas.md), [evolución v1 → v4](evolucion-v1-v4.md) y
[limitaciones](limitaciones.md); aquí va lo que se cuenta en dos minutos y las preguntas que suelen venir después.

## El discurso de 30 segundos

> Es un monorepo en migración de monolito a microservicios con el patrón *Strangler Fig*. El monolito es una API
> de ecommerce en .NET con Clean Architecture, y lo usé como laboratorio: el mismo recurso, `Customer`, está
> implementado en **cuatro versiones que conviven**, cada una aplicando una decisión distinta —de un repositorio
> que confirma por su cuenta (v1) a Unit of Work (v2), MediatR con separación comandos/consultas (v3) y validación
> en el pipeline (v4)—. Así puedo comparar qué gana y qué cuesta cada cambio, no solo afirmarlo.

## 1. Arquitectura general

| Decisión | Qué hay | Por qué |
|---|---|---|
| **Clean Architecture** en capas | `Domain` ← `Application` ← `Infrastructure` / `Api`; `Transversal` para lo compartido | Las dependencias apuntan hacia dentro. `Application` referencia solo `Domain` y `Transversal`; `Infrastructure` solo `Domain`. Los `.csproj` lo demuestran |
| **Regla: ningún almacén por encima de `Infrastructure`** | EF Core, Redis y las sondas de health check viven en `Infrastructure`; `Api` no referencia paquetes de SQL ni Redis | Quien abre la conexión, la vigila. El registro de health checks bajó a `Infrastructure`; solo las rutas y el HTML se quedan en `Api` |
| **Monorepo + Strangler Fig** | `Monolito/` sigue vivo; cada pieza extraída nace en `Microservicios/` (primero `Identity`) | Sin reescritura de golpe; el sistema viejo sigue funcionando mientras el nuevo crece |
| **CI por carpeta** | Un workflow por proyecto con filtro `paths`, más `secret-scan` siempre activo | El filtro es propiedad del workflow, no del job; por eso son ficheros separados. Permisos mínimos (`contents: read`) |
| **Contenedores** | Dockerfile multi-stage, usuario sin privilegios, secretos montados como fichero, SQL Server con health check | La capa de `restore` se cachea aparte; el SDK no viaja a producción |

## 2. Patrones de diseño

### Repository + Unit of Work (v1 → v2)
- **v1 es el antipatrón a propósito**: cada método del repositorio hace su `SaveChanges`, así que el límite
  transaccional está en persistencia.
- **v2 mueve ese límite al caso de uso**, que es quien sabe qué cambios forman una unidad indivisible.
- Lo que lo hace posible es que `DbContext` sea **Scoped**: `UnitOfWork` y todos los repositorios comparten la
  misma instancia durante la petición, y un único `SaveChangesAsync` confirma todo en una transacción.
- Consecuencia visible en las firmas: `AddAsync` → `Task`, `Update`/`Delete` → `void`. **Si el repositorio no
  confirma, tampoco puede informar del resultado.** `Delete` recibe la entidad, no el id: comprobar existencia
  es decisión del caso de uso, y así se separa el 404 del 500.

### Mediator + CQRS ligero (v3)
- El controller solo conoce `IMediator`; añadir una operación no toca su constructor.
- Commands escriben por `IUnitOfWork`; queries leen por `ICustomerReadRepository`, un contrato **sin métodos de
  escritura** y con `AsNoTracking()`. El compilador impide que una query modifique estado.
- Un validador por command; el `CancellationToken` recorre toda la cadena.

### Pipeline / Chain of Responsibility (v4)
- `ValidationBehaviour` y `LoggingBehaviour` son `IPipelineBehavior` de MediatR: el handler recibe una petición
  ya válida y no sabe que hay logging.
- **Marker interface** (`IValidatableRequest`): el behaviour se restringe con `where TRequest : IValidatableRequest`
  para que v3 no cambie de contrato sin que nadie la toque. Un test lo fija.
- **El orden importa**: Logging envuelve a Validation, y este al handler.

### Result pattern (`Response<T>`)
- Los fallos **esperados** (no existe, duplicado, validación) viajan en el valor de retorno; los **inesperados**
  suben como excepción hasta un `GlobalExceptionHandler`.
- `Application` devuelve un `ErrorType` (enum), no un status HTTP: la capa de aplicación no sabe qué es un 404.
  Mañana podría exponerse por gRPC o una cola sin tocarla.
- Dividendo no buscado: `LoggingBehaviour` lee el `ErrorType` a través de `IResponse` (cara no genérica de
  `Response<T>`) y elige el nivel del log.

### Interceptor / Decorator sobre `SaveChanges`
- `AuditableEntitySaveChangesInterceptor` rellena `CreatedAt/By` y `LastUpdatedAt/By` recorriendo el
  `ChangeTracker`: un único punto para una preocupación transversal, sin repetirla en cada repositorio.
- Es también el sitio previsto para invalidar la caché **después** del commit.

### Cache-aside (Redis)
- Solo sobre `GetAll`, y montado como **ejercicio del patrón, no como optimización medida**. Se dice así en la
  entrevista.
- Vive en `Infrastructure`: Redis es un almacén y no asoma por encima de esa capa. El repositorio de escritura
  no puede invalidar porque aún no ha confirmado → la invalidación va en un interceptor tras el commit.

### Options / configuración validada
- Rate limiter con dos políticas (`user-limited`, `auth-limited` sin cola) particionadas por IP.
- Ventanas como `"00:00:30"` con `TryParseExact`, porque `TimeSpan` interpreta un `30` suelto como días.
- **Configuración inválida ⇒ el arranque falla**, nombrando la clave.

### Otros
- **Dependency Injection** con `IServiceCollection` extension methods por capa (`AddXxxServices`) y un test que
  construye el contenedor con `validateScopes: true` para cazar dependencias cautivas.
- **Mapeo dual**: AutoMapper para el alta y mapeo manual en la actualización, porque copiar sobre la entidad
  ya cargada mantiene el tracking y EF genera un `UPDATE` solo con las columnas cambiadas.
- **Middleware pipeline** con el orden razonado: excepciones primero, `ForwardedHeaders`, Serilog antes de la
  autenticación (registra también los 401), CORS antes de la autorización (preflight), rate limiter antes de
  validar el JWT.

## 3. Seguridad

- **JWT**: `Issuer`/`Audience`/clave en la misma sección de configuración para que emisor y validador no se
  desincronicen; `ClockSkew = 0`; solo la clave sale del control de versiones.
- **Contraseñas** con `IPasswordHasher<T>` (PBKDF2 + salt), sin criptografía propia.
- **`SignIn` no permite enumerar usuarios**: mismo mensaje y mismo 401 exista o no el email.
- **El middleware de excepciones responde un texto fijo**; el detalle solo va al log.
- **`EnableSensitiveDataLogging` solo en Development.**
- **No se registra el payload** en el logging: un `SignUpDto` lleva la contraseña. El riesgo está documentado y la
  solución prevista es marcar qué campos no se escriben nunca, no solo bajar el nivel.
- **Escaneo de secretos** con gitleaks en CI.

## 4. Testing

- Dobles en los **límites** de cada capa (NSubstitute); mapper y validadores **reales**, porque son parte de lo
  probado. El mapper se valida con `AssertConfigurationIsValid()`.
- Repositorios con EF InMemory y patrón de **dos contextos** (escribir con uno, leer con otro) para probar que se
  persistió y no que quedó en el `ChangeTracker`.
- Se prueba lo que no se ve leyendo el código: `SaveChangesAsync` **una sola vez**, y **ninguna** si falla la
  validación o el cliente está duplicado.
- **Tests de caracterización**: fijan a propósito comportamientos defectuosos conocidos, para que al arreglarlos
  el rojo diga exactamente qué cambió.
- Un test de integración contra SQL Server real; la suite unitaria corre en CI sin dependencias externas.

## 5. Decisiones que conviene defender (y su contrapartida)

| Decisión | Contrapartida que reconozco |
|---|---|
| v4 valida lanzando una excepción | Usa una excepción para un fallo esperado, justo lo que el resto del diseño evita. Alternativa anotada: miembro estático abstracto (C# 11) para construir `TResponse.Invalid(...)` sin excepción |
| Caché en `Infrastructure`, no tras un puerto `ICacheService` en `Application` | Leyendo un handler no hay ninguna pista de que exista caché |
| Cuatro versiones conviviendo | Nombres con sufijo `UoW` solo por convivencia; un tipo no debería nombrarse por el patrón interno |
| Actualización solo por `POST` con el `Id` en el body (v3/v4) | Decisión de que el body *es* el command; no es una desalineación |
| `LoggingBehaviour` solo cubre lo que pasa por MediatR | v1/v2 y auth necesitan logging manual (`IApiLogger`, aún sin cablear) |

## 6. Deuda conocida (mejor contarla antes de que la encuentren)

Está priorizada en [limitaciones](limitaciones.md). Las que más sentido tiene mencionar:

- **La caché no se invalida nunca** (nº 24): es el fallo funcional más grave. Solución diseñada: interceptor
  `SavedChangesAsync`, cacheando un modelo propio con clave versionada.
- **Dominio anémico** (nº 12): faltan invariantes en la entidad (factory methods, value objects).
- **Generar JWT y hashear contraseñas son infraestructura** pero viven en `Application` (nº 13): deben ser puertos.
- **Sin paginación en `GetAll`** (nº 18), que además es el endpoint cacheado: paginar va antes que afinar la caché.
- **La frontera HTTP no tiene tests de integración completos** (nº 8).
- **Actualizar con los mismos datos devuelve 500** (nº 4): `SaveChangesAsync` = 0 no es un fallo.

## 7. Hacia dónde va (microservicios)

Plan por fases: cerrar v0 → monolito modular (Identity/Sales, un esquema por módulo) → DDD táctico → JWT de HMAC
a **RS256 + JWKS** → extraer `Identity` con BD propia → refresh tokens/outbox solo si hacen falta. Motivo real de
extraer Identity: **reutilizarlo en otro proyecto**, no separar por separar.

Acoplamientos detectados **antes** de separar:

- La `Jwt:Key` simétrica está compartida entre emisor y validación: al separar hay que pasar a clave asimétrica.
- `IUnitOfWork` agrupa repositorios de `Customer` y `User` en la misma transacción física: hay que partirlo.
- Un único `DbContext` con ambas entidades: hoy no hay FK entre ellas, así que separar el esquema es limpio, pero
  se pierde la atomicidad "gratis" entre ambos.

## 8. Preguntas probables y respuesta corta

- **¿Por qué cuatro versiones del mismo recurso?** Para comparar decisiones sobre el mismo problema. Cada versión
  cambia una cosa y los tests fijan qué cambia.
- **¿Por qué Unit of Work si EF ya lo es?** Porque el valor no es "agrupar repositorios", sino decidir *quién*
  marca el límite transaccional: el caso de uso, no la persistencia.
- **¿Por qué `Response<T>` y no excepciones?** Un cliente que no existe no es excepcional. Modelarlo con
  excepciones esconde el flujo y obliga a `try/catch` en cada llamador.
- **¿Por qué MediatR? ¿Es CQRS?** MediatR es el mecanismo; aquí solo separa lectura y escritura en contratos y
  caminos distintos. No hay modelos de lectura/escritura separados ni bases distintas.
- **¿Por qué cacheas solo `GetAll`?** `GetById` va por clave primaria y el salto a Redis puede costar más que el
  seek. Y `GetAll` sin paginar es el síntoma: se pagina primero y luego se replantea la caché.
- **¿Cómo evitas fugar datos sensibles en logs?** No se vuelca el payload; solo se registra el borde (nivel según
  `ErrorType`) y lo que el status no dice ya. Emails en logs persistidos es una deuda anotada (nº 19).
- **¿Qué cambiarías primero?** Invalidación de caché tras el commit, paginación, y mover JWT/hash a puertos en
  `Infrastructure`.
