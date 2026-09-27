namespace OneRoof.Domain.Commands
{
    public sealed class BuildUndergroundCorridorCommand : ICommand
    {
        public BuildUndergroundCorridorCommand(int x, int depth, int width)
        { X = x; Depth = depth; Width = width; }
        public int X { get; }
        public int Depth { get; }
        public int Width { get; }
    }
}
