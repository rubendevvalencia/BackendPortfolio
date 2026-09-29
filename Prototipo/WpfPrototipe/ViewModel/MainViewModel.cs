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
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RegistroPerf.ViewModels
{
    public partial class MainViewModel
    {
        private readonly string _url; 
        private string _fullName = "Ana Prueba";
        private string _email = "ana.prueba@example.com";
        private int _repetitions = 1;
        private string _message = "";

        public string _password { get; set; } = "";

        public MainViewModel(string url)
        {
            _url = url;
        }

        public async Task<bool> RegisterService()
        {
            if (_fullName.Length <= 0) return false;
            if(_email.Length <= 0 || !_email.Contains('@')) return false;
            if(_password.Length <= 0) return false;

            var request = new SingUpDto()
            {
                FirstName = _fullName,
                LastName = _fullName,
                Email = _email,
                UserName = _fullName,
                Password = _password
            };
            
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            ResponseConverter<bool> resultCorrect;
            var manager = new Connection();
            HttpClient client = manager.CreateClient(_url);
            HttpResponseMessage responseMessage = await client.PostAsync(_url, content);

            if (!responseMessage.IsSuccessStatusCode)
            {
                new SystemException("Error response client");
                return false;
            }
            else
            {
                // Leer el contenido como string
                string jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                // Deserializar el JSON al objeto de tipo T
                var conversion = JsonConvert.DeserializeObject<ResponseConverter<bool>>(jsonResponse);
                if (conversion.IsSuccess) MessageBox.Show("Correct Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("Incorrect Sign Up", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);

                return true;
            }


            return true;
        }
      
    }
}


