using System.Windows;
using Morskoy_Goy.ViewModels;

namespace Morskoy_Goy.Views
{
    public partial class GameWindow : Window
    {
        private GameViewModel _viewModel;

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

            _viewModel = new GameViewModel(
                playerName,
                opponentName,
                isHost,
                networkObject,
                playerField
            );

            DataContext = _viewModel;

            _viewModel.OnPlayerFieldUpdated += UpdatePlayerField;
            _viewModel.OnEnemyFieldUpdated += UpdateEnemyField;
            _viewModel.OnWindowClosed += CloseWindow;

            InitializeFields();

            EnemyField.CellClicked += (x, y) =>
                _viewModel.EnemyCellClickCommand?.Execute($"{x},{y}");
        }

        private void InitializeFields()
        {
            if (_viewModel == null) return;

            MyField.SetGameFieldLogic(_viewModel.LocalPlayer.Field);
            MyField.SetHideShips(false);

            EnemyField.SetGameFieldLogic(_viewModel.EnemyField);
            EnemyField.SetHideShips(true);

            MyField.UpdateView();
            EnemyField.UpdateView();
        }

        private void UpdatePlayerField()
        {
            MyField.UpdateView();
        }

        private void UpdateEnemyField()
        {
            EnemyField.UpdateView();
        }

        private void CloseWindow()
        {
            this.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ExitCommand.Execute(null);
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);

            if (_viewModel != null)
            {
                _viewModel.OnPlayerFieldUpdated -= UpdatePlayerField;
                _viewModel.OnEnemyFieldUpdated -= UpdateEnemyField;
                _viewModel.OnWindowClosed -= CloseWindow;
                _viewModel.Cleanup();
            }
        }
    }
}