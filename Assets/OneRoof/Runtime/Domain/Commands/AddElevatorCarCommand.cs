using System;

namespace OneRoof.Domain.Commands
{
    /// <summary>
    /// Domain command to add a new elevator car to the building's elevator bank.
    /// </summary>
    public sealed class AddElevatorCarCommand : ICommand
    {
        public AddElevatorCarCommand(int capacity = 10, int startingFloor = 0, int cellX = 0)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
            }

            Capacity = capacity;
            StartingFloor = startingFloor;
            CellX = cellX;
        }

        public int Capacity { get; }
        public int StartingFloor { get; }
        public int CellX { get; }

        public override string ToString() => $"AddElevatorCar at Floor {StartingFloor}, CellX {CellX} (Cap: {Capacity})";
    }
}
