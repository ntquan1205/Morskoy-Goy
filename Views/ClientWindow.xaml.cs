using Morskoy_Goy.Network;
using Morskoy_Goy.Network.Client;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Morskoy_Goy.Views
{
    public partial class ClientWindow : Window
    {
        private GameClient _gameClient;

        public ClientWindow()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainMenu = new MainMenuWindow();
            mainMenu.Show();
            this.Close();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string playerName = PlayerNameTextBox.Text;
                string hostIp = HostIpTextBox.Text;
                int port = int.Parse(PortTextBox.Text);

                ConnectButton.IsEnabled = false;
                PlayerNameTextBox.IsEnabled = false;
                HostIpTextBox.IsEnabled = false;
                PortTextBox.IsEnabled = false;

                StatusBorder.Visibility = Visibility.Visible;
                StatusText.Text = "Подключение к хосту...";
                ConnectionInfoText.Text = $"{hostIp}:{port}";

                _gameClient = new GameClient();
                _gameClient.Connected += OnConnected;
                _gameClient.Disconnected += OnDisconnected;

                await _gameClient.Connect(hostIp, port, playerName);

                StatusText.Text = "Подключение установлено!";
                ConnectionInfoText.Text = "Ожидаем начала игры...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                ConnectButton.IsEnabled = true;
                PlayerNameTextBox.IsEnabled = true;
                HostIpTextBox.IsEnabled = true;
                PortTextBox.IsEnabled = true;
                StatusBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void OnConnected(string hostName)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Подключено к хосту: {hostName}";
                ConnectionInfoText.Text = "Игра начинается...";

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    StartGame();
                };
                timer.Start();
            });
        }

        private void OnDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Соединение с хостом разорвано", "Отключено");
                BackButton_Click(null, null);
            });
        }

        private void StartGame()
        {
            MessageBox.Show("Игра начинается!", "Морской Гой");
            BackButton_Click(null, null);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _gameClient?.Disconnect();
        }
    }
}