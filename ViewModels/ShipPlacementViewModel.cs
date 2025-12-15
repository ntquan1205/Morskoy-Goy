using Morskoy_Goy.Models;
using Morskoy_Goy.Services;
using Morskoy_Goy.ViewModels.Base;
using Morskoy_Goy.ViewModels.Common;
using Morskoy_Goy.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Morskoy_Goy.ViewModels
{
    public class ShipPlacementViewModel : ViewModelBase
    {
        private Models.GameField _playerField;
        private ShipPlacementService _placementService;
        private Ship _selectedShip;
        private string _playerName;
        private string _opponentName;
        private bool _isHost;
        private object _networkObject;
        private bool _isHorizontal = true; 

        public ObservableCollection<ShipListItem> ShipsToPlace { get; } = new ObservableCollection<ShipListItem>();

        public RelayCommand CellClickedCommand { get; }
        public RelayCommand RandomPlaceCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand ReadyCommand { get; }

        private bool _isReadyButtonEnabled;
        public bool IsReadyButtonEnabled
        {
            get => _isReadyButtonEnabled;
            set => Set(ref _isReadyButtonEnabled, value);
        }

        private string _statusText = "Разместите все корабли";
        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        public bool IsHorizontal
        {
            get => _isHorizontal;
            set => Set(ref _isHorizontal, value);
        }

        public bool IsVertical
        {
            get => !_isHorizontal;
            set => IsHorizontal = !value;
        }

        public Models.GameField PlayerField => _playerField;

        public Ship SelectedShip
        {
            get => _selectedShip;
            set => Set(ref _selectedShip, value);
        }

        public event Action FieldUpdated;

        public ShipPlacementViewModel(string playerName, string opponentName, bool isHost, object networkObject)
        {
            _playerName = playerName;
            _opponentName = opponentName;
            _isHost = isHost;
            _networkObject = networkObject;

            _playerField = new Models.GameField();
            _placementService = new ShipPlacementService(_playerField);

            CellClickedCommand = new RelayCommand(OnCellClicked);
            RandomPlaceCommand = new RelayCommand(PlaceRandomly);
            ClearCommand = new RelayCommand(ClearField);
            ReadyCommand = new RelayCommand(StartGame);

            InitializeShipsList();
        }

        private void InitializeShipsList()
        {
            ShipsToPlace.Clear();
            foreach (var ship in _placementService.GetShipsToPlace())
            {
                ShipsToPlace.Add(new ShipListItem
                {
                    DisplayName = GetShipName(ship.Type),
                    Ship = ship
                });
            }

            if (ShipsToPlace.Count > 0)
            {
                SelectedShip = ShipsToPlace[0].Ship;
            }
        }

        private void OnCellClicked(object parameter)
        {
            if (!(parameter is string coordinates)) return;

            var parts = coordinates.Split(',');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int x) ||
                !int.TryParse(parts[1], out int y))
                return;

            if (SelectedShip == null)
            {
                MessageBox.Show("Выберите корабль из списка!");
                return;
            }

            if (_placementService.PlaceShip(SelectedShip, x, y, IsHorizontal))
            {
                RemoveShipFromList(SelectedShip);
                UpdateReadyStatus();
                FieldUpdated?.Invoke();
            }
            else
            {
                MessageBox.Show("Нельзя разместить корабль здесь!");
            }
        }

        public void HandleShipSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ShipListItem item)
            {
                SelectedShip = item.Ship;
            }
        }

        private void PlaceRandomly(object parameter = null)
        {
            _placementService.PlaceAllShipsRandomly();
            ShipsToPlace.Clear();
            SelectedShip = null;
            UpdateReadyStatus();
            FieldUpdated?.Invoke();
        }

        private void ClearField(object parameter = null)
        {
            _playerField = new Models.GameField();
            _placementService = new ShipPlacementService(_playerField);
            InitializeShipsList();
            IsReadyButtonEnabled = false;
            FieldUpdated?.Invoke();
        }

        private void StartGame(object parameter = null)
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

            foreach (Window window in Application.Current.Windows)
            {
                if (window is ShipPlacementWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        private void RemoveShipFromList(Ship ship)
        {
            for (int i = ShipsToPlace.Count - 1; i >= 0; i--)
            {
                if (ShipsToPlace[i].Ship == ship)
                {
                    ShipsToPlace.RemoveAt(i);
                    break;
                }
            }

            if (ShipsToPlace.Count > 0)
            {
                SelectedShip = ShipsToPlace[0].Ship;
            }
            else
            {
                SelectedShip = null;
            }
        }

        private void UpdateReadyStatus()
        {
            IsReadyButtonEnabled = _playerField.IsReady;
            StatusText = _playerField.IsReady
                ? "Все корабли размещены! Нажмите 'Готов к бою!'"
                : $"Осталось разместить: {_placementService.ShipsRemaining} кораблей";
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
    }

    public class ShipListItem
    {
        public string DisplayName { get; set; }
        public Ship Ship { get; set; }
    }
}