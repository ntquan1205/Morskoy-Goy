using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Morskoy_Goy.Models;

namespace Morskoy_Goy.Views
{
    public partial class GameField : UserControl
    {
        private Models.GameField _gameFieldLogic;
        private bool _hideShips = false;
        public event Action<int, int> CellClicked;

        public GameField()
        {
            InitializeComponent();
            _gameFieldLogic = new Models.GameField();
        }
        public Models.GameField? GetFieldLogic()
        {
            return _gameFieldLogic;
        }

        public CellStatus? GetCellState(int x, int y)
        {
            if (_gameFieldLogic == null) return null;

            var cell = _gameFieldLogic.GetCell(x, y);
            return cell?.Status;
        }
        
        public void SetGameFieldLogic(Models.GameField gameField)
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

       

        public (bool isHit, bool isDestroyed) ProcessShot(int x, int y)
        {
            if (_gameFieldLogic == null) return (false, false);

            var cell = _gameFieldLogic.GetCell(x, y);
            if (cell == null) return (false, false);

            
            if (cell.Status == CellStatus.Miss ||
                cell.Status == CellStatus.ShipHited ||
                cell.Status == CellStatus.ShipDestroyed)
                return (false, false);

            bool isHit = false;
            bool isDestroyed = false;

            
            if (cell.Status == CellStatus.Ship)
            {
                
                cell.Status = CellStatus.ShipHited;
                cell.Ship?.RegisterHit(cell);
                isHit = true;

                if (cell.Ship?.IsDestroyed == true)
                {
                    isDestroyed = true;
                    cell.Status = CellStatus.ShipDestroyed;
                    MarkCellsAroundDestroyedShip(cell.Ship);
                }
            }
            else if (cell.Status == CellStatus.Empty)
            {
                
                cell.Status = CellStatus.Miss;
                isHit = false;
            }
            else
            {
                
                return (false, false);
            }

            UpdateCellView(x, y);
            return (isHit, isDestroyed);
        }

        private void MarkCellsAroundDestroyedShip(Ship ship)
        {
            foreach (var cell in ship.OccupiedCells)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int checkX = cell.X + dx;
                        int checkY = cell.Y + dy;

                        var neighborCell = _gameFieldLogic.GetCell(checkX, checkY);
                        if (neighborCell != null && neighborCell.Status == CellStatus.Empty)
                        {
                            neighborCell.Status = CellStatus.Miss;
                            UpdateCellView(checkX, checkY);
                        }
                    }
                }
            }
        }

        private void UpdateCellView(int x, int y)
        {
            foreach (var child in FieldGrid.Children)
            {
                if (child is Border border)
                {
                    var tag = border.Tag as string;
                    if (tag == $"{x},{y}")
                    {
                        var cell = _gameFieldLogic.GetCell(x, y);
                        border.Background = GetCellColor(cell);
                        return;
                    }
                }
            }
        }

        public bool AllShipsDestroyed()
        {
            return _gameFieldLogic?.Ships?.All(ship => ship.IsDestroyed) ?? false;
        }
    }
}