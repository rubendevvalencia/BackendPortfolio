using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RegistroPerf.ViewModels
{
    public partial class MainViewModel
    {
        private string _fullName = "Ana Prueba";
        private string _email = "ana.prueba@example.com";
        private int _repetitions = 1;
        private string _message = "";

        public string Password { get; set; } = "";

        public bool RegisterService()
        {
            if (_fullName.Length <= 0) return false;
            if(_email.Length <= 0 || !_email.Contains('@')) return false;
            if(Password.Length <= 0) return false;




            return true;
        }
      
    }
}


