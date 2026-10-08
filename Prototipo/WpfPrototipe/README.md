# Prototipo WPF (RegistroPerf)

Cliente de escritorio para probar el consumo de la API desde otra tecnología. Vive en el monorepo como
una carpeta más, no como repo aparte.

- **Stack:** WPF sobre .NET Framework 4.8. **Es un cliente con code-behind, no MVVM:** la ventana gestiona
  eventos `Click` y de campos, y no hay `Binding` ni comandos. Las carpetas (`View`, `ViewModel`, `Model`) solo
  anticipan la separación; `MainViewModel` es hoy una clase de servicio que llama a la API, sin notificación
  de cambios. La migración a MVVM está en la hoja de ruta (ver más abajo).
  [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) 8.4.0 está referenciado en
  el csproj, pero aún no se usa: es el paso previo a esa migración.
- **Estructura:** `View/Themes/` (ventana, estilos y `App.xaml.cs`), `ViewModel/` (`MainViewModel` y sus
  servicios de mensajes), `Configuration/` (URLs de los endpoints), `Services/`, `Converters/`, `Model/` y
  `Properties/`.
- **Pantalla actual:** una sola ventana con dos tarjetas lado a lado.
  - **Sign Up:** usuario, nombre completo, email y contraseña, con validación en cliente y botones Register y
    Cancelar.
  - **Sign In:** email y contraseña. Si la API responde bien, el `AccessToken` se muestra en una caja de solo
    lectura (monoespaciada, con fondo tintado) para poder copiarlo.
- **Consumo de la API:** [`MainViewModel`](ViewModel/MainViewModel.cs) expone `SignUpService()` y
  `SignInService()`. Cada uno valida en cliente (guard clauses), envía un `POST` JSON con `HttpClient`
  (timeout de 30 s) a través de [`Connection`](Services/Connection.cs) y deserializa la respuesta con
  Newtonsoft.Json en un `ResponseConverter<T>`. `SignInService()` devuelve el token (o `null` si falla).
- **Modelos:** los DTOs están en `Model/Request` (`SingUpDto`, `SignInDto`) y `Model/Response` (`UserDto`,
  `TokenDto`). El `ViewModel` guarda un DTO de cada tipo y el code-behind de la ventana lo rellena con los
  eventos de los campos (los `PasswordBox` no admiten `Binding`).
- **Endpoints:** [`EndPointsService`](Configuration/Services/EndPointsService.cs) lee las URLs ya resueltas de
  `App.ApiConf` y las entrega al `ViewModel` en un [`EndPointsDefinition`](Configuration/Models/EndPointsDefinition.cs)
  (`SignUpUrl`, `SignInUrl`).
- **Estilos:** los colores y la tipografía están en `View/Themes/ColorsStyle.xaml` y los estilos de controles
  en `Controls.xaml` (`Card`, `Section`, `Label`, campos, botones `Primary`/`Secondary` y `TokenBox`).
- **Pendiente:** el manejo de errores del `ViewModel` es básico, el token solo se muestra (aún no se usa en
  llamadas autenticadas) y no hay inyección de dependencias: la ventana crea `EndPointsService` a mano.

## Hoja de ruta: migración a MVVM

Estado actual: code-behind. Pasos previstos, en orden:

1. Hacer que `MainViewModel` herede de `ObservableObject` y exponga los DTOs como propiedades observables.
2. Sustituir los eventos `Click` por comandos (`[RelayCommand]`, con versiones asíncronas para las llamadas HTTP).
3. Enlazar los campos de texto con `Binding`. Los `PasswordBox` no admiten `Binding` directo, así que
   necesitarán un comportamiento adjunto (*attached behavior*) o una solución equivalente.
4. Inyectar `EndPointsService` y `Connection` en el `ViewModel` en lugar de crearlos a mano en la ventana.
5. Tests unitarios del `ViewModel`, posibles solo una vez desaparezca la dependencia del code-behind.

## Configuración

.NET Framework no trae `appsettings.json`, así que se monta a mano con `Microsoft.Extensions.Configuration`
(mismos paquetes y mismo criterio que la API). Se construye una sola vez en
[`App.xaml.cs`](View/Themes/App.xaml.cs), antes de abrir la ventana, y el resultado queda en `App.ApiConf`.

Orden de carga (el último gana):

1. [`appsettings.json`](appsettings.json), obligatorio. Se versiona y define la **forma** de la configuración.
2. `appsettings.{Entorno}.json`, opcional. El entorno sale de la variable `DOTNET_ENVIRONMENT` y, si no existe,
   es `Production`.
3. Variables de entorno, con `__` como separador de nivel.

```json
{
  "ApiConf": {
    "BaseUrl": "http://localhost:5102",
    "SignUpPath": "/api/v4/UserAuth/SignUp",
    "SignInPath": "/api/v4/UserAuth/SignIn"
  }
}
```

- Cada clave se enlaza por nombre con una propiedad de [`ApiOptions`](Model/ApiOptions.cs). Si falta `BaseUrl`,
  `SignUpPath` o `SignInPath`, la app avisa y se cierra.
- `ApiOptions` expone además `SignUpUrl` y `SignInUrl` (base + ruta ya unidas) para no concatenar en cada ventana.
- Para apuntar a otra API sin tocar el archivo: `ApiConf__BaseUrl=http://otro-host:5102`.
- El archivo se copia junto al `.exe` en cada compilación (`CopyToOutputDirectory` en el csproj); si se mueve el
  `.exe` a otra carpeta hay que llevarse el JSON con él.
- No hay secretos en el cliente. Si los hubiera, se declararían vacíos en el JSON y sus valores irían por
  variables de entorno o User Secrets, nunca versionados (el `.gitignore` ya excluye `appsettings.*.local.json`).
  Ojo: en una app de escritorio distribuida eso protege el repositorio, no al usuario que ejecuta el `.exe`.

## Ejecutar

Abrir [`RegistroPerf.slnx`](RegistroPerf.slnx) en Visual Studio y lanzar `RegistroPerf`. Solo Windows. La API
debe estar levantada en la URL configurada.
