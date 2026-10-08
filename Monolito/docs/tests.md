# Tests

[← Volver al README](../README.md)

**421 tests unitarios** (más 4 de integración en un proyecto aparte), repartidos en frentes distintos porque cada uno tiene un problema distinto.

**Repositorios (`Infrastructure`).** `DbContextEF` no expone miembros virtuales, así que **no se puede
sustituir con un mock**. Estos tests usan el proveedor InMemory de EF Core, con una base distinta por test y
un patrón de **dos contextos**: se escribe con uno y se lee con otro, de modo que la lectura venga del
almacén y no del `ChangeTracker`. Es la diferencia entre probar que algo persiste y probar que algo se quedó
en memoria. En v2 se prueba además lo que define al patrón: **sin `SaveChanges` no se persiste nada**, y
varios cambios se confirman en un único `SaveChanges`. `CompareInfoInDb` —la detección de duplicados— se prueba
con sus tres casos: mismos datos (`true`), un campo distinto (`false`) y tabla vacía (`false`).

**Casos de uso y handlers (`Application`).** Organizados como el código: `Feature/v1`, `Feature/v2`,
`Feature/v3`, `Feature/v4` y `Feature/Jwt`, y dentro de v3 y v4 un fichero por comando y por query. La decisión que importa es **qué se sustituye y qué no**: se doblan los
*límites* de la capa —repositorios e `IUnitOfWork`, con NSubstitute— pero el mapper y los validadores se
usan **reales**, porque forman parte de lo que se está probando. `ApplicationTestBase` construye el mapper
una sola vez y llama a `AssertConfigurationIsValid()`, que revienta si algún mapa deja propiedades de
destino sin mapear ni marcar como `Ignore()`.

Lo que estos tests demuestran y no se puede demostrar leyendo el código: que **el `SaveChangesAsync` se
llama una sola vez y en el momento correcto**, y que cuando la validación falla, el cliente está duplicado
o no existe no se llama en absoluto.

La comparación v3 → v4 también se ve en los tests: los handlers de v3 tienen un caso de validación y los de
v4 no, porque en v4 la validación ya no vive en el handler sino en `ValidationBehaviour`.

**Validadores (v3 y v4).** Los de `Create` y `Update` recorren cada regla con `[Theory]`: campo vacío,
longitud exactamente en el límite (válida) y un carácter por encima (inválida). En v4, `Delete` y
`GetCustomer` prueban además la regla del `Id`, que en v3 comprobaba el controller a mano. Si alguien cambia
una regla, falla su fila concreta.

**Behaviours del pipeline.** `ValidationBehaviourTests` prueba el behaviour aislado, sin MediatR: con una
petición válida llama a `next()`, con una inválida lanza `ValidationExceptionCustom` **y no llama a
`next()`** —que es la garantía de que el handler no se ejecuta—, y sin validadores registrados deja pasar.
`ValidationBehaviourRegistrationTests` construye el contenedor real y comprueba lo que no se ve en el código:
que una petición de v4 recibe el behaviour y una de v3 no. `LoggingBehaviourTests` fija la clasificación por
`ErrorType`: una respuesta correcta registra `Debug` e `Information`, una fallida registra en el nivel que
corresponde a su `ErrorType` (una fila de `[Theory]` por tipo), y una respuesta que no es `Response<T>` solo
registra `Debug`.

**Productos y asignación a clientes.** La relación N:M `Customer` ↔ `Product` (ver
[*Modelo de datos*](modelo-de-datos.md#caso-de-uso-asignar-un-producto-a-un-cliente)) tiene sus tres frentes:

- **Handlers** (`Feature/Products/Commands/CreateProduct` y `Feature/ProductToCustomer`), con el mismo criterio
  que v3 y v4: se doblan `IUnitOfWork` y los repositorios, y mapper y validadores van reales. Cubren el alta, el
  duplicado (409), el commit sin filas, la excepción, que el `CancellationToken` llegue a cada paso, que
  **cualquier número positivo de filas escritas es éxito** y que `Category` se mapea al `enum`. En la asignación,
  además, que cada entidad se busque por **su propio** `Id`, que un cliente con productos **conserve los
  anteriores**, y que si falla la búsqueda no se guarde nada.
- **Validadores** (`CreateProductValidatorTests`, `ProductToCustomerValidatorTests`): `[Theory]` por regla, con
  los límites de `Name` (100) y `Description` (500) en el límite exacto y un carácter por encima, `Price` y
  `Category` positivos, `StockQuantity` no negativo, y en la asignación que cada `Id` inválido marque solo su
  propio campo.
- **Repositorio** (`ProductRepositoryTest`), con InMemory y dos contextos como el de `Customer`: alta, lectura
  (`GetAllAsync` sin *tracking*), `Update` sobre entidad trackeada y *detached*, `Delete`, que **sin
  `SaveChanges` no se persiste ni se borra nada**, y `CompareInfoInDb` (igual / algún dato distinto).
- **Handlers reales sobre InMemory** (`ProductToCustomerIntegrationTest` y `CreateProductIntegrationTest`):
  handler, repositorios, Unit of Work e interceptor de auditoría reales sobre una base InMemory. Comprueban lo
  que los dobles no pueden: que la fila de `CustomerProducts` se **persiste de verdad**, que un mismo producto
  puede asignarse a dos clientes y un cliente acumular dos productos, que una asignación con cliente o producto
  inexistente no deja ninguna fila, que se rellenan los campos de auditoría, y que dos productos solo son
  duplicados si coinciden todos sus datos (distinto precio, no). **No son** los de `Ecommerce.IntegrationTest`:
  estos corren en la suite unitaria y sin dependencias externas.

**Tests de caracterización.** Algunos tests fijan a propósito un comportamiento **defectuoso** conocido
(sufijo `_DefectoDeSeguridad` o `_PendienteDeCorregir`, y en v3 y v4 `Handle_UpdateCustomer_SaveFail`,
marcado con un comentario). No describen lo deseado: hacen visible la deuda y
garantizan que, al arreglarla, el rojo diga exactamente qué ha cambiado.

**Composition root.** `ConfigureServicesTest` construye el contenedor con
`BuildServiceProvider(validateScopes: true)` y resuelve el grafo completo. Detecta la clase de error que no
rompe la compilación ni la suite, pero sí el arranque — un `AddScoped` olvidado, o una *captive dependency*.

**La caché.** Se prueba sin levantar Redis, porque `IDistributedCache` es una interfaz y se sustituye con
NSubstitute. `CustomerReadRepositoryTest` usa el `DbContext` real con InMemory y la caché doblada, y fija los
cuatro caminos de `GetAllAsync`: acierto (devuelve lo de la caché y **no toca la base de datos ni vuelve a
guardar**), fallo (lee de la base de datos y guarda **una** vez), lo que se guarda (los clientes serializados
y las caducidades de `CustomerAll`) y tabla vacía (colección vacía, no `null`). `CacheConfigurationTests`
cubre la lectura de las caducidades: cada clave (`Default` y `CustomerAll`) lee **solo sus propios valores**, y
un valor ausente, vacío, `"30"` (que `TimeSpan.TryParse` leería como 30 días), con días o con horas fuera de
rango lanza `InvalidOperationException` indicando cuál de las dos caducidades falla y de qué clave.

**Configuración y salud.** `SensitiveDataLoggingTest` comprueba que `EnableSensitiveDataLogging` solo se activa
en `Development`. `HealthCheckCustomeTests` inyecta un `Random` controlado (el constructor pensado para tests)
para fijar el estado que devuelve cada tramo de tiempo de respuesta, sin depender del azar.

**Tests de integración.** `Ecommerce.IntegrationTest` es un proyecto aparte, con 4 tests de autenticación contra
SQL Server real y con `DataProtection` real. La CI **no los ejecuta**: no tiene base de datos ni *user secrets*.

**Lo que falta:** ningún test cruza un controller; `ConfigureServicesTest` no resuelve `ICustomerReadRepository`
ni los handlers de MediatR; y cuando exista la invalidación de la caché (nº 24) faltará el test que compruebe
que se desaloja **después** del commit. Ver pendientes nº 8, 24 y 31.
