# Tests

[← Volver al README](../README.md)

**291 tests**, repartidos en frentes distintos porque cada uno tiene un problema distinto.

**Repositorios (`Infrastructure`).** `DbContextEF` no expone miembros virtuales, así que **no se puede
sustituir con un mock**. Estos tests usan el proveedor InMemory de EF Core, con una base distinta por test y
un patrón de **dos contextos**: se escribe con uno y se lee con otro, de modo que la lectura venga del
almacén y no del `ChangeTracker`. Es la diferencia entre probar que algo persiste y probar que algo se quedó
en memoria. En v2 se prueba además lo que define al patrón: **sin `SaveChanges` no se persiste nada**, y
varios cambios se confirman en un único `SaveChanges`.

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
que una petición de v4 recibe el behaviour y una de v3 no. `LoggingBehaviour` todavía no tiene tests.

**Tests de caracterización.** Algunos tests fijan a propósito un comportamiento **defectuoso** conocido
(sufijo `_DefectoDeSeguridad` o `_PendienteDeCorregir`, y en v3 y v4 `Handle_UpdateCustomer_SaveFail`,
marcado con un comentario). No describen lo deseado: hacen visible la deuda y
garantizan que, al arreglarla, el rojo diga exactamente qué ha cambiado.

**Composition root.** `ConfigureServicesTest` construye el contenedor con
`BuildServiceProvider(validateScopes: true)` y resuelve el grafo completo. Detecta la clase de error que no
rompe la compilación ni la suite, pero sí el arranque — un `AddScoped` olvidado, o una *captive dependency*.

**Lo que falta:** ningún test cruza un controller, y `LoggingBehaviour` y `CustomerReadRepository` —con
toda la lógica de caché dentro— no tienen tests. Ver pendientes nº 8, 29 y 31.
