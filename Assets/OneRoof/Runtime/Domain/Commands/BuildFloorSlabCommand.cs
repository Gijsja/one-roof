using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Commands
{
    /// <summary>
    /// Domain command to construct a horizontal concrete floor slab, establishing buildable cell range.
    /// </summary>
    public sealed class BuildFloorSlabCommand : ICommand
    {
        public BuildFloorSlabCommand(int floorLevel, int minX, int maxX)
        {
            FloorLevel = floorLevel;
            MinX = minX;
            MaxX = maxX;
        }

        public int FloorLevel { get; }

        public int MinX { get; }

        public int MaxX { get; }

        public CellBounds Bounds => new CellBounds(FloorLevel, MinX, MaxX);

        public override string ToString() => $"BuildFloorSlab (Floor {FloorLevel}: [{MinX}..{MaxX}])";
    }
}
