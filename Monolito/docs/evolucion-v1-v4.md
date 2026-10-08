# Evolución v1 → v4

[← Volver al README](../README.md)

### v1 → v2: mover el límite transaccional

**v1 es el antipatrón, deliberadamente.** Cada método del repositorio hace su propio `SaveChanges`, así que
el límite transaccional vive en la capa de persistencia. Funciona mientras cada caso de uso toque una sola
entidad — y deja de funcionar en cuanto haya que escribir en dos tablas de forma atómica, porque la primera
escritura ya está confirmada cuando falla la segunda.

**v2 aplica el Unit of Work.** Se ve en las firmas de `IBaseRepositoryUoW`:

```csharp
Task AddAsync(T entity, CancellationToken cancellationToken = default);  // Task, no Task<bool>
void Update(T entity);                                                    // void: no confirma
void Delete(T entity);                                                    // recibe la ENTIDAD, no el id
```

Los tres detalles importan:

- **`AddAsync` devuelve `Task`** y no `Task<bool>`: el repositorio no puede informar del resultado porque
  todavía no ha pasado nada. Solo ha registrado la intención en el `ChangeTracker`.
- **`Update` y `Delete` son `void`** por lo mismo. Si devolvieran algo, ese algo sería mentira.
- **`Delete` recibe la entidad y no el id.** Comprobar si existe es una decisión del caso de uso, no del
  repositorio, y así queda separado el *"no existe"* (404) del *"no se pudo borrar"* (500). Con la firma
  de v1 (`DeleteAsync(int id)` devolviendo `bool`) ese `bool` significaba las dos cosas a la vez.

| | v1 | v2 |
|---|---|---|
| Caso de uso | `CustomerApplication` | `CustomerApplicationUoW` |
| Dependencia | `ICustomerRepository` directo | `IUnitOfWork` |
| `SaveChangesAsync` por petición | uno por operación de repositorio | exactamente uno |

### v2 → v3: separar comandos y consultas

v3 conserva el Unit of Work para escribir y cambia **cómo se organiza y se despacha** el caso de uso:

- **El controller solo conoce `IMediator`.** Construye un command o una query y lo envía; no depende de
  ningún servicio de aplicación concreto. Añadir una operación no obliga a tocar su constructor.
- **Escritura y lectura van por caminos distintos.** Los commands (`Create`, `Update`, `Delete`) usan
  `IUnitOfWork`; las queries (`GetAll`, `GetById`) usan `ICustomerReadRepository`, un contrato de solo
  lectura (`IBaseReadQueriesRepository<T>`) que no expone ningún método de escritura. Una query no puede
  modificar estado aunque quiera: no tiene con qué.
- **Un validador por command.** `CreateCustomerValidator` y `UpdateCustomerValidator` validan exactamente
  la forma de su operación — `UpdateCustomerCommand` exige además `Id > 0`.
- **El `CancellationToken` recorre toda la cadena** en v3: controller → `IMediator.Send` → handler →
  repositorio. Si el cliente aborta la petición, la consulta a base de datos se cancela.

Dos diferencias de contrato respecto a v2 **son decisiones, no desalineaciones**:

- La creación se expone como `POST Create` en lugar de `AddAsync`.
- La actualización existe solo como `POST UpdateAsyncPost`, **con el `Id` dentro del command** en el body
  y no en la ruta: el command lleva todo lo que la operación necesita y el body *es* el command.

> **Nota sobre nombres.** El sufijo `UoW` (`ICustomerApplicationUoW`, `CustomerRepositoryUoW`,
> `_customersUoW`) existe solo para que las versiones convivan en el ejemplo. Un tipo no debe nombrarse por
> el patrón que usa por dentro.

Para el caso contrario —cuando el contrato **no** cambia— no hace falta duplicar nada. Basta con declarar
varias versiones sobre la misma clase, como hace el controller de autenticación:

```csharp
// Controllers/UserAuthController.cs — una clase, varias versiones
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0", Deprecated = true)]
[ApiVersion("3.0")]
public class UserAuthController : ControllerBase
```

### v3 → v4: la validación sale del handler

En v3 cada handler de escritura recibe un `IValidator<TCommand>`, valida al principio y, si falla, devuelve
`Response.Invalid(...)`. Son las mismas tres líneas en cada handler, y la comprobación de `id <= 0` de las
lecturas vive además en el controller. v4 saca todo eso al pipeline de MediatR:

- **`ValidationBehaviour<TRequest, TResponse>`** resuelve todos los `IValidator<TRequest>` registrados, los
  ejecuta y, si hay errores, **lanza `ValidationExceptionCustom` sin llamar a `next()`**: el handler no llega
  a ejecutarse.
- **Los handlers de v4 no tienen `IValidator`** en el constructor. Reciben una petición que ya es válida.
- **`GlobalExceptionHandler` captura esa excepción** en un `catch` específico, antes del genérico, y
  responde **400** con la lista de errores.
- **El controller ya no comprueba nada**: el `id > 0` de `GetById` y `Delete` pasa a `GetCustomerValidator`
  y `DeleteCustomerValidator`, y su `ToActionResult` no tiene rama de `Validation`.

**Por qué una marca y no todas las peticiones.** El behaviour está restringido con
`where TRequest : IValidatableRequest`, una interfaz vacía que solo implementan los commands y queries de v4.
Sin ella se aplicaría también a v3, validaría antes que su handler y lanzaría la excepción: v3 dejaría de
responder su `Response.Invalid` y cambiaría su contrato sin que nadie tocara v3. El contenedor de dependencias
omite un behaviour genérico cuando la petición no cumple la restricción, así que la marca basta para aislar
las dos versiones. `ValidationBehaviourRegistrationTests` lo fija: una petición de v4 incluye el behaviour y
una de v3 no.

**El orden importa.** Los behaviours se ejecutan en el orden en que se registran, del más externo al más
interno: `LoggingBehaviour` envuelve a `ValidationBehaviour`, y este al handler.

**La contrapartida, y es deliberada.** v4 usa una excepción para un fallo *esperado*, justo lo que la
sección [*`Response<T>` para los fallos esperados*](decisiones-tecnicas.md#responset-para-los-fallos-esperados-middleware-para-los-inesperados)
desaconseja. Es el precio de sacar la validación del handler sin resolver el problema de fondo: dentro del
behaviour `TResponse` es genérico y no hay forma directa de construir un `Response<X>.Invalid(...)` sin
conocer `X`. La excepción lo esquiva, a cambio de tres cosas: el flujo de control deja de leerse en la firma,
`Application` depende de que exista un middleware que la traduzca, y el fallo de validación ya no llega a
`LoggingBehaviour` como `Response` (ver [*Quién escribe los logs*](decisiones-tecnicas.md#quién-escribe-los-logs-interceptor-o-call-site)).
v3 y v4 conviven precisamente para poder comparar las dos formas.

La alternativa sin excepción existe y queda anotada: un miembro estático abstracto en una interfaz
(C# 11) que `Response<T>` implemente, de modo que el behaviour, con
`where TResponse : IInvalidResponse<TResponse>`, pueda llamar a `TResponse.Invalid(errors)` y devolverlo.
