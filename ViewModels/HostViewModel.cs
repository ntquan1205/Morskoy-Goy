using Morskoy_Goy.Network.Host;
using Morskoy_Goy.ViewModels.Base;
using Morskoy_Goy.ViewModels.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;

namespace Morskoy_Goy.ViewModels
{
    public class HostViewModel : ViewModelBase
    {
        private GameHost _gameHost;
        private DispatcherTimer _connectionTimer;

        private string _playerName = "Игрок 1";
        public string PlayerName
        {
            get => _playerName;
            set => Set(ref _playerName, value);
        }

        private string _port = "8888";
        public string Port
        {
            get => _port;
            set => Set(ref _port, value);
        }

        private string _ipAddress = "Загрузка...";
        public string IpAddress
        {
            get => _ipAddress;
            set => Set(ref _ipAddress, value);
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

        private bool _isHostRunning = false;
        public bool IsHostRunning
        {
            get => _isHostRunning;
            set => Set(ref _isHostRunning, value);
        }

        public RelayCommand StartHostCommand { get; }
        public RelayCommand BackCommand { get; }

        // События для навигации
        public event Action OnBackRequested;
        public event Action OnGameStarted;

        public HostViewModel()
        {
            StartHostCommand = new RelayCommand(StartHost);
            BackCommand = new RelayCommand(GoBack);

            LoadLocalIpAddress();
        }

        private void LoadLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IpAddress = ip.ToString();
                        return;
                    }
                }
                IpAddress = "127.0.0.1 (localhost)";
            }
            catch
            {
                IpAddress = "127.0.0.1";
            }
        }

        private async void StartHost(object parameter)
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

                IsControlsEnabled = false;
                IsStatusVisible = true;
                StatusText = "Сервер запускается...";
                ConnectionInfo = $"Порт: {portNumber}";

                _gameHost = new GameHost();
                _gameHost.ClientConnected += OnClientConnected;
                _gameHost.ClientDisconnected += OnClientDisconnected;

                // Запускаем хост в отдельном потоке
                await System.Threading.Tasks.Task.Run(() => _gameHost.Start(portNumber, PlayerName));

                IsHostRunning = true;
                StatusText = "Ожидание подключения соперника...";
                ConnectionInfo = $"IP: {IpAddress}:{portNumber}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска сервера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                ResetControls();
            }
        }

        private void OnClientConnected(string clientName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Соперник подключен: {clientName}";
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

        private void OnClientDisconnected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Соперник отключился", "Соединение разорвано");
                GoBack(null);
            });
        }

        private void StartGame()
        {
            OnGameStarted?.Invoke();
        }

        private void GoBack(object parameter)
        {
            StopHost();
            OnBackRequested?.Invoke();
        }

        private void StopHost()
        {
            IsHostRunning = false;
            _gameHost?.Stop();
            ResetControls();
        }

        private void ResetControls()
        {
            IsControlsEnabled = true;
            IsStatusVisible = false;
            IsHostRunning = false;
        }

        public void Cleanup()
        {
            StopHost();
        }
    }
}