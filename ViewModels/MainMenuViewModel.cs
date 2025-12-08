using Morskoy_Goy.ViewModels.Base;
using Morskoy_Goy.ViewModels.Common;
using System.Windows;

namespace Morskoy_Goy.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        public RelayCommand CreateGameCommand { get; }
        public RelayCommand JoinGameCommand { get; }
        public RelayCommand ExitCommand { get; }

        public MainMenuViewModel()
        {
            CreateGameCommand = new RelayCommand(_ => CreateGame());
            JoinGameCommand = new RelayCommand(_ => JoinGame());
            ExitCommand = new RelayCommand(_ => Exit());
        }

        private void CreateGame()
        {
            var hostWindow = new Views.HostWindow();
            hostWindow.Show();
            Application.Current.Windows[0]?.Close();
        }

        private void JoinGame()
        {
            var clientWindow = new Views.ClientWindow();
            clientWindow.Show();
            Application.Current.Windows[0]?.Close();
        }

        private void Exit()
        {
            Application.Current.Shutdown();
        }
    }
}