using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Commands
{
    /// <summary>Expands the existing ground-floor slab with contiguous buildable cells.</summary>
    public sealed class ExpandGroundSlabCommand : ICommand
    {
        public ExpandGroundSlabCommand(int minX, int maxX)
        {
            MinX = minX;
            MaxX = maxX;
        }

        public int MinX { get; }
        public int MaxX { get; }
        public CellBounds Bounds => new CellBounds(0, MinX, MaxX);
        public override string ToString() => $"ExpandGroundSlab ([{MinX}..{MaxX}])";
    }
}
