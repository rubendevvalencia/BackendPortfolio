using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using RegistroPerf.Configuration.Models;
using RegistroPerf.Converters;
using RegistroPerf.Model;
using RegistroPerf.Model.Request;
using RegistroPerf.Model.Response;
using RegistroPerf.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RegistroPerf.ViewModel
{
    public partial class MainViewModel 
    {
        private readonly EndPointsDefinition _endPoints;
        public readonly SingUpDto _singUp = new SingUpDto();
        public readonly SignInDto _signIn = new SignInDto();
       
        public MainViewModel(EndPointsDefinition endPoints) => _endPoints = endPoints;

        public async Task<bool> SignUpService()
        {
            //GuardClauses para evitar llamdas cuando sabemos que es incorrecto, 1era barrera
            if (string.IsNullOrWhiteSpace(_singUp.UserName)) return EmptyInformation();
            if (string.IsNullOrWhiteSpace(_singUp.FirstName)) return EmptyInformation();
            if (string.IsNullOrWhiteSpace(_singUp.Email)) return EmptyInformation();
            if (!_singUp.Email.Contains('@')) return IncorrectInformation("Email without @");
            if (string.IsNullOrEmpty(_singUp.Password)) return EmptyInformation();
            if (_singUp.Password.Length < 8) return IncorrectInformation("Min 8 characters");
            // El resto de reglas (formato completo de email, longitudes) las valida SignUpValidator en la API.

           
            var json = JsonConvert.SerializeObject(_singUp);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var manager = new Connection();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            CancellationToken cancellation = cts.Token;
            HttpClient client = manager.CreateClient(_endPoints.SignUpUrl);
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                HttpResponseMessage responseMessage = await client.PostAsync(_endPoints.SignUpUrl, content, cancellation);
                // Leer el contenido como string
                string jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                if (!responseMessage.IsSuccessStatusCode) return ShowApiError(jsonResponse, (int)responseMessage.StatusCode);

                // Deserializar el JSON al objeto de tipo T
                var conversion = JsonConvert.DeserializeObject<ResponseConverter<bool>>(jsonResponse);
                if (conversion.IsSuccess) MessageBox.Show("Correct Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("Incorrect Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                Mouse.OverrideCursor = Cursors.Arrow;


                return conversion.IsSuccess;
            }
            catch(OperationCanceledException)
            {
                MessageBox.Show("Service doesn't resolve", "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                Mouse.OverrideCursor = Cursors.Arrow;
                return false;
            }

        }

        // Muestra el motivo que devuelve la API (validator) o, si el cuerpo no se entiende, el código HTTP.
        private bool ShowApiError(string jsonResponse, int statusCode)
        {
            string text = $"The API rejected the request (HTTP {statusCode}).";

            try
            {
                var conversion = JsonConvert.DeserializeObject<ResponseConverter<bool>>(jsonResponse);
                if (conversion != null)
                {
                    var messages = new System.Collections.Generic.List<string>();

                    if (!string.IsNullOrWhiteSpace(conversion.Message)) messages.Add(conversion.Message);

                    if (conversion.Error != null)
                    {
                        foreach (BaseError error in conversion.Error)
                        {
                            if (!string.IsNullOrWhiteSpace(error.ErrorMessage)) messages.Add(error.ErrorMessage);
                        }
                    }

                    if (messages.Count > 0) text = string.Join(Environment.NewLine, messages);
                }
            }
            catch (JsonException)
            {
                // Cuerpo que no es JSON: se queda el texto con el código HTTP.
            }

            MessageBox.Show(text, "Sign Up", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        public async Task<string?> SignInService()
        {
            //GuardClauses para evitar llamdas cuando sabemos que es incorrecto, 1era barrera
            if (string.IsNullOrWhiteSpace(_signIn.Email)) return EmptyInformationString();
            if (!_signIn.Email.Contains('@')) return IncorrectInformationString("Email without @");
            if (string.IsNullOrEmpty(_signIn.Password)) return EmptyInformationString();
            if (_signIn.Password.Length < 8) return IncorrectInformationString("Min 8 characters");

            var json = JsonConvert.SerializeObject(_signIn);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var manager = new Connection();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            CancellationToken cancellation = cts.Token;
            HttpClient client = manager.CreateClient(_endPoints.SignInUrl);

            try
            {
                HttpResponseMessage responseMessage = await client.PostAsync(_endPoints.SignInUrl, content, cancellation);
                // Leer el contenido como string
                string jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                if (!responseMessage.IsSuccessStatusCode)
                {
                    ShowApiError(jsonResponse, (int)responseMessage.StatusCode);
                    return null;
                }

                // Deserializar el JSON al objeto de tipo T
                var conversion = JsonConvert.DeserializeObject<ResponseConverter<TokenDto>>(jsonResponse);
                if (conversion.IsSuccess) MessageBox.Show("Correct Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                {
                    MessageBox.Show("Incorrect Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
                return conversion.Data.AccessToken;
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Service dosen't resolve", "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }
    }
}


