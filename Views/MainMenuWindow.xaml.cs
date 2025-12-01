using System.Windows;

namespace Morskoy_Goy.Views
{
    public partial class MainMenuWindow : Window
    {
        public MainMenuWindow()
        {
            InitializeComponent();
        }

        private void CreateGameButton_Click(object sender, RoutedEventArgs e)
        {
            var hostWindow = new HostWindow();
            hostWindow.Show();
            this.Close();
        }

        private void JoinGameButton_Click(object sender, RoutedEventArgs e)
        {
            var clientWindow = new ClientWindow();
            clientWindow.Show();
            this.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}