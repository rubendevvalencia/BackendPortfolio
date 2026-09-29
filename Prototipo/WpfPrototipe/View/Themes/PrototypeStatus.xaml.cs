using System.Windows;
using RegistroPerf.ViewModels;

namespace RegistroPerf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        // PasswordBox no permite Binding en Password (por seguridad), así que la pasamos a mano.
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = PasswordInput.Password;
            _viewModel.Password = password;
        }
    }
}
