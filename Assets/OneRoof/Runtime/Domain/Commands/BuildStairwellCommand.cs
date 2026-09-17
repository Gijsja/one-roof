using System;

namespace OneRoof.Domain.Commands
{
    /// <summary>
    /// Domain command to construct a stairwell column spanning a range of contiguous floors,
    /// creating stair landing portals and vertical walk edges.
    /// </summary>
    public sealed class BuildStairwellCommand : ICommand
    {
        public BuildStairwellCommand(int stairMinX, int stairMaxX, int bottomFloor, int topFloor)
        {
            if (stairMinX > stairMaxX)
            {
                throw new ArgumentException($"stairMinX ({stairMinX}) cannot be greater than stairMaxX ({stairMaxX}).", nameof(stairMinX));
            }

            if (bottomFloor >= topFloor)
            {
                throw new ArgumentException($"bottomFloor ({bottomFloor}) must be strictly less than topFloor ({topFloor}).", nameof(bottomFloor));
            }

            StairMinX = stairMinX;
            StairMaxX = stairMaxX;
            BottomFloor = bottomFloor;
            TopFloor = topFloor;
        }

        public int StairMinX { get; }

        public int StairMaxX { get; }

        public int BottomFloor { get; }

        public int TopFloor { get; }

        public int FloorSpan => TopFloor - BottomFloor + 1;

        public override string ToString() => $"BuildStairwell at [{StairMinX}..{StairMaxX}] floors {BottomFloor}..{TopFloor}";
    }
}
