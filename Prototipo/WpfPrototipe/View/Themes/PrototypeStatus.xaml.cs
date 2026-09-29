using System.Windows;

namespace RegistroPerf
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        // PasswordBox no permite Binding en Password (por seguridad), así que la pasamos a mano.
        private void OnPasswordChanged(object sender, RoutedEventArgs e) { }
    }

}



