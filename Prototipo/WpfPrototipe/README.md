# Prototipo WPF (RegistroPerf)

Cliente de escritorio para probar el consumo de la API desde otra tecnología. Vive en el monorepo como
una carpeta más, no como repo aparte.

- **Stack:** WPF sobre .NET Framework 4.8 y MVVM con
  [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) 8.4.0.
- **Estructura:** `View/Themes/` (ventana, estilos y `App.xaml.cs`), `ViewModel/MainViewModel.cs`,
  `Services/`, `Model/` y `Properties/`.
- **Pantalla actual:** formulario de registro (usuario, nombre completo, email y contraseña) con validación en
  cliente y botones Register y Cancelar.
- **Consumo de la API:** [`MainViewModel.cs`](ViewModel/MainViewModel.cs) envía el registro con
  `HttpClient` (`POST` JSON al endpoint de SignUp de la API) a través de
  [`Connection`](Services/Connection.cs), y deserializa la respuesta con Newtonsoft.Json. Los DTOs están en
  `Model/Request` y `Model/Response`.
- **Pendiente:** el manejo de errores del `ViewModel` es básico.

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
  "Api": {
    "BaseUrl": "http://localhost:5102",
    "SignUpPath": "/api/v4/UserAuth/SignUp"
  }
}
```

- Cada clave se enlaza por nombre con una propiedad de [`ApiOptions`](Model/ApiOptions.cs). Si falta `BaseUrl` o
  `SignUpPath`, la app avisa y se cierra.
- Para apuntar a otra API sin tocar el archivo: `Api__BaseUrl=http://otro-host:5102`.
- El archivo se copia junto al `.exe` en cada compilación (`CopyToOutputDirectory` en el csproj); si se mueve el
  `.exe` a otra carpeta hay que llevarse el JSON con él.
- No hay secretos en el cliente. Si los hubiera, se declararían vacíos en el JSON y sus valores irían por
  variables de entorno o User Secrets, nunca versionados (el `.gitignore` ya excluye `appsettings.*.local.json`).
  Ojo: en una app de escritorio distribuida eso protege el repositorio, no al usuario que ejecuta el `.exe`.

## Ejecutar

Abrir [`RegistroPerf.slnx`](RegistroPerf.slnx) en Visual Studio y lanzar `RegistroPerf`. Solo Windows. La API
debe estar levantada en la URL configurada.
