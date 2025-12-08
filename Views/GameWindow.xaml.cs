using System;
using System.Windows;
using Morskoy_Goy.Models;
using Morskoy_Goy.Network.Client;
using Morskoy_Goy.Network.Common;
using Morskoy_Goy.Network.Host;
using Morskoy_Goy.Services;

namespace Morskoy_Goy.Views
{
    public partial class GameWindow : Window
    {
        private GameHost? _host;
        private GameClient? _client;
        private Player? _localPlayer;
        private ShipPlacementService? _placementService;

        private bool _isHostMode = false;
        private string _playerName = "";
        private string _opponentName = "";
        private Models.GameField _playerField;

        
        private int _enemyHitsCount = 0;
        private const int TOTAL_ENEMY_DECKS = 20; 

        public string GameStatus { get; set; } = "";
        public string CurrentPlayerInfo { get; set; } = "";
        public string MessageText { get; set; } = "";

        
        public GameWindow()
        {
            InitializeComponent();
        }

        
        public GameWindow(
            string playerName,
            string opponentName,
            bool isHost,
            object networkObject,
            Models.GameField playerField)
        {
            InitializeComponent();

            _playerName = playerName;
            _opponentName = opponentName;
            _isHostMode = isHost;
            _playerField = playerField;

            InitializeGame(networkObject);
            DataContext = this;
        }


        public void ProcessIncomingShotFromNetwork(int x, int y, bool isHit, bool isDestroyed)
        {
            Dispatcher.Invoke(() =>
            {
                
                if (MyField != null)
                {
                    MyField.ProcessShot(x, y);
                }

                if (isHit)
                {
                    MessageText = isDestroyed ?
                        "Противник потопил ваш корабль!" :
                        "Противник попал в ваш корабль!";

                    _localPlayer!.IsMyTurn = false;
                }
                else
                {
                    MessageText = "Противник промахнулся!";
                    _localPlayer!.IsMyTurn = true;
                }

                if (MyField?.AllShipsDestroyed() == true)
                {
                    _localPlayer!.IsMyTurn = false;
                    MessageBox.Show("Вы проиграли! Все ваши корабли уничтожены!", "Игра окончена",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }

                UpdateGameStatus();
            });
        }
        private void InitializeGame(object networkObject)
        {
            try
            {
                _localPlayer = new Player(_isHostMode);
                CopyFieldData(_playerField, _localPlayer.Field);
                _localPlayer.IsReady = true;

                if (MyField != null)
                {
                    MyField.SetGameFieldLogic(_localPlayer.Field);
                    MyField.SetHideShips(false);
                    MyField.CellClicked += OnMyFieldCellClicked;
                }

                var emptyField = new Models.GameField();
                if (EnemyField != null)
                {
                    EnemyField.SetGameFieldLogic(emptyField);
                    EnemyField.SetHideShips(true);
                    EnemyField.CellClicked += OnEnemyFieldCellClicked;
                }

                UpdateGameStatus();

                if (_isHostMode)
                {
                    InitializeAsHost(networkObject);
                }
                else
                {
                    InitializeAsClient(networkObject);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void CopyFieldData(Models.GameField source, Models.GameField destination)
        {
            if (source == null || destination == null) return;

            for (int x = 0; x < Models.GameField.Width; x++)
            {
                for (int y = 0; y < Models.GameField.Height; y++)
                {
                    var sourceCell = source.GetCell(x, y);
                    var destCell = destination.GetCell(x, y);

                    if (sourceCell != null && destCell != null)
                    {
                        destCell.Status = sourceCell.Status;
                        destCell.Ship = sourceCell.Ship;
                    }
                }
            }

            destination.Ships.Clear();
            foreach (var ship in source.Ships)
            {
                destination.Ships.Add(ship);
            }

            destination.IsReady = source.IsReady;
        }

        private void InitializeAsHost(object networkObject)
        {
            if (networkObject is GameHost host)
            {
                _host = host;
            }
            else
            {
                _host = new GameHost();
                _host.Start(12345, _playerName);
            }

            _host.ClientConnected += OnClientConnected;
            _host.ClientDisconnected += OnClientDisconnected;
            _host.ShotResultReceived += OnShotResultReceived;

            MessageText = _host != null ? "Ожидание подключения соперника..." : "Готов к игре";
        }

        private void InitializeAsClient(object networkObject)
        {
            if (networkObject is GameClient client)
            {
                _client = client;
            }
            else
            {
                _client = new GameClient();
                try
                {
                    _client.Connect("127.0.0.1", 12345, _playerName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }
            }

            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.ShotResultReceived += OnShotResultReceived;

            MessageText = "Подключение установлено";
        }

        private void OnEnemyFieldCellClicked(int x, int y)
        {
            if (!_localPlayer?.IsMyTurn ?? true) return;

            var currentState = EnemyField?.GetCellState(x, y);
            if (currentState == CellStatus.Miss ||
                currentState == CellStatus.ShipHited ||
                currentState == CellStatus.ShipDestroyed)
            {
                MessageText = "Уже стреляли в эту клетку!";
                return;
            }

            bool shotSent = false;

            if (_isHostMode && _host != null)
            {
                _host.SendShot(x, y);
                shotSent = true;
            }
            else if (_client != null)
            {
                _client.SendShot(x, y);
                shotSent = true;
            }

            if (shotSent)
            {
                MessageText = $"Выстрел в ({x}, {y}) отправлен...";
                _localPlayer!.IsMyTurn = false; 
                UpdateGameStatus();
            }
            else
            {
                MessageText = "Нет подключения к противнику!";
            }
        }


        private void OnMyFieldCellClicked(int x, int y)
        {
 
            var state = MyField?.GetCellState(x, y);
            MessageText = $"Ваше поле: ({x},{y}) = {state}";
        }

        private void OnShotResultReceived(ShotResultData result)
        {
            Dispatcher.Invoke(() =>
            {
                bool isHit = false;
                bool isDestroyed = false;

                var currentState = EnemyField?.GetCellState(result.X, result.Y);
                if (currentState == CellStatus.Miss ||
                    currentState == CellStatus.ShipHited ||
                    currentState == CellStatus.ShipDestroyed)
                {
                    return;
                }

                if (EnemyField != null)
                {
                    (isHit, isDestroyed) = EnemyField.ProcessShot(result.X, result.Y);
                }

                if (!isHit && result.IsHit)
                {
                    isHit = true;
                    isDestroyed = result.IsShipDestroyed;

                    if (EnemyField != null)
                    {
                        var field = EnemyField.GetFieldLogic(); 
                        var cell = field?.GetCell(result.X, result.Y);
                        if (cell != null)
                        {
                            if (isDestroyed)
                            {
                                cell.Status = CellStatus.ShipDestroyed;
                                
                                MarkCellsAround(result.X, result.Y);
                            }
                            else
                            {
                                cell.Status = CellStatus.ShipHited;
                            }
                            EnemyField.UpdateView();
                        }
                    }
                }

                if (isHit)
                {
                    _enemyHitsCount++;

                    if (isDestroyed)
                    {
                        MessageText = $"Корабль противника потоплен! Попаданий: {_enemyHitsCount}/{TOTAL_ENEMY_DECKS}";
                    }
                    else
                    {
                        MessageText = $"Попадание! Попаданий: {_enemyHitsCount}/{TOTAL_ENEMY_DECKS}";
                    }

                    _localPlayer!.IsMyTurn = true;
                }
                else
                {
                    MessageText = "Промах!";
                    _localPlayer!.IsMyTurn = false;
                }

                if (_enemyHitsCount >= TOTAL_ENEMY_DECKS)
                {
                    _localPlayer!.IsMyTurn = false;
                    MessageText = "Вы победили! Все корабли противника уничтожены!";
                    MessageBox.Show("Поздравляем! Вы победили!", "Игра окончена",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                    return;
                }

                UpdateGameStatus();
            });
        }

        private void MarkCellsAround(int x, int y)
        {
            if (EnemyField == null) return;

            var field = EnemyField.GetFieldLogic();
            if (field == null) return;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;

                    var cell = field.GetCell(checkX, checkY);
                    if (cell != null && cell.Status == CellStatus.Empty)
                    {
                        cell.Status = CellStatus.Miss;
                    }
                }
            }
            EnemyField.UpdateView();
        }

        public void ProcessIncomingShot(int x, int y)
        {
            Dispatcher.Invoke(() =>
            {
                if (MyField != null)
                {
                    var (isHit, isDestroyed) = MyField.ProcessShot(x, y);

                    if (isHit)
                    {
                        MessageText = isDestroyed ?
                            "Ваш корабль потоплен!" :
                            "Попадание по вашему кораблю!";

                        _localPlayer!.IsMyTurn = false;
                    }
                    else
                    {
                        MessageText = "Противник промахнулся!";
                        _localPlayer!.IsMyTurn = true;
                    }

                    if (MyField.AllShipsDestroyed())
                    {
                        _localPlayer!.IsMyTurn = false;
                        MessageBox.Show("Вы проиграли! Все ваши корабли уничтожены!", "Игра окончена",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                }

                UpdateGameStatus();
            });
        }

        private void UpdateGameStatus()
        {
            bool localDestroyed = MyField?.AllShipsDestroyed() ?? false;

            bool enemyDestroyed = (_enemyHitsCount >= TOTAL_ENEMY_DECKS);

            if (localDestroyed)
            {
                GameStatus = "Вы проиграли!";
                CurrentPlayerInfo = "Игра окончена";
            }
            else if (enemyDestroyed)
            {
                GameStatus = "Вы победили!";
                CurrentPlayerInfo = "Игра окончена";
            }
            else if (_localPlayer?.IsMyTurn ?? false)
            {
                GameStatus = "Ваш ход";
                CurrentPlayerInfo = "Стреляйте по полю противника";
            }
            else
            {
                GameStatus = "Ход противника";
                CurrentPlayerInfo = "Ожидайте...";
            }

            OnPropertyChanged(nameof(GameStatus));
            OnPropertyChanged(nameof(CurrentPlayerInfo));
            OnPropertyChanged(nameof(MessageText));
        }

        private void OnClientConnected(string playerName)
        {
            Dispatcher.Invoke(() =>
            {
                MessageText = $"Подключен: {playerName}";
                _opponentName = playerName;
            });
        }

        private void OnClientDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageText = "Соперник отключился";
                MessageBox.Show("Соперник отключился от игры", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            });
        }

        private void OnConnected(string playerName)
        {
            Dispatcher.Invoke(() =>
            {
                MessageText = $"Подключен к: {playerName}";
                _opponentName = playerName;
            });
        }

        private void OnDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageText = "Отключен от хоста";
                MessageBox.Show("Соединение с хостом потеряно", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            });
        }

        private void OnRandomPlacementClick(object sender, RoutedEventArgs e)
        {
            MessageText = "Расстановка уже завершена!";
        }

        private void OnReadyClick(object sender, RoutedEventArgs e)
        {
            MessageText = "Вы уже в игре!";
        }

        private void OnSurrenderClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Сдаться?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                MessageText = "Вы сдались!";
                MessageBox.Show("Вы сдались!", "Игра окончена",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из игры?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_isHostMode && _host != null)
                _host.Stop();
            else if (_client != null)
                _client.Disconnect();

            base.OnClosed(e);
        }
    }
}