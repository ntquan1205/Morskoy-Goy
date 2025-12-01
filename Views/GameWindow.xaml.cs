using System;
using System.Windows;

namespace Morskoy_Goy.Views
{
    public partial class GameWindow : Window
    {
        private string _playerName;
        private string _opponentName;
        private bool _isHost;

        public GameWindow(string playerName, string opponentName = "Соперник", bool isHost = false)
        {
            InitializeComponent();

            _playerName = playerName;
            _opponentName = opponentName;
            _isHost = isHost;

            Loaded += OnWindowLoaded;
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

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                ExitButton_Click(null, null);
            };
            timer.Start();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся в главное меню
            var mainMenu = new MainMenuWindow();
            mainMenu.Show();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Здесь можно добавить отключение от сети
        }
    }
}