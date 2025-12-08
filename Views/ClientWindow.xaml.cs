using System.Windows;
using Morskoy_Goy.ViewModels;

namespace Morskoy_Goy.Views
{
    public partial class ClientWindow : Window
    {
        private ClientViewModel _viewModel;

        public ClientWindow()
        {
            InitializeComponent();

            _viewModel = new ClientViewModel();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            _viewModel.OnBackRequested += GoBack;
            _viewModel.OnGameStarted += StartGame;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClientViewModel.IsStatusVisible))
            {
                StatusBorder.Visibility = _viewModel.IsStatusVisible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void StartGame()
        {
            var placementWindow = new ShipPlacementWindow(
                _viewModel.PlayerName,
                "Хост",
                false,
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
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel?.Cleanup();
        }
    }
}