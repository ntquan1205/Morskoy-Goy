using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Morskoy_Goy.Models
{
    public class Ship
    {
        public ShipType Type { get; set; }
        public List<Cell> OccupiedCells { get; set; }  
        public List<Cell> HitedCells { get; set; }    
        public bool IsDestroyed => OccupiedCells.All(cell => HitedCells.Contains(cell));

        public Ship(ShipType type)
        {
            Type = type;
            OccupiedCells = new List<Cell>();
            HitedCells = new List<Cell>();
        }

        public void AddCell(Cell cell)
        {
            OccupiedCells.Add(cell);
            cell.Ship = this;  
        }

        public void RegisterHit(Cell cell)
        {
            if (OccupiedCells.Contains(cell) && !HitedCells.Contains(cell))
            {
                HitedCells.Add(cell);
            }
        }
    }

    public enum ShipType
    {
        SingleDeck = 1,
        DoubleDeck = 2,
        TripleDeck = 3,
        FourDeck = 4,
    }
}
