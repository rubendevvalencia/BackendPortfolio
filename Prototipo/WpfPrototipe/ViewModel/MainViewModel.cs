using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using RegistroPerf.Converters;
using RegistroPerf.Model;
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

namespace RegistroPerf.ViewModel
{
    public partial class MainViewModel 
    {
        private readonly string _url;

        public string _password { get; set; } = "";
        public string _fullName { get; set; } = "";
        public string _email { get; set; } = "";
        public string _userName { get; set; } = "";
       
        public MainViewModel(string url)
        {
            _url = url;
        }

        public async Task<bool> RegisterService()
        {
            //GuardClauses para evitar llamdas cuando sabemos que es incorrecto, 1era barrera
            if (string.IsNullOrWhiteSpace(_userName)) return EmptyInformation();
            if (string.IsNullOrWhiteSpace(_fullName)) return EmptyInformation();
            if (string.IsNullOrWhiteSpace(_email)) return EmptyInformation();
            if (!_email.Contains('@')) return IncorrectInformation("Email without @");
            if (string.IsNullOrEmpty(_password)) return EmptyInformation();
            if (_password.Length < 8) return IncorrectInformation("Min 8 characters");
            // El resto de reglas (formato completo de email, longitudes) las valida SignUpValidator en la API.

            var request = new SingUpDto()
            {
                FirstName = _fullName,
                LastName = _fullName,
                Email = _email,
                UserName = _userName,
                Password = _password
            };
            
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var manager = new Connection();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            CancellationToken cancellation = cts.Token;
            HttpClient client = manager.CreateClient(_url);

            try
            {
                HttpResponseMessage responseMessage = await client.PostAsync(_url, content, cancellation);
                // Leer el contenido como string
                string jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                if (!responseMessage.IsSuccessStatusCode) return ShowApiError(jsonResponse, (int)responseMessage.StatusCode);

                // Deserializar el JSON al objeto de tipo T
                var conversion = JsonConvert.DeserializeObject<ResponseConverter<bool>>(jsonResponse);
                if (conversion.IsSuccess) MessageBox.Show("Correct Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("Incorrect Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return conversion.IsSuccess;
            }
            catch(OperationCanceledException)
            {
                MessageBox.Show("Service dosen't resolve", "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}


