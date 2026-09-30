using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RegistroPerf.ViewModel;

namespace RegistroPerf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            string url = "http://localhost:5102/api/v4/UserAuth/SignUp";//Change to Appsettings
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

        private void EmailInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string email = EmailInput.Text;
            _viewModel._email = email;
        }

        private void tbFullName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string name = tbFullName.Text;
            _viewModel._fullName = name;
        }

        private void tbUserName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string userName = tbUserName.Text;
            _viewModel._userName = userName;
        }
    }
}
