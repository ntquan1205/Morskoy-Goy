using System;
using System.Collections.Generic;
using System.Linq;
using Morskoy_Goy.GameLogic.Models;

namespace Morskoy_Goy.GameLogic.Services
{
    public class ShipPlacementService
    {
        private GameFieldLogic _field;
        private List<Ship> _shipsToPlace;
        private Random _random;

        public ShipPlacementService(GameFieldLogic field)
        {
            _field = field;
            _random = new Random();
            InitializeShips();
        }

        private void InitializeShips()
        {
            _shipsToPlace = new List<Ship>
            {
                new Ship(ShipType.FourDeck),
                new Ship(ShipType.TripleDeck),
                new Ship(ShipType.TripleDeck),
                new Ship(ShipType.DoubleDeck),
                new Ship(ShipType.DoubleDeck),
                new Ship(ShipType.DoubleDeck),
                new Ship(ShipType.SingleDeck),
                new Ship(ShipType.SingleDeck),
                new Ship(ShipType.SingleDeck),
                new Ship(ShipType.SingleDeck)
            };
        }

        public bool CanPlaceShip(Ship ship, int startX, int startY, bool isHorizontal)
        {
            if (isHorizontal)
            {
                if (startX + (int)ship.Type > GameFieldLogic.Width) return false;
            }
            else
            {
                if (startY + (int)ship.Type > GameFieldLogic.Height) return false;
            }

            for (int i = 0; i < (int)ship.Type; i++)
            {
                int x = isHorizontal ? startX + i : startX;
                int y = isHorizontal ? startY : startY + i;

                if (!_field.IsValidCoordinates(x, y)) return false;

                var cell = _field.GetCell(x, y);
                if (cell != null && cell.Status != CellStatus.Empty)
                    return false;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int checkX = x + dx;
                        int checkY = y + dy;

                        if (_field.IsValidCoordinates(checkX, checkY))
                        {
                            var neighborCell = _field.GetCell(checkX, checkY);
                            if (neighborCell != null && neighborCell.Status == CellStatus.Ship)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        public bool PlaceShip(Ship ship, int startX, int startY, bool isHorizontal)
        {
            if (!CanPlaceShip(ship, startX, startY, isHorizontal))
                return false;

            for (int i = 0; i < (int)ship.Type; i++)
            {
                int x = isHorizontal ? startX + i : startX;
                int y = isHorizontal ? startY : startY + i;

                var cell = _field.GetCell(x, y);
                if (cell != null)
                {
                    cell.Status = CellStatus.Ship;
                    cell.Ship = ship;
                    ship.OccupiedCells.Add(cell);
                }
            }

            _field.Ships.Add(ship);
            _shipsToPlace.Remove(ship);

            if (_shipsToPlace.Count == 0)
                _field.IsReady = true;

            return true;
        }

        public void PlaceAllShipsRandomly()
        {
            ClearField();
            InitializeShips();

            foreach (var ship in _shipsToPlace.ToList())
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 1000)
                {
                    int x = _random.Next(0, GameFieldLogic.Width);
                    int y = _random.Next(0, GameFieldLogic.Height);
                    bool horizontal = _random.Next(0, 2) == 0;

                    if (CanPlaceShip(ship, x, y, horizontal))
                    {
                        PlaceShip(ship, x, y, horizontal);
                        placed = true;
                    }

                    attempts++;
                }

                if (!placed)
                {
                    ClearField();
                    InitializeShips();
                    PlaceAllShipsRandomly();
                    return;
                }
            }
        }

        private void ClearField()
        {
            for (int x = 0; x < GameFieldLogic.Width; x++)
            {
                for (int y = 0; y < GameFieldLogic.Height; y++)
                {
                    var cell = _field.GetCell(x, y);
                    if (cell != null)
                    {
                        cell.Status = CellStatus.Empty;
                        cell.Ship = null;
                    }
                }
            }

            _field.Ships.Clear();
            _field.IsReady = false;
        }

        public List<Ship> GetShipsToPlace()
        {
            return _shipsToPlace;
        }

        public int ShipsRemaining => _shipsToPlace.Count;

        public bool AllShipsPlaced => _shipsToPlace.Count == 0;
    }
}