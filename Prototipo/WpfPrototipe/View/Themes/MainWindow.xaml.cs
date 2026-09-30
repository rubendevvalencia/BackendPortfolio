using RegistroPerf.Configuration.Models;
using RegistroPerf.Configuration.Services;
using RegistroPerf.ViewModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RegistroPerf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            EndPointsService endPointsService = new EndPointsService(); //Hay que buscar la inyección de dependencias, esto es una locura
            _viewModel = new MainViewModel(endPointsService.ReadUrlAppSettings());
            DataContext = _viewModel;
        }

        // PasswordBox no permite Binding en Password (por seguridad), así que la pasamos a mano.
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = PasswordInput.Password;
            _viewModel._singUp.Password = password;
        }

        private void Button_Click(object sender, RoutedEventArgs e) => _viewModel.SignUpService();

        private void EmailInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string email = EmailInput.Text;
            _viewModel._singUp.Email = email;
        }

        private void tbFullName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string name = tbFullName.Text;
            _viewModel._singUp.FirstName = name;
            _viewModel._singUp.LastName = name;
        }

        private void tbUserName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string userName = tbUserName.Text;
            _viewModel._singUp.UserName = userName;
        }

        private void tbSignInUserName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string userName = tbSignInUserName.Text;
            _viewModel._signIn.Email = userName;
        }

        private void SignInPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = SignInPasswordInput.Password;
            _viewModel._signIn.Password = password;
        }

        // async void es obligatorio en un manejador de eventos; con async Task WPF no lo encuentra.
        private async void button_signIn_Click(object sender, RoutedEventArgs e)
        {
            string? result = await _viewModel.SignInService();
            tbToken.Text = result ?? string.Empty;
        }
    }
}
