using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;
using Morskoy_Goy.Network.Host;

namespace Morskoy_Goy.Views
{
    public partial class HostWindow : Window
    {
        private GameHost _gameHost;

        public HostWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ShowLocalIpAddress();
        }

        private void ShowLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IpAddressText.Text = ip.ToString();
                        return;
                    }
                }
                IpAddressText.Text = "127.0.0.1 (localhost)";
            }
            catch
            {
                IpAddressText.Text = "127.0.0.1";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainMenu = new MainMenuWindow();
            mainMenu.Show();
            this.Close();
        }

        private async void StartHostButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string playerName = PlayerNameTextBox.Text;
                int port = int.Parse(PortTextBox.Text);

                StartHostButton.IsEnabled = false;
                PlayerNameTextBox.IsEnabled = false;
                PortTextBox.IsEnabled = false;

                StatusBorder.Visibility = Visibility.Visible;
                StatusText.Text = "Сервер запускается...";
                ConnectionInfoText.Text = $"Порт: {port}";

                _gameHost = new GameHost();
                _gameHost.ClientConnected += OnClientConnected;
                _gameHost.ClientDisconnected += OnClientDisconnected;

                await System.Threading.Tasks.Task.Run(() => _gameHost.Start(port, playerName));

                StatusText.Text = "Ожидание подключения соперника...";
                ConnectionInfoText.Text = $"IP: {IpAddressText.Text}:{port}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска сервера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                StartHostButton.IsEnabled = true;
                PlayerNameTextBox.IsEnabled = true;
                PortTextBox.IsEnabled = true;
                StatusBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void OnClientConnected(string clientName)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Соперник подключен: {clientName}";
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

        private void OnClientDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Соперник отключился", "Соединение разорвано");
                BackButton_Click(null, null);
            });
        }

        private void StartGame()
        {
            MessageBox.Show("Игра начинается! (Здесь будет игровое поле)", "Морской Бой");
            BackButton_Click(null, null);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _gameHost?.Stop();
        }
    }
}