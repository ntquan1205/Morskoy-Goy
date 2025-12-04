using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Morskoy_Goy.GameLogic.Models
{
    public class GameFieldLogic
    {
        public const int Width = 10;
        public const int Height = 10;

        public Cell[,] Cells { get; set; }
        public List<Ship> Ships { get; set; }
        public bool IsReady;

        public GameFieldLogic()
        {
            Cells = new Cell[Width, Height];
            Ships = new List<Ship>();
            InitializeEmptyField();
        }
        private void InitializeEmptyField()
        {
            for (int x = 0; x < Width; x++)
            {
                    for (int y = 0; y < Height; y++)
                    {
                            Cells[x, y] = new Cell(x, y);
                    }
            }
        }
        public Cell GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return Cells[x, y];
        }
        public bool IsValidCoordinates(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}
