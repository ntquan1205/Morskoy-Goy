using System.Windows;
using Morskoy_Goy.GameLogic.Models;
using Morskoy_Goy.GameLogic.Services;
using System.Windows.Controls;

namespace Morskoy_Goy.Views
{
    public partial class ShipPlacementWindow : Window
    {
        private GameFieldLogic _playerField;
        private ShipPlacementService _placementService;
        private Ship _selectedShip;
        private string _playerName;
        private string _opponentName;
        private bool _isHost;
        private object _networkObject;

        public ShipPlacementWindow(string playerName, string opponentName, bool isHost, object networkObject)
        {
            InitializeComponent();

            _playerName = playerName;
            _opponentName = opponentName;
            _isHost = isHost;
            _networkObject = networkObject;

            _playerField = new GameFieldLogic();
            _placementService = new ShipPlacementService(_playerField);
            PlacementField.CellClicked += OnCellClicked;
            PlacementField.SetGameFieldLogic(_playerField);
            InitializeShipsList();
        }

        private void OnCellClicked(int x, int y)
        {
            if (_selectedShip == null)
            {
                MessageBox.Show("Выберите корабль из списка!");
                return;
            }

            bool isHorizontal = true;

            if (_placementService.PlaceShip(_selectedShip, x, y, isHorizontal))
            {
                PlacementField.UpdateView();
                UpdateShipsList();
                CheckIfReady();
            }
            else
            {
                MessageBox.Show("Нельзя разместить корабль здесь!");
            }
        }

        private void InitializeShipsList()
        {
            ShipsListBox.Items.Clear();

            foreach (var ship in _placementService.GetShipsToPlace())
            {
                var item = new ListBoxItem
                {
                    Content = GetShipName(ship.Type),
                    Tag = ship
                };
                ShipsListBox.Items.Add(item);
            }

            if (ShipsListBox.Items.Count > 0)
                ShipsListBox.SelectedIndex = 0;
        }

        private void UpdateShipsList()
        {
            for (int i = ShipsListBox.Items.Count - 1; i >= 0; i--)
            {
                if (ShipsListBox.Items[i] is ListBoxItem item && item.Tag == _selectedShip)
                {
                    ShipsListBox.Items.RemoveAt(i);
                    break;
                }
            }

            if (ShipsListBox.Items.Count > 0)
                ShipsListBox.SelectedIndex = 0;
            else
                _selectedShip = null;
        }

        private string GetShipName(ShipType type)
        {
            return type switch
            {
                ShipType.SingleDeck => "Однопалубный (1 клетка)",
                ShipType.DoubleDeck => "Двухпалубный (2 клетки)",
                ShipType.TripleDeck => "Трёхпалубный (3 клетки)",
                ShipType.FourDeck => "Четырёхпалубный (4 клетки)",
                _ => type.ToString()
            };
        }

        private void CheckIfReady()
        {
            ReadyButton.IsEnabled = _playerField.IsReady;
        }

        private void RandomPlaceButton_Click(object sender, RoutedEventArgs e)
        {
            _placementService.PlaceAllShipsRandomly();
            PlacementField.UpdateView();
            ShipsListBox.Items.Clear();
            _selectedShip = null;
            CheckIfReady();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _playerField = new GameFieldLogic();
            _placementService = new ShipPlacementService(_playerField);
            PlacementField.SetGameFieldLogic(_playerField);
            InitializeShipsList();
            ReadyButton.IsEnabled = false;
        }

        private void ReadyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_playerField.IsReady)
            {
                MessageBox.Show("Расставьте все корабли!");
                return;
            }

            var gameWindow = new GameWindow(
                _playerName,
                _opponentName,
                _isHost,
                _networkObject,
                _playerField
            );

            gameWindow.Show();
            this.Close();
        }

        private void ShipsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShipsListBox.SelectedItem is ListBoxItem selectedItem)
            {
                _selectedShip = selectedItem.Tag as Ship;
            }
        }
    }
}