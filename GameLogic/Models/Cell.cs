using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Morskoy_Goy.GameLogic.Models
{
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public CellStatus Status { get; set; }
        public Ship Ship { get; set; }

        public Cell(int x, int y )
        {
            X = x;
            Y = y;
            Status = CellStatus.Empty;
        }
    }
    public enum CellStatus
    {
        Empty,
        Ship,
        ShipHited,
        Miss,
        ShipDestroyed,
    }
}
