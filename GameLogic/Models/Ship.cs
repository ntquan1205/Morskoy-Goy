using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morskoy_Goy.GameLogic.Models
{
    public class Ship
    {
        public ShipType Type { get; set; }
        public List<Cell> HitedCells { get; set; }
        public bool IsDestroyed { get; set; }

        public Ship(ShipType type) {
            Type = type;
            HitedCells = new List<Cell>();
            IsDestroyed = false;
        }
    }
    public enum ShipType
    {
        SingleDeck,
        DoubleDeck,
        TripleDeck,
        FourDeck,
    }
}
