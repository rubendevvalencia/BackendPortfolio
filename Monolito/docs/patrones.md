# Patrones usados

[← Volver al README](../README.md)

Mapa de los patrones que hay en el código, qué hace cada uno y dónde verlo. Los matices de por qué se
eligieron están en [*Decisiones técnicas*](decisiones-tecnicas.md) y la comparación entre versiones en
[*Evolución v1 → v4*](evolucion-v1-v4.md).

## Fluent API

| Uso | Qué hace | Dónde |
|---|---|---|
| **EF Core Fluent API** | Define el mapeo de cada entidad a la base de datos (tabla, clave, longitudes, índices únicos) sin ensuciar el dominio con atributos. | [`CustomerConfiguration`](../Ecommerce.Infrastructure/Persistence/CustomerConfiguration.cs) y [`UserConfiguration`](../Ecommerce.Infrastructure/Persistence/Jwt/UserConfiguration.cs), descubiertas con `ApplyConfigurationsFromAssembly` en [`DbContextEF`](../Ecommerce.Infrastructure/Data/DbContextEF.cs) |
| **FluentValidation** | Declara las reglas de validación de cada DTO o command con `RuleFor(...)` en una clase aparte. | [`SignUpValidator`](../Ecommerce.Application/Validator/Jwt/SignUpValidator.cs) y los `*Validator` de v3 y v4, registrados con `AddValidatorsFromAssembly` en [`ConfigureServices`](../Ecommerce.Application/ConfigureServices.cs) |
| **Interfaz fluida / builder** | Encadena llamadas sobre el mismo objeto para configurarlo en una sola expresión legible. | `DistributedCacheEntryOptions` en [`CustomerReadRepository`](../Ecommerce.Infrastructure/Repository/Customer/CustomerReadRepository.cs); `AddHealthChecks()...` en [`ConfigureServices`](../Ecommerce.Infrastructure/ConfigureServices.cs) |

El mapeo de `User` tiene un detalle: `FirstName` y `LastName` admiten 500 caracteres porque `Protect()` de
Data Protection añade cabecera, IV y firma y lo pasa a base64. El mapeo está dimensionado para el cifrado.

## Diseño y arquitectura

| Patrón | Qué hace | Dónde |
|---|---|---|
| **Repository** | Oculta el acceso a datos tras un contrato para que la lógica de negocio no conozca EF. | `ICustomerRepository`, `ICustomerRepositoryUoW` e `IUserRepository` en `Domain`; implementaciones en [`Infrastructure/Repository`](../Ecommerce.Infrastructure/Repository) |
| **Unit of Work** | Agrupa los cambios de varios repositorios y los confirma juntos con un solo `SaveChangesAsync`. | [`UnitOfWork`](../Ecommerce.Infrastructure/Repository/UnitOfWork.cs), [`IBaseRepositoryUoW`](../Ecommerce.Domain/Interface/IRepository/UnitOfWork/IBaseRepositoryUoW.cs) |
| **CQRS** | Separa las operaciones que escriben (commands) de las que leen (queries), con caminos y contratos distintos. | `Commands/` y `Queries/` en v3 y v4; lectura por [`CustomerReadRepository`](../Ecommerce.Infrastructure/Repository/Customer/CustomerReadRepository.cs) |
| **Mediator** (MediatR) | El controller envía una petición a un intermediario y este localiza el handler, sin acoplarse a él. | [`CustomerController` v3](../Ecommerce/Controllers/Customer/v3/CustomerController.cs) y [v4](../Ecommerce/Controllers/Customer/v4/CustomerController.cs) |
| **Pipeline / Chain of Responsibility** (+ Decorator) | Envuelve cada handler con pasos reutilizables (log, validación) que se ejecutan en orden antes y después. | [`LoggingBehaviour`](../Ecommerce.Application/Common/Behaviours/LoggingBehaviour.cs) y [`ValidationBehaviour`](../Ecommerce.Application/Common/Behaviours/ValidationBehaviour.cs), registrados en [`ConfigureServices`](../Ecommerce.Application/ConfigureServices.cs) |
| **Marker interface** | Etiqueta ciertos tipos sin métodos para decidir qué reglas se les aplican. | [`IValidatableRequest`](../Ecommerce.Application/Common/Interface/IValidatableRequest.cs): solo v4 pasa por `ValidationBehaviour` |
| **Result pattern** | Devuelve el éxito o el fallo esperado como valor de retorno en vez de lanzar excepciones. | [`Response<T>`](../Ecommerce.Transversal/Common/Response.cs) con `ErrorType` |
| **Static Factory Method** | Crea objetos ya bien formados con métodos con nombre en vez de constructores. | `Success`, `Fail`, `NotFound` e `Invalid` en `Response<T>` |
| **Factory (design-time)** | Fabrica el `DbContext` fuera de la aplicación para que las herramientas de EF puedan usarlo. | [`DbContextEFDesignTimeFactory`](../Ecommerce.Infrastructure/Data/DbContextEFDesignTimeFactory.cs) |
| **Interceptor** | Se engancha al guardado de EF para rellenar la auditoría de todas las entidades en un único punto. | [`AuditableEntitySaveChangesInterceptor`](../Ecommerce.Infrastructure/Interceptors/AuditableEntitySaveChangesInterceptor.cs) |
| **Middleware** | Procesa cada petición HTTP en una cadena ordenada (errores, CORS, rate limit, timeout, auth). | [`GlobalExceptionHandler`](../Ecommerce/Modules/GlobalException/GlobalExceptionHandler.cs), [`Program.cs`](../Ecommerce/Program.cs) |
| **Ports & Adapters / DIP** | El dominio declara lo que necesita (puerto) y la capa externa lo implementa (adaptador). | `ICurrentUser` en `Domain`; [`CurrentUser`](../Ecommerce/Modules/Services/CurrentUser/CurrentUser.cs) en `Api`, la única capa que conoce `HttpContext` |
| **Dependency Injection / IoC** | El contenedor construye y entrega las dependencias, con un ciclo de vida definido (Scoped). | Un `ConfigureServices` por capa |
| **Extension methods como composition root** | Cada módulo se registra con una llamada `AddXxx` y `Program.cs` queda como un índice legible. | `AddAuth`, `AddRateLimiting`, `AddTimeOut`, `AddVersioning` y `AddSwagger` en [`Modules/`](../Ecommerce/Modules) |
| **Cache-aside** | Mira primero la caché y, si no está, consulta la base de datos y guarda el resultado. | [`CustomerReadRepository.GetAllAsync`](../Ecommerce.Infrastructure/Repository/Customer/CustomerReadRepository.cs) con Redis |
| **Options / Configuration pattern** | Convierte la configuración de `appsettings` en objetos tipados (sin validación al arrancar todavía). | `RateLimiterSettings`, `CacheConfiguration` y `eCacheKey` |
| **DTO + Mapper** | Separa lo que viaja por la API de las entidades y traduce entre ambos. | [`Dto/`](../Ecommerce.Application/Dto), `MappingProfile` (AutoMapper) y [`ManualMappingCustomer.MapInto`](../Ecommerce.Application/Mapping/ManualMappingCustomer.cs) |
| **Template / base class** | Una clase base concentra lo común de los controllers. | [`ApiResponseControllerBase`](../Ecommerce/Controllers/ApiResponseController.cs) |
| **Strategy (política configurable)** | Permite elegir qué regla se aplica a cada endpoint con solo un atributo. | Rate limiting `user-limited` y `auth-limited`; timeouts `DefaultPolicy` y `CustomPolicy` |
| **API Versioning por URL** | Mantiene varias versiones del contrato a la vez, visibles en la ruta (`api/v4/...`). | `Asp.Versioning` con un Swagger por versión; ver [*Versionado*](versionado-api.md) |
| **Health Check** | Expone el estado de la app y de sus dependencias (SQL, Redis) para monitorización. | [`HealthCheckCustome`](../Ecommerce.Infrastructure/HealthCheck/HealthCheckCustome.cs), `/health` y `/health/ui` |
| **Data Protection** | Cifra datos personales del usuario y guarda las contraseñas como hash con salt. | `IDataProtector` en `SignUp/SignInCommandHandle`; `IPasswordHasher<User>` en [`UserRepository`](../Ecommerce.Infrastructure/Repository/Jwt/UserRepository.cs) |
| **Retry / resiliencia** | Reintenta solo ante fallos transitorios de la base de datos. | `EnableRetryOnFailure()` en [`ConfigureServices`](../Ecommerce.Infrastructure/ConfigureServices.cs) |
| **Herencia para auditoría** | Las entidades heredan los campos de auditoría de una clase base común. | [`BaseAuditEntity`](../Ecommerce.Domain/Entities/Audit/BaseAuditEntity.cs) |

## Testing

| Patrón | Qué hace |
|---|---|
| **Dobles en los límites** (NSubstitute) | Sustituye repositorios y UoW para probar el caso de uso aislado, con mapper y validadores reales. |
| **Dos contextos** (EF InMemory) | Escribe con un contexto y lee con otro para comprobar que el dato persistió de verdad. |
| **Tests de caracterización** | Fijan un bug conocido para que, al corregirlo, el test diga qué cambió. |
| **Test del composition root** | Resuelve todo el grafo de DI con `validateScopes: true` para cazar dependencias mal registradas. |
| **Costura para el test** | Un constructor alternativo (o un parámetro como `IHostEnvironment`) permite controlar lo no determinista sin tocar el comportamiento en producción. | `HealthCheckCustome(Random)` y `AddInfrastructureServices(configuration, environment)` |
| **Factorías de datos** | `NewCustomerDto` y `NewSignUpDto` en la clase base evitan repetir la construcción de datos de prueba. |
| **Tests de integración** | [`Ecommerce.IntegrationTest`](../Ecommerce.IntegrationTest) usa el contenedor de DI real para recorrer el flujo SignUp → SignIn. |

Detalle en [*Tests*](tests.md).

## CI y despliegue

| Patrón | Qué hace |
|---|---|
| **Workflows por ruta** | Cada parte del monorepo solo dispara su propia CI cuando cambia. |
| **Secret scanning** (gitleaks) | Impide que entre una credencial nueva en el repositorio. |
| **Permisos mínimos** | El `GITHUB_TOKEN` nace de solo lectura y solo se eleva donde hace falta. |
| **Secrets montados en Docker** | `Program.cs` carga `/run/secrets/secrets.json`, y las credenciales quedan fuera de la imagen y del repo. |

Detalle en [*Integración continua*](integracion-continua.md).

## Lo que no es (todavía)

- **No es DDD.** El dominio es anémico: sin factorías, objetos de valor ni invariantes en la entidad
  (pendiente nº 12 de [*Limitaciones*](limitaciones.md)).
- **El repositorio genérico tiene un contrato concreto por entidad encima.** `IBaseRepository<T>` e
  `IBaseRepositoryUoW<T>` existen, pero cada entidad tiene el suyo.
- **v4 valida con una excepción**, en contra del *Result pattern* del resto. Es un compromiso asumido:
  ver [*v3 → v4*](evolucion-v1-v4.md#v3--v4-la-validación-sale-del-handler).
