using Morskoy_Goy.Models;
using Morskoy_Goy.Network.Client;
using Morskoy_Goy.Network.Common;
using Morskoy_Goy.Network.Host;
using Morskoy_Goy.Services;
using Morskoy_Goy.ViewModels.Base;
using Morskoy_Goy.ViewModels.Common;
using System;
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
            _host.IncomingShotReceived += ProcessIncomingShot;

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

            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.ShotResultReceived += OnShotResultReceived;

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

            // Проверяем, можно ли стрелять в эту клетку
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
            }
            else if (_client != null)
            {
                _client.SendShot(x, y);
                shotSent = true;
            }

            if (shotSent)
            {
                MessageText = $"Выстрел в ({x}, {y}) отправлен...";
                IsMyTurn = false;
            }
            else
            {
                MessageText = "Нет подключения к противнику!";
            }
        }

        private void OnShotResultReceived(ShotResultData result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var cell = EnemyField.GetCell(result.X, result.Y);
                if (cell == null) return;

                // Проверяем, не стреляли ли уже в эту клетку
                if (cell.Status == CellStatus.Miss ||
                    cell.Status == CellStatus.ShipHited ||
                    cell.Status == CellStatus.ShipDestroyed)
                {
                    return;
                }

                if (result.IsHit)
                {
                    _enemyHitsCount++;
                    cell.Status = result.IsShipDestroyed ?
                        CellStatus.ShipDestroyed : CellStatus.ShipHited;

                    if (result.IsShipDestroyed)
                    {
                        MarkCellsAround(result.X, result.Y);
                        MessageText = $"Корабль противника потоплен! Попаданий: {_enemyHitsCount}/{TOTAL_ENEMY_DECKS}";
                    }
                    else
                    {
                        MessageText = $"Попадание! Попаданий: {_enemyHitsCount}/{TOTAL_ENEMY_DECKS}";
                    }

                    IsMyTurn = result.ShouldRepeatTurn;
                }
                else
                {
                    cell.Status = CellStatus.Miss;
                    MessageText = "Промах!";
                    IsMyTurn = false;
                }

                // Проверяем победу
                if (_enemyHitsCount >= TOTAL_ENEMY_DECKS)
                {
                    IsGameOver = true;
                    IsMyTurn = false;
                    MessageText = "Вы победили! Все корабли противника уничтожены!";
                    MessageBox.Show("Поздравляем! Вы победили!", "Игра окончена",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                UpdateGameStatus();
                OnEnemyFieldUpdated?.Invoke();
            });
        }

        public void ProcessIncomingShot(int x, int y)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var cell = _localPlayer.Field.GetCell(x, y);
                if (cell == null) return;

                var shotResult = _localPlayer.ReceiveShot(x, y);

                if (shotResult.IsValid)
                {
                    if (shotResult.IsHit)
                    {
                        MessageText = shotResult.IsShipDestroyed ?
                            "Ваш корабль потоплен!" : "Попадание по вашему кораблю!";
                        IsMyTurn = false;
                    }
                    else
                    {
                        MessageText = "Противник промахнулся!";
                        IsMyTurn = true;
                    }

                    // Проверяем поражение
                    if (_localPlayer.AllShipsDestroyed())
                    {
                        IsGameOver = true;
                        IsMyTurn = false;
                        MessageBox.Show("Вы проиграли! Все ваши корабли уничтожены!", "Игра окончена",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                UpdateGameStatus();
                OnPlayerFieldUpdated?.Invoke();
            });
        }

        private void MarkCellsAround(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;

                    var cell = EnemyField.GetCell(checkX, checkY);
                    if (cell != null && cell.Status == CellStatus.Empty)
                    {
                        cell.Status = CellStatus.Miss;
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

        public void Cleanup()
        {
            if (_isHostMode && _host != null)
                _host.Stop();
            else if (_client != null)
                _client.Disconnect();
        }
    }
}