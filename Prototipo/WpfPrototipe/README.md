# Prototipo WPF (RegistroPerf)

Cliente de escritorio para probar el consumo de la API desde otra tecnología. Vive en el monorepo como
una carpeta más, no como repo aparte.

- **Stack:** WPF sobre .NET Framework 4.8 y MVVM con
  [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) 8.4.0.
- **Estructura:** `View/Themes/` (ventana y estilos), `ViewModel/MainViewModel.cs` y `Properties/`.
- **Pantalla actual:** formulario de registro (nombre, email, contraseña y repeticiones de 1 a 100) con
  validación en cliente y botones Registrar y Cancelar.
- **Pendiente:** [`MainViewModel.cs`](ViewModel/MainViewModel.cs) simula la llamada con un `Task.Delay`.
  Ahí irá la petición real a la API con `HttpClient`.

## Ejecutar

Abrir [`RegistroPerf.slnx`](RegistroPerf.slnx) en Visual Studio y lanzar `RegistroPerf`. Solo Windows.
