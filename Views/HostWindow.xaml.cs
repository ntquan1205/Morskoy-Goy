using System.Windows;
using Morskoy_Goy.ViewModels;

namespace Morskoy_Goy.Views
{
    public partial class HostWindow : Window
    {
        private HostViewModel _viewModel;

        public HostWindow()
        {
            InitializeComponent();

            _viewModel = new HostViewModel();
            DataContext = _viewModel;

            _viewModel.OnBackRequested += GoBack;
            _viewModel.OnGameStarted += StartGame;
        }

        private void StartGame()
        {
            var placementWindow = new ShipPlacementWindow(
                _viewModel.PlayerName,
                "Соперник",
                true,
                _viewModel 
            );
            placementWindow.Show();
            this.Hide();
        }

        private void GoBack()
        {
            var mainMenu = new MainMenuView();
            mainMenu.Show();
            this.Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            _viewModel?.Cleanup();
        }
    }
}