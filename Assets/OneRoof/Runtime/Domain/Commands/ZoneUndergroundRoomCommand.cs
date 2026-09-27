using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Commands
{
    public sealed class ZoneUndergroundRoomCommand : ICommand
    {
        public ZoneUndergroundRoomCommand(UndergroundRoomType type, int x, int depth, int width, int height)
        { Type = type; X = x; Depth = depth; Width = width; Height = height; }
        public UndergroundRoomType Type { get; }
        public int X { get; }
        public int Depth { get; }
        public int Width { get; }
        public int Height { get; }
    }
}
