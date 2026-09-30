using RegistroPerf.ViewModel.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RegistroPerf.ViewModel
{
    public partial class MainViewModel : IServicesMainViewModel
    {
        public bool EmptyInformation()
        {
            MessageBox.Show("Empty Information", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        public string EmptyInformationString()
        {
            return "Empty Information";
        }


        public bool IncorrectInformation(string parameter)
        {
            MessageBox.Show($"Incorrect Information: {parameter} ", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        public string IncorrectInformationString(string parameter)
        {
            return $"Incorrect Information: {parameter} ";
        }
    }
}
