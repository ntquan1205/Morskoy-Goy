using System.Windows;
using Morskoy_Goy.ViewModels;

namespace Morskoy_Goy.Views
{
    public partial class ShipPlacementWindow : Window
    {
        private ShipPlacementViewModel _viewModel;

        public ShipPlacementWindow(string playerName, string opponentName, bool isHost, object networkObject)
        {
            InitializeComponent();
            _viewModel = new ShipPlacementViewModel(playerName, opponentName, isHost, networkObject);
            DataContext = _viewModel;

            _viewModel.FieldUpdated += OnFieldUpdated;

            PlacementField.CellClicked += (x, y) =>
                _viewModel.CellClickedCommand?.Execute($"{x},{y}");

            PlacementField.SetGameFieldLogic(_viewModel.PlayerField);
        }

        private void OnFieldUpdated()
        {
            PlacementField.SetGameFieldLogic(_viewModel.PlayerField);
            PlacementField.UpdateView();
        }

        private void ShipsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _viewModel.HandleShipSelected(sender, e);
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            if (_viewModel != null)
            {
                _viewModel.FieldUpdated -= OnFieldUpdated;
            }
        }
    }
}