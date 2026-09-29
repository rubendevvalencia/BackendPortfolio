using System.Threading;
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
            string url = "http://localhost:5102/swagger/index.html?urls.primaryName=V4#/UserAuth/post_api_v4_UserAuth_SignUp";//Appsettings
            _viewModel = new MainViewModel(url);
            DataContext = _viewModel;
        }

        // PasswordBox no permite Binding en Password (por seguridad), así que la pasamos a mano.
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = PasswordInput.Password;
            _viewModel._password = password;
        }

        private void Button_Click(object sender, RoutedEventArgs e) => _viewModel.RegisterService();
        
    }
}
