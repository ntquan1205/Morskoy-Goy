//using Morskoy_Goy.Services;
using System.Windows;
using System.Windows.Navigation;

namespace Morskoy_Goy.Views
{
    public partial class MainMenuWindow : Window
    {
        private readonly NavigationService _navigationService;

        public MainMenuWindow()
        {
            InitializeComponent();
            //_navigationService = new NavigationService();
        }

        private void OnStartGameClick(object sender, RoutedEventArgs e)
        {
            //var lobbyWindow = new LobbyWindow();
            //lobbyWindow.Show();
            this.Close();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}