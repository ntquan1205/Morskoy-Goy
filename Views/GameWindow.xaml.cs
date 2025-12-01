using System;
using System.Windows;

namespace Morskoy_Goy.Views
{
    public partial class GameWindow : Window
    {
        private string _playerName;
        private string _opponentName;
        private bool _isHost;

        private object _networkObject; 

        public GameWindow(string playerName, string opponentName, bool isHost, object networkObject)
        {
            InitializeComponent();

            _playerName = playerName;
            _opponentName = opponentName;
            _isHost = isHost;
            _networkObject = networkObject;

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            PlayerInfoText.Text = $"Вы: {_playerName} | Соперник: {_opponentName}";

            if (_isHost)
            {
                GameStatusText.Text = "Вы - ХОСТ игры";
                ConnectionStatusText.Text = "Хост (Сервер)";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                GameStatusText.Text = "Вы - КЛИЕНТ";
                ConnectionStatusText.Text = "Клиент";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Blue;
            }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isHost && _networkObject is Network.Host.GameHost host)
            {
                host.Stop();
            }
            else if (!_isHost && _networkObject is Network.Client.GameClient client)
            {
                client.Disconnect();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); 
        }
    }
}