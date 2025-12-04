using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Morskoy_Goy.Views
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class GameField : UserControl
    {
        public GameField()
        {
            InitializeComponent();
            DrawGameField();
        }
        private void DrawGameField()
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    var cell = CreateCell(col, row);
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
                Background = Brushes.LightBlue,   
                Margin = new Thickness(0.5),     
                Width = 30,                       
                Height = 30,                
                Tag = $"{x},{y}"                  
            };

            border.MouseLeftButtonDown += (sender, e) =>
            {
                border.Background = Brushes.Gray;

                MessageBox.Show($"Вы кликнули на клетку [{x},{y}]");
            };

            return border;
        }
    }
}