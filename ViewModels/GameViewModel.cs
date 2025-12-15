using Morskoy_Goy.Models;
using Morskoy_Goy.Network.Client;
using Morskoy_Goy.Network.Common;
using Morskoy_Goy.Network.Host;
using Morskoy_Goy.Services;
using Morskoy_Goy.ViewModels.Base;
using Morskoy_Goy.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Morskoy_Goy.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private GameHost _host;
        private GameClient _client;
        private Player _localPlayer;

        private bool _isHostMode = false;
        private string _playerName = "";
        private string _opponentName = "";
        private Models.GameField _playerField;

        private int _enemyHitsCount = 0;
        private const int TOTAL_ENEMY_DECKS = 20;

        private string _gameStatus = "Игра начинается...";
        private string _currentPlayerInfo = "";
        private string _messageText = "";
        private string _connectionStatus = "Подключено";
        private bool _isMyTurn = false;
        private bool _isGameOver = false;

        private int _myHitsCount = 0;
        private const int TOTAL_MY_DECKS = 20;

        public GameViewModel(
            string playerName,
            string opponentName,
            bool isHost,
            object networkObject,
            Models.GameField playerField)
        {
            _playerName = playerName;
            _opponentName = opponentName;
            _isHostMode = isHost;
            _playerField = playerField;

            InitializeGame(networkObject);

            if (_isHostMode && _host != null)
            {
                _host.TurnChanged += OnTurnChanged;
            }
            else if (_client != null)
            {
                _client.TurnChanged += OnTurnChanged;
            }
        }

        private void OnTurnChanged(bool isMyTurn)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsMyTurn = isMyTurn;
                UpdateGameStatus();
            });
        }

        public void Cleanup()
        {
            if (_isHostMode && _host != null)
            {
                _host.TurnChanged -= OnTurnChanged;
                _host.Stop();
            }
            else if (_client != null)
            {
                _client.TurnChanged -= OnTurnChanged;
                _client.Disconnect();
            }
        }
        public string GameStatus
        {
            get => _gameStatus;
            set => Set(ref _gameStatus, value);
        }

        public string CurrentPlayerInfo
        {
            get => _currentPlayerInfo;
            set => Set(ref _currentPlayerInfo, value);
        }

        public string MessageText
        {
            get => _messageText;
            set => Set(ref _messageText, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => Set(ref _connectionStatus, value);
        }

        public bool IsMyTurn
        {
            get => _isMyTurn;
            set
            {
                if (Set(ref _isMyTurn, value))
                {
                    UpdateGameStatus();
                }
            }
        }

        public bool IsGameOver
        {
            get => _isGameOver;
            set => Set(ref _isGameOver, value);
        }

        public Player LocalPlayer => _localPlayer;
        public Models.GameField PlayerField => _playerField;
        public Models.GameField EnemyField { get; private set; }

        public ICommand ExitCommand => new RelayCommand(ExitGame);
        public ICommand SurrenderCommand => new RelayCommand(Surrender);
        public ICommand EnemyCellClickCommand => new RelayCommand<object>(OnEnemyCellClicked);

        private void InitializeGame(object networkObject)
        {
            try
            {
                _localPlayer = new Player(_isHostMode);
                CopyFieldData(_playerField, _localPlayer.Field);
                _localPlayer.IsReady = true;

                EnemyField = new Models.GameField();

            if (_isHostMode)
            {
                InitializeAsHost(networkObject);
            }
            else
            {
                InitializeAsClient(networkObject);
            }

                IsMyTurn = _localPlayer.IsMyTurn;
                UpdateGameStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyFieldData(Models.GameField source, Models.GameField destination)
        {
            if (source == null || destination == null) return;

            // Reset destination cells before copy
            for (int x = 0; x < Models.GameField.Width; x++)
            {
                for (int y = 0; y < Models.GameField.Height; y++)
                {
                    var destCell = destination.GetCell(x, y);
                    if (destCell != null)
                    {
                        destCell.Status = CellStatus.Empty;
                        destCell.Ship = null;
                    }
                }
            }

            var shipMap = new System.Collections.Generic.Dictionary<Ship, Ship>();
            destination.Ships.Clear();

            for (int x = 0; x < Models.GameField.Width; x++)
            {
                for (int y = 0; y < Models.GameField.Height; y++)
                {
                    var sourceCell = source.GetCell(x, y);
                    var destCell = destination.GetCell(x, y);

                    if (sourceCell != null && destCell != null)
                    {
                        destCell.Status = sourceCell.Status;

                        if (sourceCell.Ship != null)
                        {
                            if (!shipMap.TryGetValue(sourceCell.Ship, out var mappedShip))
                            {
                                mappedShip = new Ship(sourceCell.Ship.Type);
                                shipMap[sourceCell.Ship] = mappedShip;
                                destination.Ships.Add(mappedShip);
                            }

                            destCell.Ship = mappedShip;
                            mappedShip.OccupiedCells.Add(destCell);
                        }
                        else
                        {
                            destCell.Ship = null;
                        }
                    }
                }
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

            _host.SetLocalPlayer(_localPlayer);

            _host.ClientConnected += OnClientConnected;
            _host.ClientDisconnected += OnClientDisconnected;
            _host.ShotResultReceived += OnShotResultReceived;
            _host.IncomingShotReceived += ProcessIncomingShot;
            _host.TurnChanged += OnTurnChanged; 

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
                    return;
                }
            }

            _client.SetLocalPlayer(_localPlayer);

            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.ShotResultReceived += OnShotResultReceived;
            _client.TurnChanged += OnTurnChanged;

            _client.IncomingShotReceived += ProcessIncomingShot;

            MessageText = "Подключение установлено";
        }

        private void OnEnemyCellClicked(object parameter)
        {
            if (!IsMyTurn || IsGameOver) return;

            if (!(parameter is string coordinates)) return;

            var parts = coordinates.Split(',');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int x) ||
                !int.TryParse(parts[1], out int y))
                return;

            var cell = EnemyField.GetCell(x, y);
            if (cell == null ||
                cell.Status == CellStatus.Miss ||
                cell.Status == CellStatus.ShipHited ||
                cell.Status == CellStatus.ShipDestroyed)
            {
                MessageText = "Уже стреляли в эту клетку!";
                return;
            }

            bool shotSent = false;

            if (_isHostMode && _host != null)
            {
                _host.SendShot(x, y);
                shotSent = true;
                IsMyTurn = false;
                MessageText = $"Выстрел в ({x}, {y}) отправлен... Ожидаем результат";
            }
            else if (_client != null)
            {
                _client.SendShot(x, y);
                shotSent = true;
                IsMyTurn = false;
                MessageText = $"Выстрел в ({x}, {y}) отправлен... Ожидаем результат";
            }

            if (!shotSent)
            {
                MessageText = "Нет подключения к противнику!";
            }

            UpdateGameStatus();
        }

        public void ProcessIncomingShot(ShotResultData shotResult)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var cell = _localPlayer.Field.GetCell(shotResult.X, shotResult.Y);
                if (cell == null) return;

                if (shotResult.IsHit)
                {
                    _myHitsCount++;
                    MessageText = shotResult.IsShipDestroyed ?
                        $"Ваш корабль потоплен! Уничтожено палуб: {_myHitsCount}/{TOTAL_MY_DECKS}" :
                        $"Попадание по вашему кораблю! Уничтожено палуб: {_myHitsCount}/{TOTAL_MY_DECKS}";

                    IsMyTurn = false;
                }
                else
                {
                    MessageText = "Противник промахнулся! Теперь ваш ход.";
                    IsMyTurn = true;
                }

                if (_localPlayer.AllShipsDestroyed())
                {
                    IsGameOver = true;
                    IsMyTurn = false;
                    MessageText = "Вы проиграли! Все ваши корабли уничтожены!";
                    MessageBox.Show("Вы проиграли! Все ваши корабли уничтожены!", "Игра окончена",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                OnPlayerFieldUpdated?.Invoke();

                UpdateGameStatus();
            });
        }

        private void OnShotResultReceived(ShotResultData result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {

                var cell = EnemyField.GetCell(result.X, result.Y);
                if (cell == null) return;

                if (cell.Status == CellStatus.Miss ||
                    cell.Status == CellStatus.ShipHited ||
                    cell.Status == CellStatus.ShipDestroyed)
                {
                    MessageText = "Повторный выстрел в эту клетку!";
                    return;
                }

                if (result.IsHit)
                {
                    _enemyHitsCount++;
                    cell.Status = result.IsShipDestroyed ?
                        CellStatus.ShipDestroyed : CellStatus.ShipHited;

                    if (result.IsShipDestroyed)
                    {
                        MarkCellsAroundDestroyed(result.X, result.Y);
                        MessageText = $"Корабль противника потоплен! Попаданий: {_enemyHitsCount}/{TOTAL_ENEMY_DECKS}";
                    }
                    else
                    {
                        MessageText = $"Попадание! Попаданий: {_enemyHitsCount}/{TOTAL_ENEMY_DECKS}";
                    }

                    IsMyTurn = result.ShouldRepeatTurn;

                    if (result.ShouldRepeatTurn)
                    {
                        MessageText += " Продолжайте стрелять!";
                    }
                }
                else
                {
                    cell.Status = CellStatus.Miss;
                    MessageText = "Промах! Ход переходит к противнику.";
                    IsMyTurn = false;
                }

                OnEnemyFieldUpdated?.Invoke();
                UpdateGameStatus();

                if (_enemyHitsCount >= TOTAL_ENEMY_DECKS)
                {
                    IsGameOver = true;
                    IsMyTurn = false;
                    MessageText = "Вы победили! Все корабли противника уничтожены!";
                    MessageBox.Show("Поздравляем! Вы победили!", "Игра окончена",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
        }
        private void MarkCellsAroundDestroyed(int x, int y)
        {
            if (EnemyField == null) return;

            var shipCells = new System.Collections.Generic.List<Cell>();
            var visited = new System.Collections.Generic.HashSet<string>();

            void Dfs(int cx, int cy)
            {
                if (!EnemyField.IsValidCoordinates(cx, cy)) return;

                var key = $"{cx},{cy}";
                if (visited.Contains(key)) return;

                var current = EnemyField.GetCell(cx, cy);
                if (current == null ||
                    (current.Status != CellStatus.ShipHited && current.Status != CellStatus.ShipDestroyed))
                    return;

                visited.Add(key);
                shipCells.Add(current);
                current.Status = CellStatus.ShipDestroyed;

                Dfs(cx + 1, cy);
                Dfs(cx - 1, cy);
                Dfs(cx, cy + 1);
                Dfs(cx, cy - 1);
            }

            Dfs(x, y);

            foreach (var shipCell in shipCells)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = shipCell.X + dx;
                        int ny = shipCell.Y + dy;

                        var neighbor = EnemyField.GetCell(nx, ny);
                        if (neighbor != null && neighbor.Status == CellStatus.Empty)
                        {
                            neighbor.Status = CellStatus.Miss;
                        }
                    }
                }
            }
        }

        private void UpdateGameStatus()
        {
            if (IsGameOver)
            {
                GameStatus = _localPlayer.AllShipsDestroyed() ? "Вы проиграли!" : "Вы победили!";
                CurrentPlayerInfo = "Игра окончена";
            }
            else if (IsMyTurn)
            {
                GameStatus = "Ваш ход";
                CurrentPlayerInfo = "Стреляйте по полю противника";
            }
            else
            {
                GameStatus = "Ход противника";
                CurrentPlayerInfo = "Ожидайте...";
            }

            ConnectionStatus = (_isHostMode && _host != null) || (!_isHostMode && _client != null)
                ? "Подключено" : "Нет подключения";
        }

        private void OnClientConnected(string playerName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageText = $"Подключен: {playerName}";
                _opponentName = playerName;
            });
        }

        private void OnClientDisconnected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageText = "Соперник отключился";
                MessageBox.Show("Соперник отключился от игры", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ExitGame(null);
            });
        }

        private void OnConnected(string playerName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageText = $"Подключен к: {playerName}";
                _opponentName = playerName;
            });
        }

        private void OnDisconnected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageText = "Отключен от хоста";
                MessageBox.Show("Соединение с хостом потеряно", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ExitGame(null);
            });
        }

        private void ExitGame(object parameter)
        {
            if (MessageBox.Show("Выйти из игры?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                OnWindowClosed?.Invoke();
            }
        }

        private void Surrender(object parameter)
        {
            if (MessageBox.Show("Сдаться?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                MessageText = "Вы сдались!";
                MessageBox.Show("Вы сдались!", "Игра окончена",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                OnWindowClosed?.Invoke();
            }
        }

        public event Action OnPlayerFieldUpdated;
        public event Action OnEnemyFieldUpdated;
        public event Action OnWindowClosed;

        
    }
}