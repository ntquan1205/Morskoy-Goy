using System.Collections.Generic;
using System.Linq;

namespace Morskoy_Goy.GameLogic.Models
{
    public class Player
    {
        public GameFieldLogic Field { get; set; }
        public bool IsMyTurn { get; set; }
        public bool IsHost { get; }

        public bool IsReady { get; set; }
        public Player(bool isHost = false)
        {
            Field = new GameFieldLogic();
            IsHost = isHost;
            IsMyTurn = isHost;
        }

        public ShotResult ReceiveShot(int x, int y)
        {
            var cell = Field.GetCell(x, y);
            if (cell == null || cell.Status == CellStatus.Miss ||
                cell.Status == CellStatus.ShipHited || cell.Status == CellStatus.ShipDestroyed)
            {
                return new ShotResult { IsValid = false };
            }

            var result = new ShotResult
            {
                X = x,
                Y = y,
                IsValid = true
            };

            if (cell.Status == CellStatus.Ship)
            {
                cell.Status = CellStatus.ShipHited;
                cell.Ship?.RegisterHit(cell);
                result.IsHit = true;

                if (cell.Ship?.IsDestroyed == true)
                {
                    result.IsShipDestroyed = true;
                    MarkCellsAroundDestroyedShip(cell.Ship);
                }
            }
            else
            {
                cell.Status = CellStatus.Miss;
                result.IsHit = false;
            }

            return result;
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

                        var neighborCell = Field.GetCell(checkX, checkY);
                        if (neighborCell != null && neighborCell.Status == CellStatus.Empty)
                        {
                            neighborCell.Status = CellStatus.Miss;
                        }
                    }
                }
            }
        }

        public bool AllShipsDestroyed()
        {
            return Field.Ships.All(ship => ship.IsDestroyed);
        }
    }

    public class ShotResult
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsValid { get; set; }
        public bool IsHit { get; set; }
        public bool IsShipDestroyed { get; set; }
    }
}