namespace OneRoof.Domain.Commands
{
    /// <summary>Builds a square lair-floor footprint within excavated earth cells.</summary>
    public sealed class BuildUndergroundFloorCommand : ICommand
    {
        public BuildUndergroundFloorCommand(int x, int depth, int size)
        {
            X = x;
            Depth = depth;
            Size = size;
        }

        public int X { get; }
        public int Depth { get; }
        public int Size { get; }
    }
}
