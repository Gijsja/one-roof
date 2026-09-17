using System;

namespace OneRoof.Domain.Commands
{
    /// <summary>
    /// Domain command to construct an elevator shaft spanning a range of contiguous floors,
    /// creating shaft portals and updating the vertical transit network.
    /// </summary>
    public sealed class AddElevatorShaftCommand : ICommand
    {
        public AddElevatorShaftCommand(
            int shaftMinX,
            int shaftMaxX,
            int bottomFloor,
            int topFloor,
            int carCapacity = 10)
        {
            if (shaftMinX > shaftMaxX)
            {
                throw new ArgumentException($"shaftMinX ({shaftMinX}) cannot be greater than shaftMaxX ({shaftMaxX}).", nameof(shaftMinX));
            }

            if (bottomFloor >= topFloor)
            {
                throw new ArgumentException($"bottomFloor ({bottomFloor}) must be strictly less than topFloor ({topFloor}).", nameof(bottomFloor));
            }

            if (carCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(carCapacity), carCapacity, "Car capacity must be positive.");
            }

            ShaftMinX = shaftMinX;
            ShaftMaxX = shaftMaxX;
            BottomFloor = bottomFloor;
            TopFloor = topFloor;
            CarCapacity = carCapacity;
        }

        public int ShaftMinX { get; }

        public int ShaftMaxX { get; }

        public int BottomFloor { get; }

        public int TopFloor { get; }

        public int CarCapacity { get; }

        public int FloorSpan => TopFloor - BottomFloor + 1;

        public override string ToString() => $"AddElevatorShaft at [{ShaftMinX}..{ShaftMaxX}] floors {BottomFloor}..{TopFloor} (Cap: {CarCapacity})";
    }
}
