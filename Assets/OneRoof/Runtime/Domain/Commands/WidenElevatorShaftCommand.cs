using System;

namespace OneRoof.Domain.Commands
{
    public enum ElevatorShaftWideningSide
    {
        Left = 0,
        Right = 1
    }

    /// <summary>Widens an aligned elevator shaft by one cell on the selected side.</summary>
    public sealed class WidenElevatorShaftCommand : ICommand
    {
        public WidenElevatorShaftCommand(int bottomFloor, int topFloor, ElevatorShaftWideningSide side)
        {
            if (bottomFloor < 0) throw new ArgumentOutOfRangeException(nameof(bottomFloor));
            if (topFloor < bottomFloor) throw new ArgumentOutOfRangeException(nameof(topFloor));
            if (side != ElevatorShaftWideningSide.Left && side != ElevatorShaftWideningSide.Right)
                throw new ArgumentOutOfRangeException(nameof(side));

            BottomFloor = bottomFloor;
            TopFloor = topFloor;
            Side = side;
        }

        public int BottomFloor { get; }
        public int TopFloor { get; }
        public ElevatorShaftWideningSide Side { get; }
        public int FloorSpan => TopFloor - BottomFloor + 1;

        public override string ToString() => $"Widen elevator shaft on {Side} across floors {BottomFloor}..{TopFloor}";
    }
}
