namespace OneRoof.Domain.Commands
{
    public sealed class BuildUndergroundCoreCommand : ICommand
    {
        public BuildUndergroundCoreCommand(int x, int depth)
        { X = x; Depth = depth; }
        public int X { get; }
        public int Depth { get; }
    }
}
