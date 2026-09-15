# Backend Portfolio — API Ecommerce

API REST en **.NET 10** construida con **Clean Architecture**, como proyecto de portfolio y aprendizaje
deliberado de backend en C#.

El objetivo no es la cantidad de funcionalidad, sino la **calidad de las decisiones**: por qué cada pieza
está donde está, qué problema resuelve y qué se rompería si estuviera en otro sitio. El código lleva
comentarios que explican el *porqué*, no el *qué* — están puestos a propósito, con fines didácticos, y en
un proyecto de producción tendrían bastante menos densidad.

---

## Cómo leer este repositorio

**El mismo recurso (`Customer`) está implementado tres veces, una por versión de la API.** No es
duplicación accidental: cada versión resuelve el mismo caso de uso con un enfoque distinto, y que convivan
es lo que permite evaluar la evolución completa desde la propia API — mismo recurso, tres formas de decidir
dónde vive la transacción y cómo se separan lecturas y escrituras.

| | **v1** | **v2** | **v3** |
|---|---|---|---|
| Enfoque | Repositorio directo | Unit of Work | CQRS con MediatR |
| Quién confirma (`SaveChanges`) | Cada método del repositorio | El caso de uso, una vez | El handler, una vez |
| Lecturas | Mismo repositorio | Mismo repositorio | Repositorio de lectura separado |
| Controller depende de | `ICustomerApplication` | `ICustomerApplicationUoW` | Solo `IMediator` |
| Detección de duplicados | No | Sí → 409 | Sí → 409 |
| Qué demuestra | **El antipatrón**, a propósito | El límite transaccional en el caso de uso | Separación de comandos y consultas |
| Estado | `Deprecated` | Vigente | Vigente |

Recorrido recomendado: [`Controllers/v1`](Ecommerce/Controllers/v1/CustomerController.cs) →
[`v2`](Ecommerce/Controllers/v2/CustomerController.cs) → [`v3`](Ecommerce/Controllers/v3/CustomerController.cs),
y detrás de cada uno su implementación en
[`Ecommerce.Application/Feature/Customers`](Ecommerce.Application/Feature/Customers). Las secciones
[*Evolución v1 → v2 → v3*](#evolución-v1--v2--v3) y [*Decisiones técnicas*](#decisiones-técnicas) explican
el porqué de cada salto.

---

## Arquitectura

Clean Architecture / Onion, con la regla de dependencia como única norma innegociable: **las dependencias
apuntan siempre hacia dentro**, hacia lo que menos cambia.

```mermaid
flowchart RL
    API["<b>Ecommerce.Api</b><br/>Controllers v1 · v2 · v3<br/>Versionado · Swagger · CORS · JWT<br/>Middleware de excepciones<br/><i>la única capa que conoce HTTP</i>"]
    APP["<b>Ecommerce.Application</b><br/>Casos de uso · Commands/Queries (MediatR)<br/>DTOs · Validadores · Mapeo"]
    DOM["<b>Ecommerce.Domain</b><br/>Entidades<br/>Interfaces de repositorio<br/><i>cero dependencias externas</i>"]
    INF["<b>Ecommerce.Infrastructure</b><br/>EF Core · DbContext · Migraciones<br/>Repositorios de escritura y lectura<br/>Interceptores"]
    TRA["<b>Ecommerce.Transversal</b><br/>Response&lt;T&gt; · ErrorType<br/>Logging (Serilog)"]

    API --> APP
    APP --> DOM
    INF -.->|implementa| DOM
    API -.->|solo para registrar servicios| INF
    APP --> TRA
    API --> TRA
```

La prueba de que la regla se cumple no está en la estructura de carpetas, sino en los `.csproj`:
**`Ecommerce.Domain` no tiene un solo `PackageReference`**. Las interfaces de repositorio viven ahí — en
quien las *usa* — y `Infrastructure` las implementa; ahí está la inversión de dependencias.

| Proyecto | Responsabilidad |
|---|---|
| `Ecommerce.Api` | Traduce HTTP ↔ casos de uso. Versionado, autenticación, Swagger, manejo global de excepciones. Composition root. |
| `Ecommerce.Application` | Orquesta los casos de uso (servicios en v1/v2, handlers de MediatR en v3). No sabe qué es un código HTTP. |
| `Ecommerce.Domain` | Entidades y contratos. No sabe que existe una base de datos. |
| `Ecommerce.Infrastructure` | Persistencia con EF Core. Implementa los contratos del dominio. |
| `Ecommerce.Transversal` | Tipos y servicios compartidos por varias capas (`Response<T>`, `ErrorType`, logging). |
| `Ecommerce.Test` | xUnit + NSubstitute + EF Core InMemory. |

### Organización de `Application`

Por *feature* y, dentro de `Customers`, por versión — para que la comparación se lea en el árbol de carpetas:

```
Feature/
├── Customers/
│   ├── v1/  CustomerApplication.cs          ← repositorio que confirma
│   ├── v2/  CustomerApplicationUoW.cs       ← Unit of Work
│   └── v3/
│       ├── Commands/
│       │   ├── CreateCustomer/   Command · Handler · Validator
│       │   ├── UpdateCustomer/   Command · Handler · Validator
│       │   └── DeleteCustomer/   Command · Handler · Validator
│       └── Queries/
│           ├── GetAllCustomerQuery/   Query · Handler
│           └── GetCustomerQuery/      Query · Handler
├── Users/   UserAuthApplication.cs
└── Jwt/     JwtApplication.cs
```

En v3 cada operación es una carpeta autocontenida: todo lo que hace falta para entender *crear un cliente*
está junto, en lugar de repartido entre un servicio, un DTO compartido y un validador genérico.

---

## Stack

- **.NET 10** · C# · ASP.NET Core Web API
- **Entity Framework Core 10** (Code First, migraciones) sobre **SQL Server**
- **MediatR** — CQRS en la v3 de la API
- **Asp.Versioning** — versionado de la API por segmento de URL
- **JWT Bearer** — autenticación con `Microsoft.AspNetCore.Authentication.JwtBearer`
- **FluentValidation** — validación desacoplada del modelo
- **AutoMapper** — mapeo entidad ↔ DTO
- **Serilog** — logging estructurado a consola, fichero y SQL Server
- **Swashbuckle / OpenAPI** — documentación con anotaciones, un documento por versión
- **xUnit · NSubstitute · Coverlet** — tests y cobertura

---

## Evolución v1 → v2 → v3

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
[ApiVersion("2.0")]
public class UserAuthController : ControllerBase
```

---

## Versionado de la API

La API se versiona **por segmento de URL**: la versión viaja en la propia ruta, `api/v1/...`, `api/v2/...`,
`api/v3/...`.

### Cómo está montado

| Archivo | Papel |
|---|---|
| `Ecommerce/Models/Version/VersionExtensions.cs` | Registra el versionado y el API Explorer |
| `Ecommerce/Models/Swagger/ConfigureSwaggerOptions.cs` | Genera **un documento de Swagger por versión** descubierta |
| `Ecommerce/Program.cs` | Recorre las versiones y publica un endpoint de Swagger UI por cada una |

```csharp
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;                    // cabeceras api-supported-versions / api-deprecated-versions
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader());
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";                  // v1, v2, v3...
    options.SubstituteApiVersionInUrl = true;            // sustituye {version} por el valor real en la doc
});
```

### Por qué segmento de URL y no cabecera

Hay cuatro estrategias posibles y `Asp.Versioning` las soporta todas — las tres alternativas están
comentadas en `VersionExtensions.cs` a propósito, para dejar constancia de que la elección fue consciente:

| Estrategia | Ventaja | Inconveniente |
|---|---|---|
| **Segmento de URL** (elegida) | Visible, cacheable, se prueba desde el navegador o curl sin herramientas | La versión ensucia la URL; ortodoxamente la URI debería identificar el recurso, no su representación |
| Query string (`?api-version=2.0`) | No ensucia la ruta | Fácil de olvidar; problemas de caché |
| Cabecera (`X-Version: 2.0`) | URL limpia, es lo más "correcto" según REST | Invisible; no se puede probar pegando una URL en el navegador |
| Media type (`Accept: ...;ver=2.0`) | El más purista | El más incómodo de consumir y de documentar |

Para un proyecto cuyo objetivo es **enseñar y que se entienda leyéndolo**, la visibilidad gana a la
ortodoxia. En una API interna con clientes generados, la cabecera sería la elección razonable.

### Swagger por versión

`ConfigureSwaggerOptions` implementa `IConfigureOptions<SwaggerGenOptions>` y **recorre
`IApiVersionDescriptionProvider`**, así que crea un documento por cada versión que descubra en los
controllers. No hay ningún `SwaggerDoc("v1", ...)` escrito a mano: **la v3 se añadió sin tocar la
configuración de Swagger**, que es exactamente lo que este diseño prometía.

`Program.cs` hace lo simétrico en la UI: resuelve el provider desde `app.Services` (en lugar de construir
un contenedor nuevo) y publica un `SwaggerEndpoint` por versión, que es lo que produce el desplegable de
versiones arriba a la derecha en `/swagger`.

Las versiones obsoletas lo declaran en el atributo, y eso viaja a dos sitios: a la descripción del
documento de Swagger y a la cabecera `api-deprecated-versions` de cada respuesta.

---

## Decisiones técnicas

Las que tienen algo que explicar. El resto del código intenta ser aburrido a propósito.

### `Response<T>` para los fallos esperados, middleware para los inesperados

Que un cliente no exista, que un DTO no valide o que un cliente ya esté registrado **no es excepcional**:
es un resultado previsto del caso de uso. Modelarlo con excepciones sale caro, esconde el flujo de control
y obliga a un `try/catch` en cada llamador. Aquí el fallo esperado forma parte del valor de retorno.

**`Application` dice *qué* falló, `Api` decide *cómo* se comunica.** Por eso existe un `enum ErrorType` y
no un `int statusCode` — la capa de aplicación no sabe qué es un 404:

| `ErrorType` | HTTP |
|---|---|
| `Validation` | 400 |
| `NotFound` | 404 |
| `Duplicated` | 409 |
| `Unexpected` | 500 |

Si mañana estos casos de uso se expusieran por gRPC o por una cola, `Application` no se toca; solo se
escribe otro traductor.

Lo que **sí** es excepcional —una caída de la base de datos, un timeout— ya no se captura en cada caso de
uso. v1 y v2 dejaron de tener `try/catch`: la excepción sube hasta `GlobalExceptionHandler`, un
`IMiddleware` que la registra con Serilog y devuelve un 500. Los tests lo fijan explícitamente
(`*_PropagaLaExcepcion*`): el caso de uso **no** debe tragarse la excepción. Quedan puntos por cerrar en
este middleware — ver [*Estado actual*](#estado-actual-y-limitaciones-conocidas).

### Unit of Work: quién decide cuándo se confirma

El patrón no sirve para "agrupar repositorios" — eso es una consecuencia, no el objetivo. Sirve para
**mover el límite transaccional desde la capa de persistencia hasta el caso de uso**, que es la única que
sabe qué conjunto de cambios forma una unidad indivisible.

La pieza que lo hace posible no está en el código del `UnitOfWork`, sino en el registro de servicios: el
`DbContext` está registrado como **Scoped**, así que `UnitOfWork` y todos los repositorios comparten la
*misma* instancia durante toda la petición. Ese contexto compartido es lo que permite que un único
`SaveChangesAsync` confirme en bloque los cambios registrados desde varios repositorios, dentro de una sola
transacción.

De ahí sale una regla que atraviesa todo el diseño: **si el repositorio no confirma, tampoco puede
informar del resultado.** Por eso `Update` y `Delete` devuelven `void` y `AddAsync` devuelve `Task`.

Y una consecuencia contraintuitiva: **cuando `SaveChangesAsync` devuelve 0, no ha fallado nada.** Significa
que EF no encontró diferencias que escribir — actualizar un registro con los mismos valores es el caso
típico. Con la existencia comprobada antes por el caso de uso, un 0 es una operación correcta sin efecto.
*(El código actual todavía no lo trata así: es el pendiente nº 4.)*

### Repositorio de lectura separado (v3)

`ICustomerReadRepository` hereda de `IBaseReadQueriesRepository<T>`, que solo declara `GetByIdAsync` y
`GetAllAsync`. Separar el contrato hace que la intención sea verificable por el compilador: los handlers de
consulta reciben una dependencia **sin métodos de escritura**, y el listado se lee con `AsNoTracking()`
porque nadie va a modificar esas entidades — EF se ahorra el coste de rastrearlas.

### Duplicados como resultado de negocio (409)

v2 y v3 comprueban si el cliente ya existe antes de darlo de alta y responden `ErrorType.Duplicated` →
**409 Conflict**, en lugar de dejar que el alta prospere o que acabe en un 500. Es un caso previsto, así
que viaja por `Response<T>` como el resto de fallos esperados. v1 no lo hace, y esa diferencia también
forma parte de la comparación.

### Autenticación con JWT

La firma y la validación comparten la sección `Jwt` de la configuración, para que el `Issuer`, la
`Audience` y la clave no puedan desincronizarse entre quien emite el token y quien lo valida.
`ClockSkew = TimeSpan.Zero` elimina el margen de 5 minutos que .NET añade por defecto a la caducidad.

`Issuer` y `Audience` **no son secretos** y viven en `appsettings.json`: viajan dentro del propio token.
Solo la clave firma, y por eso es la única que sale del control de versiones.

El hash de contraseñas usa `IPasswordHasher<T>` de ASP.NET Core Identity — PBKDF2 con salt por usuario,
sin escribir criptografía a mano.

### Auditoría con un interceptor de `SaveChanges`

`CreatedAt`, `CreatedBy`, `LastUpdatedAt` y `LastUpdatedBy` los rellena un `SaveChangesInterceptor` que
recorre el `ChangeTracker`. Como *todos* los cambios pasan por `SaveChanges`, hay un punto único donde
resolver una preocupación que afecta a todas las entidades — en lugar de repetir las mismas cuatro líneas
en cada repositorio.

### Mapeo manual en la actualización

El alta usa AutoMapper, pero la actualización usa un mapeo escrito a mano (`ManualMappingCustomer.MapInto`),
y es deliberado: copia el DTO o el command **encima** de la entidad ya cargada, que sigue *connected* en el
`ChangeTracker`. EF genera entonces un `UPDATE` solo con las columnas que de verdad cambiaron. Mapear a una
instancia nueva la dejaría *detached* y perdería esa detección.

### CORS fuera del `if (IsDevelopment())`

`UseCors()` se registra siempre, no solo en desarrollo: si estuviera dentro del `if`, en producción las
respuestas saldrían sin la cabecera `Access-Control-Allow-Origin`. El fallo es difícil de diagnosticar
porque **CORS lo aplica el navegador, no el servidor** — la API responde 200, los logs se ven bien y desde
curl o Postman funciona, pero el navegador oculta la respuesta al JavaScript.

Va después de `UseHttpsRedirection()` y antes de `UseAuthorization()`, para que el preflight `OPTIONS` se
responda antes de que nadie exija autenticación. Y va precedido de `UseForwardedHeaders()`, porque cuando el
TLS termina en un balanceador la aplicación recibe HTTP plano y `UseHttpsRedirection()` respondería 307
incluso a los preflight.

### Logging con Serilog

Tres destinos con criterios distintos: consola para desarrollo, fichero rotado a diario (7 días) para el
histórico local, y **SQL Server solo a partir de `Warning`** — la tabla de logs no debe llenarse con el
tráfico normal.

`UseSerilogRequestLogging()` va antes de la autenticación en el pipeline, a propósito: así registra también
los intentos que acaban en 401.

### Arranque que falla de forma visible

Todo `Program.cs` está envuelto en un `try/catch` que escribe en `stderr`, registra con `Log.Fatal` y fija
`Environment.ExitCode = 1`. Sin eso, un fallo de arranque —un puerto ocupado, los User Secrets sin
configurar— acababa en un proceso que salía con código 0 y sin rastro en la consola.

---

## Tests

**126 tests**, repartidos en frentes distintos porque cada uno tiene un problema distinto.

**Repositorios (`Infrastructure`).** `DbContextEF` no expone miembros virtuales, así que **no se puede
sustituir con un mock**. Estos tests usan el proveedor InMemory de EF Core, con una base distinta por test y
un patrón de **dos contextos**: se escribe con uno y se lee con otro, de modo que la lectura venga del
almacén y no del `ChangeTracker`. Es la diferencia entre probar que algo persiste y probar que algo se quedó
en memoria. En v2 se prueba además lo que define al patrón: **sin `SaveChanges` no se persiste nada**, y
varios cambios se confirman en un único `SaveChanges`.

**Casos de uso y handlers (`Application`).** Organizados como el código: `Feature/v1`, `Feature/v2`,
`Feature/v3` y `Feature/Jwt`. La decisión que importa es **qué se sustituye y qué no**: se doblan los
*límites* de la capa —repositorios e `IUnitOfWork`, con NSubstitute— pero el mapper y los validadores se
usan **reales**, porque forman parte de lo que se está probando. `ApplicationTestBase` construye el mapper
una sola vez y llama a `AssertConfigurationIsValid()`, que revienta si algún mapa deja propiedades de
destino sin mapear ni marcar como `Ignore()`.

Lo que estos tests demuestran y no se puede demostrar leyendo el código: que **el `SaveChangesAsync` se
llama una sola vez y en el momento correcto**, y que cuando la validación falla o el cliente está duplicado
no se llama en absoluto.

**Validadores (v3).** `CreateCustomerValidatorTests` recorre cada regla con `[Theory]`: campo vacío,
longitud exactamente en el límite (válida) y un carácter por encima (inválida). Si alguien cambia una regla,
falla su fila concreta.

**Tests de caracterización.** Algunos tests fijan a propósito un comportamiento **defectuoso** conocido
(sufijo `_DefectoDeSeguridad` o `_PendienteDeCorregir`). No describen lo deseado: hacen visible la deuda y
garantizan que, al arreglarla, el rojo diga exactamente qué ha cambiado.

**Composition root.** `ConfigureServicesTest` construye el contenedor con
`BuildServiceProvider(validateScopes: true)` y resuelve el grafo completo. Detecta la clase de error que no
rompe la compilación ni la suite, pero sí el arranque — un `AddScoped` olvidado, o una *captive dependency*.

**Lo que falta:** ningún test cruza un controller, y los handlers de v3 salvo `CreateCustomer` están sin
cubrir. Ver pendientes nº 8 y nº 9.

---

## Puesta en marcha

**Requisitos:** SDK de .NET 10 y una instancia de SQL Server (vale SQL Server Express).

```bash
git clone https://github.com/rubendevvalencia/BackendPortfolio.git
cd BackendPortfolio
```

**1. Configura los secretos.** `appsettings.json` declara las claves pero las deja vacías a propósito: el
archivo define la *forma* de la configuración, no sus valores.

```bash
dotnet user-secrets set "ConnectionStrings:EcommerceDb" \
  "Server=localhost\SQLEXPRESS;Database=Ecommerce;Trusted_Connection=True;TrustServerCertificate=True;" \
  --project Ecommerce

dotnet user-secrets set "Jwt:Key" "<clave aleatoria de 32 bytes o mas>" --project Ecommerce
```

La clave JWT no es opcional: HMAC-SHA256 exige 256 bits, y el límite se mide en bytes, no en caracteres.
En despliegue ambos valores llegan por variables de entorno: `ConnectionStrings__EcommerceDb` y `Jwt__Key`.

**2. Crea la base de datos.**

```bash
dotnet ef database update --project Ecommerce.Infrastructure --startup-project Ecommerce
```

**3. Arranca.**

```bash
dotnet run --project Ecommerce
```

Swagger queda en `https://localhost:7051/swagger`, con el desplegable de versiones arriba a la derecha.

**Tests:**

```bash
dotnet test
```

---

## Endpoints

Todos devuelven un `Response<T>` con el mismo contrato.

### Autenticación — `api/v{1|2}/UserAuth`

Un solo controller sirviendo ambas versiones. Los dos endpoints son `[AllowAnonymous]`; el resto de la API
exige un JWT válido en la cabecera `Authorization: Bearer <token>`.

| Verbo | Ruta | Descripción |
|---|---|---|
| `POST` | `/SignUp` | Registra un usuario |
| `POST` | `/SignIn` | Devuelve un token de acceso |

### Clientes v1 y v2 — `api/v{1|2}/Customer`

Mismos endpoints; lo que cambia es la implementación detrás.

| Verbo | Ruta | Descripción |
|---|---|---|
| `GET` | `/GetAllAsync` | Lista todos los clientes |
| `GET` | `/GetByIdAsync/{id}` | Recupera un cliente |
| `POST` | `/AddAsync` | Crea un cliente (v2: 409 si ya existe) |
| `PUT` | `/UpdateAsync{id}` | Actualiza un cliente |
| `POST` | `/UpdateAsyncPost/{id}` | Igual que el anterior, para clientes que no admiten `PUT` |
| `DELETE` | `/DeleteAsync/{id}` | Elimina un cliente |

### Clientes v3 (CQRS) — `api/v3/Customer`

| Verbo | Ruta | Mensaje MediatR | Descripción |
|---|---|---|---|
| `GET` | `/GetAllAsync` | `GetAllCustomerQuery` | Lista todos los clientes |
| `GET` | `/GetByIdAsync/{id}` | `GetCustomerQuery` | Recupera un cliente |
| `POST` | `/Create` | `CreateCustomerCommand` | Crea un cliente (409 si ya existe) |
| `POST` | `/UpdateAsyncPost` | `UpdateCustomerCommand` | Actualiza un cliente; el `Id` va en el body |
| `DELETE` | `/DeleteAsync/{id}` | `DeleteCustomerCommand` | Elimina un cliente |

> Las rutas llevan el verbo dentro de la URL en lugar de seguir REST puro (`POST api/v3/customers`), y
> `UpdateAsync{id}` genera una ruta sin separador. Está en la lista de pendientes.

---

## Historial de cambios

Los hitos del proyecto, en el orden en que se construyeron. Cada uno responde a un problema que el anterior
dejaba abierto.

| # | Cambio | Qué resolvió |
|---|---|---|
| 1 | Clean Architecture en 5 proyectos · CRUD de `Customer` con EF Core | Base con la regla de dependencia verificable en los `.csproj` |
| 2 | FluentValidation · `Response<T>` · `ErrorType` | Fallos esperados sin excepciones; `Application` sin conocer HTTP |
| 3 | Migraciones EF · interceptor de auditoría · CORS | Esquema versionado y auditoría en un punto único |
| 4 | JWT · hash con `IPasswordHasher<T>` · User Secrets | Autenticación y secretos fuera del repositorio |
| 5 | Serilog (consola, fichero, SQL Server) | Logging estructurado con destinos según severidad |
| 6 | Versionado de API · Swagger por versión | Posibilidad de convivir contratos distintos |
| 7 | **v1 sin UoW / v2 con Unit of Work** | Límite transaccional movido al caso de uso, con el antipatrón como contraste |
| 8 | Tests de `Infrastructure` y `Application` · test del composition root | Comportamiento fijado antes de refactorizar |
| 9 | **Middleware global de excepciones** · retirada de `try/catch` en v1 y v2 | Un único punto para lo inesperado; los casos de uso dejan subir la excepción |
| 10 | **v3 con CQRS (MediatR)** · commands y queries por carpeta · repositorio de lectura | Separación de escrituras y lecturas; controller acoplado solo a `IMediator` |
| 11 | Detección de duplicados → 409 en v2 y v3 | Alta duplicada como resultado de negocio, no como error |
| 12 | Tests de v3 · reorganización de tests por feature y versión | La suite refleja la misma estructura que el código |

---

## Estado actual y limitaciones conocidas

Este es un proyecto en construcción y prefiero decir dónde está el trabajo pendiente a que se descubra
leyendo. Lo que sé que falta, por orden de prioridad:

### Seguridad

| | Pendiente |
|---|---|
| 1 | **El middleware de excepciones está al final del pipeline y devuelve `ex.Message`.** Registrado después de `MapControllers()`, solo envuelve a los endpoints: lo que falle en autenticación o CORS no lo captura. Y el mensaje crudo de la excepción llega al cliente. Debe ir el primero, responder un texto genérico (`ProblemDetails`) y dejar el detalle solo en el log. `UserAuthApplication` conserva además sus `try/catch` con la misma fuga, y el endpoint de prueba `UserAuth/boom` sigue publicado. |
| 2 | **`EnableSensitiveDataLogging()` está activo en todos los entornos** (`DbContextEF.OnConfiguring`). En producción registraría los valores de los parámetros, incluido `PasswordHash`. |
| 3 | **`SignIn` permite enumerar usuarios.** Un email inexistente y una contraseña incorrecta dan respuestas distinguibles. Debe ser un único 401 genérico. |

### Corrección

| | Pendiente |
|---|---|
| 4 | **Actualizar con los mismos datos devuelve 500** en v2 y v3: EF no escribe nada, `SaveChangesAsync` devuelve 0 y el caso de uso lo traduce a error. Contradice lo explicado en *Unit of Work*. |
| 5 | **`CustomerDto` no expone `Id`**: el listado devuelve clientes que luego no se pueden identificar para actualizar o borrar. |
| 6 | **`DefaultApiVersion` apunta a `1.0`, obsoleta.** Debe apuntar a la vigente. `UserAuthController` no declara la `3.0`, así que un cliente de v3 se autentica contra v2. |
| 7 | **La validación de `Jwt:Key` es asimétrica**: se comprueba al emitir el token, no al arrancar. La comprobación debe estar en el arranque (`JwtOptions` con `ValidateOnStart()`). |

### Tests

| | Pendiente |
|---|---|
| 8 | **La frontera HTTP no tiene ni un test.** Falta un test de integración con `WebApplicationFactory` que recorra SignUp → SignIn → CRUD, y que ejecute **los mismos casos contra v1, v2 y v3** para demostrar que comparten contrato. |
| 9 | **Handlers de v3 sin cubrir** (`Update`, `Delete` y las dos queries), y `ConfigureServicesTest` no incluye `ICustomerReadRepository` ni la resolución de los handlers de MediatR. |

### Diseño

| | Pendiente |
|---|---|
| 10 | **MediatR sin *pipeline behaviors*.** Su valor aquí no está en los handlers, sino en los behaviors: `ValidationBehavior` sacaría la validación que hoy repite cada handler, y `UnhandledExceptionBehavior`/`TransactionBehavior` llevarían al borde del pipeline lo que hoy está dentro de cada caso de uso. |
| 11 | **El lado de lectura devuelve entidades**, y `GetByIdAsync` usa `FindAsync` con tracking. En CQRS la consulta debería proyectar directamente al DTO con `AsNoTracking()`. |
| 12 | **El dominio es anémico** y las reglas de `Customer` están repartidas: tres validadores con reglas idénticas, una configuración de EF que admite nulos que los validadores rechazan, y un mapeo que contempla nulos que nunca llegan. Faltan invariantes en la entidad (factory methods, value objects). La detección de duplicados compara los diez campos y no se apoya en un índice único. |
| 13 | **Generar el JWT y hashear contraseñas son infraestructura**, pero viven en `Application` (`JwtApplication` lee `IConfiguration`) y en el repositorio (`UserRepository.CreateUserAsync`). Deberían ser puertos con la implementación en `Infrastructure`. |
| 14 | **Duplicación que no forma parte de la comparación entre versiones**: `ToActionResult` repetido en los tres controllers y dos sobrecargas idénticas de `ManualMappingCustomer`. |

### Higiene

| | Pendiente |
|---|---|
| 15 | `DbContextEF` depende de `IConfiguration` para una rama muerta de `OnConfiguring`. `IUnitOfWork` expone `_customersUoW` y `_user` con prefijo de campo privado. `Customer.Id` es `int?`. |
| 16 | 13 warnings de nulabilidad enterrados bajo los `CS1591` de documentación XML, y avisos del analizador de `Asp.Versioning` (`AV0013`, `AV0016`, `AV0029`). |
| 17 | `Microsoft.AspNetCore.Identity` 2.3 es el paquete heredado de ASP.NET Core 2.x; `PasswordHasher<T>` está en `Microsoft.Extensions.Identity.Core`. `Transversal` mezcla tipos puros con la configuración de Serilog, y arrastra ASP.NET Core hasta `Application` por dependencia transitiva. |
| 18 | Rutas con el verbo en la URL, sin paginación en `GetAll`, sin refresh token ni roles, caducidad del token fijada en código y sin healthcheck. |

Menores, anotados para no perderlos: `Discount` y `DiscountStatus` son código muerto; el sink de Serilog a
SQL Server apunta a la misma base de datos que la aplicación; y con seis proyectos repitiendo
`TargetFramework` y versiones de paquetes ya compensa un `Directory.Build.props` y *Central Package
Management*.

**Build y tests:** compila sin errores; **126 tests en verde**.

---

## Hoja de ruta

Bloques en el orden en que se van a abordar. El criterio: lo que modifica el contrato público va antes que
lo que lo publica, porque una vez publicado, cambiarlo es un *breaking change*.

| Bloque | Contenido | Estado |
|---|---|---|
| **A** | Versionado de la API · limpieza de rutas a REST · healthcheck | 🟡 Versionado hecho (v1, v2, v3) |
| **B** | Unit of Work · middleware global de excepciones · `EnableSensitiveDataLogging` por entorno | 🟡 Unit of Work cerrado; middleware montado pero por corregir (pendiente nº 1) |
| **C** | Tests de `Application` · CQRS con MediatR · *pipeline behaviors* · tests de integración | 🟡 Tests de `Application` y handlers de v3 hechos; faltan behaviors e integración |
| **D** | Dominio con invariantes · modelado relacional (`Order` → `OrderLine`) · paginación · Postgres | ⬜ |
| **E** | Dockerfile · GitHub Actions · despliegue en Azure | ⬜ |

Siguientes pasos concretos:

1. Cerrar el bloque B: middleware al principio del pipeline con respuesta genérica, `try/catch` fuera de
   `UserAuthApplication`, `EnableSensitiveDataLogging` por entorno.
2. `ValidationBehavior` y tests de los handlers de v3 que faltan.
3. Autenticación: 401 único en `SignIn`, `JwtOptions` validadas al arrancar, `ITokenService` en
   `Infrastructure`.
4. Tests de integración con `WebApplicationFactory` recorriendo las tres versiones, y healthcheck.
5. Bloque D: un agregado real (`Order` → `OrderLine`) donde el Unit of Work tenga dos tablas que confirmar
   de forma atómica.

---

## Licencia

Proyecto personal de aprendizaje, sin licencia de uso definida.
