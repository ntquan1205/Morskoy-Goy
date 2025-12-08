using Morskoy_Goy.Network.Client;
using Morskoy_Goy.ViewModels.Base;
using Morskoy_Goy.ViewModels.Common;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Morskoy_Goy.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        private GameClient _gameClient;
        private DispatcherTimer _connectionTimer;

        private string _playerName = "Игрок 2";
        public string PlayerName
        {
            get => _playerName;
            set => Set(ref _playerName, value);
        }

        private string _hostIp = "127.0.0.1";
        public string HostIp
        {
            get => _hostIp;
            set => Set(ref _hostIp, value);
        }

        private string _port = "8888";
        public string Port
        {
            get => _port;
            set => Set(ref _port, value);
        }

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        private string _connectionInfo = "";
        public string ConnectionInfo
        {
            get => _connectionInfo;
            set => Set(ref _connectionInfo, value);
        }

        private bool _isStatusVisible = false;
        public bool IsStatusVisible
        {
            get => _isStatusVisible;
            set => Set(ref _isStatusVisible, value);
        }

        private bool _isControlsEnabled = true;
        public bool IsControlsEnabled
        {
            get => _isControlsEnabled;
            set => Set(ref _isControlsEnabled, value);
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set => Set(ref _isConnected, value);
        }

        public RelayCommand ConnectCommand { get; }
        public RelayCommand BackCommand { get; }

        // События для навигации
        public event Action OnBackRequested;
        public event Action OnGameStarted;

        public ClientViewModel()
        {
            ConnectCommand = new RelayCommand(ConnectToHost);
            BackCommand = new RelayCommand(GoBack);
        }

        private async void ConnectToHost(object parameter)
        {
            try
            {
                if (!int.TryParse(Port, out int portNumber) || portNumber < 1 || portNumber > 65535)
                {
                    MessageBox.Show("Введите корректный порт (1-65535)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(PlayerName))
                {
                    MessageBox.Show("Введите ваше имя", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(HostIp))
                {
                    MessageBox.Show("Введите IP адрес хоста", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                IsControlsEnabled = false;
                IsStatusVisible = true;
                StatusText = "Подключение к хосту...";
                ConnectionInfo = $"{HostIp}:{portNumber}";

                _gameClient = new GameClient();
                _gameClient.Connected += OnConnected;
                _gameClient.Disconnected += OnDisconnected;

                await _gameClient.Connect(HostIp, portNumber, PlayerName);

                StatusText = "Подключение установлено!";
                ConnectionInfo = "Ожидаем начала игры...";
                IsConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                ResetControls();
            }
        }

        private void OnConnected(string hostName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Подключено к хосту: {hostName}";
                ConnectionInfo = "Игра начинается...";

                // Запускаем таймер для перехода к игре
                _connectionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _connectionTimer.Tick += (s, e) =>
                {
                    _connectionTimer.Stop();
                    StartGame();
                };
                _connectionTimer.Start();
            });
        }

        private void OnDisconnected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Соединение с хостом разорвано", "Отключено");
                GoBack(null);
            });
        }

        private void StartGame()
        {
            OnGameStarted?.Invoke();
        }

        private void GoBack(object parameter)
        {
            Disconnect();
            OnBackRequested?.Invoke();
        }

        private void Disconnect()
        {
            IsConnected = false;
            _gameClient?.Disconnect();
            ResetControls();
        }

        private void ResetControls()
        {
            IsControlsEnabled = true;
            IsStatusVisible = false;
            IsConnected = false;
        }

        public void Cleanup()
        {
            Disconnect();
        }
    }
}