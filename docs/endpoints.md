# Endpoints

[← Volver al README](../README.md)

Referencia completa de la API. En local, la base es `https://localhost:7051` y Swagger queda en
`/swagger`, con un documento por versión en el desplegable de arriba a la derecha.

## Autenticación

Los dos endpoints de `UserAuth` son `[AllowAnonymous]`. **Todo lo demás exige un JWT válido**:

```http
Authorization: Bearer <access_token>
```

El token sale de `POST /SignIn` y **caduca a la hora** (`expiresIn`: 3600 s), un valor fijado en código y no
en configuración, sin refresh token ni roles (pendiente nº 18).

## El contrato: `Response<T>`

Todos los endpoints —acierto o fallo— devuelven la misma envoltura, definida en
[`Transversal/Common/Response.cs`](../Ecommerce.Transversal/Common/Response.cs):

```jsonc
{
  "data": { },          // el resultado; null cuando IsSuccess es false
  "isSuccess": true,
  "message": "",        // solo se rellena en los fallos
  "errors": { },        // errores de validación por propiedad (v1-v3)
  "error": null,        // lista de errores de validación (solo v4)
  "errorType": 1        // el motivo del fallo
}
```

`ErrorType` es lo que el controller traduce a código HTTP, sin mirar el `Message`:

| `errorType` | Valor | HTTP | Cuándo |
|---|---|---|---|
| `None` | 1 | **200** | La operación fue correcta |
| `Validation` | 2 | **400** | El cuerpo no cumple las reglas |
| `NotFound` | 3 | **404** | El recurso no existe |
| `Unexpected` | 4 | **500** | Fallo no controlado |
| `Duplicated` | 5 | **409** | Ya existe un cliente con esos datos |

A esos se suma el **429** del rate limiter, que responde sin cuerpo cuando se supera el cupo
(ver [*Rate limiting*](decisiones-tecnicas.md#rate-limiting-versión-simplificada-a-modo-de-prueba)), y el
**401** cuando falta el token o no es válido.

## `UserAuth` — `api/v{1|2|3}/UserAuth`

Un único controller sirve las tres versiones; v1 y v2 están marcadas `Deprecated`. **No declara la `4.0`**,
así que un cliente de v4 se autentica contra v3 (pendiente nº 6).

| Verbo | Ruta | Cuerpo | Devuelve |
|---|---|---|---|
| `POST` | `/SignUp` | `SignUpDto` | `Response<bool>` |
| `POST` | `/SignIn` | `SignInDto` | `Response<TokenDto>` |

```jsonc
// SignUpDto
{ "firstName": "Ada", "lastName": "Lovelace", "email": "ada@example.com",
  "userName": "ada", "password": "<contraseña>" }

// SignInDto
{ "email": "ada@example.com", "password": "<contraseña>" }

// TokenDto, dentro de data
{ "accessToken": "eyJhbGciOi...", "tokenType": "Bearer", "expiresIn": 3600 }
```

> Un email inexistente y una contraseña incorrecta dan hoy respuestas distinguibles, así que `SignIn`
> permite enumerar usuarios. Es el pendiente nº 3.

## `Customer` — las cuatro versiones

El mismo recurso implementado cuatro veces. Las rutas de v1 y v2 son idénticas entre sí, y las de v3 y v4
también; lo que cambia es la implementación detrás de cada una y, en v4, el contrato del 400.

| Operación | v1 · v2 | v3 · v4 | Mensaje MediatR (v3/v4) |
|---|---|---|---|
| Listar | `GET /GetAllAsync` | `GET /GetAllAsync` | `GetAllCustomerQuery` |
| Recuperar uno | `GET /GetByIdAsync/{id}` | `GET /GetByIdAsync/{id}` | `GetCustomerQuery` |
| Crear | `POST /AddAsync` | `POST /Create` | `CreateCustomerCommand` |
| Actualizar | `PUT /UpdateAsync{id}` | — | — |
| Actualizar (POST) | `POST /UpdateAsyncPost/{id}` | `POST /UpdateAsyncPost` | `UpdateCustomerCommand` |
| Borrar | `DELETE /DeleteAsync/{id}` | `DELETE /DeleteAsync/{id}` | `DeleteCustomerCommand` |

Dos diferencias de forma que no son descuidos:

- **v3 y v4 renombran `AddAsync` a `Create`** y **no exponen `PUT`**: la actualización va solo por `POST`
  con el `Id` dentro del cuerpo, no en la ruta.
- **v1 no detecta duplicados** —es el antipatrón a propósito—, así que nunca responde 409. v2, v3 y v4 sí.

```jsonc
// CustomerDto: el cuerpo de creación y actualización, y lo que devuelve el listado
{ "companyName": "Contoso", "contactName": "Ada Lovelace", "contactTitle": "Owner",
  "address": "Calle Mayor 1", "city": "Valencia", "region": "VLC",
  "postalCode": "46001", "country": "España", "phone": "600000000", "fax": "" }
```

> **`CustomerDto` no expone el `Id`**, así que los clientes que devuelve el listado no se pueden
> identificar después para actualizarlos o borrarlos. Es el pendiente nº 5.

### Qué cambia en v4: el 400

Mismas rutas y mismos mensajes que v3, en su propio *namespace*. Lo que cambia es la respuesta a una
petición inválida —incluido un `id <= 0` en `GetByIdAsync` y `DeleteAsync`, que en v3 comprueba el
controller a mano—: el 400 lo genera `GlobalExceptionHandler` a partir de la excepción que lanza
`ValidationBehaviour`, y los errores viajan en `error` como lista, no en `errors` como diccionario.

```jsonc
// 400 en v1, v2 y v3
{ "isSuccess": false, "errorType": 2, "message": "One or more validation errors occurred.",
  "errors": { "City": ["City is required."] } }

// 400 en v4
{ "isSuccess": false, "errorType": 1,
  "error": [ { "propertyMessage": "City", "errorMessage": "City is required." } ] }
```

Las dos diferencias que saltan a la vista —`errorType` a `1` (*None*, que el enum define como *operación
correcta*) y dos colecciones distintas para lo mismo— son el pendiente nº 20.

## Dos avisos antes de probarla

> **El listado de v3 y v4 se sirve desde Redis y la caché no se invalida nunca.** Crear, actualizar o
> borrar un cliente no toca la caché, así que `GetAllAsync` puede seguir devolviendo el estado anterior
> hasta que la entrada expire sola (una hora absoluta, doce minutos deslizantes). Es el pendiente nº 24 y
> el fallo funcional más visible de la API. `GetByIdAsync` no está cacheado, a propósito.
>
> Y si Redis no responde, `GetAllAsync` no cae a la base de datos: responde **500** (pendiente nº 26).

> **Las rutas llevan el verbo dentro de la URL** en lugar de seguir REST puro (`POST api/v3/customers`), y
> `UpdateAsync{id}` genera una ruta sin separador. Está en la lista de pendientes, y limpiarlo es un
> *breaking change* del contrato público.

El detalle de cada pendiente citado aquí está en [*limitaciones conocidas*](limitaciones.md).
