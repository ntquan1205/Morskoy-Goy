using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Morskoy_Goy.GameLogic.Models;

namespace Morskoy_Goy.Views
{
    public partial class GameField : UserControl
    {
        private GameFieldLogic _gameFieldLogic;
        private bool _hideShips = false;
        public event Action<int, int> CellClicked;
        public GameField()
        {
            InitializeComponent();
        }

        public void SetGameFieldLogic(GameFieldLogic gameField)
        {
            _gameFieldLogic = gameField;
            UpdateView();
        }
        public void SetHideShips(bool hide)
        {
            _hideShips = hide;
            if (_gameFieldLogic != null)
                UpdateView();
        }
        public void UpdateView()
        {
            FieldGrid.Children.Clear();

            if (_gameFieldLogic == null)
            {
                DrawEmptyField();
                return;
            }

            for (int y = 0; y < 10; y++)  
            {
                for (int x = 0; x < 10; x++)  
                {
                    var cell = CreateCell(x, y);
                    FieldGrid.Children.Add(cell);
                }
            }
        }

        private void DrawEmptyField()
        {
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    var cell = CreateCell(x, y);
                    FieldGrid.Children.Add(cell);
                }
            }
        }

        private Border CreateCell(int x, int y)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0.5),
                Width = 30,
                Height = 30,
                Tag = $"{x},{y}"
            };

            if (_gameFieldLogic != null)
            {
                var cell = _gameFieldLogic.GetCell(x, y);
                if (cell != null)
                {
                    border.Background = GetCellColor(cell);
                }
                else
                {
                    border.Background = Brushes.LightBlue;
                }
            }
            else
            {
                border.Background = Brushes.LightBlue;  
            }

            border.MouseLeftButtonDown += (sender, e) =>
            {
                CellClicked?.Invoke(x, y);
            };

            return border;
        }

        private Brush GetCellColor(Cell cell)
        {
            if (_hideShips && cell.Status == CellStatus.Ship)
            {
                return Brushes.LightBlue;
            }

            switch (cell.Status)
            {
                case CellStatus.Ship:
                    return Brushes.Gray;         
                case CellStatus.ShipHited:
                    return Brushes.Red;           
                case CellStatus.Miss:
                    return Brushes.White;         
                case CellStatus.ShipDestroyed:
                    return Brushes.DarkRed;      
                default: 
                    return Brushes.LightBlue;     
            }
        }
    }
}