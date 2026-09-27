namespace OneRoof.Domain.Commands
{
    /// <summary>Excavates a square brush of earth beneath the building.</summary>
    public sealed class DigUndergroundCommand : ICommand
    {
        public DigUndergroundCommand(int x, int depth, int size)
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
