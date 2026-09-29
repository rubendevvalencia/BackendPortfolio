using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RegistroPerf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // [ObservableProperty] genera la propiedad pública (FullName, Email…) con aviso de cambio.
        [ObservableProperty] private string _fullName = "Ana Prueba";
        [ObservableProperty] private string _email = "ana.prueba@example.com";
        [ObservableProperty] private int _repetitions = 1;
        [ObservableProperty] private string _message = "";

        public string Password { get; set; } = "";

        // [RelayCommand] genera RegisterCommand.
        // IncludeCancelCommand genera RegisterCancelCommand, que solo se habilita mientras se ejecuta.
        [RelayCommand(IncludeCancelCommand = true)]
        private async Task RegisterAsync(CancellationToken ct)
        {
            var error = Validate();
            if (error is not null)
            {
                Message = error;
                return;
            }

            // Math.Clamp no existe en .NET Framework 4.8.
            int atLeastOne = Math.Max(Repetitions, 1);
            int runs = Math.Min(atLeastOne, 100);
            try
            {
                for (int i = 1; i <= runs; i++)
                {
                    Message = $"Registrando {i}/{runs}…";
                    await Task.Delay(300, ct); // aquí irá la llamada real a la API
                }
                Message = $"Hecho: {runs} registro(s) de {Email}.";
            }
            catch (OperationCanceledException)
            {
                Message = "Cancelado.";
            }
        }

        private string? Validate()
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "El nombre es obligatorio.";
            // string.Contains(char) no existe en .NET Framework 4.8: se usa la sobrecarga de string.
            if (!Email.Contains("@")) return "El email no es válido.";

            bool hasDigit = Password.Any(char.IsDigit);
            bool hasLetter = Password.Any(char.IsLetter);
            if (Password.Length < 8 || !hasDigit || !hasLetter)
                return "La contraseña necesita 8 caracteres con letras y números.";
            return null;
        }
    }
}


