using System;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Commands
{
    /// <summary>
    /// Domain command to construct a functional room on a floor within valid slab bounds.
    /// </summary>
    public sealed class BuildRoomCommand : ICommand
    {
        public BuildRoomCommand(
            int floor,
            int minX,
            int maxX,
            ContentId contentType,
            int capacity,
            int? portalX = null)
        {
            contentType.EnsureValid();

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity cannot be negative.");
            }

            Floor = floor;
            MinX = minX;
            MaxX = maxX;
            ContentType = contentType;
            Capacity = capacity;
            PortalX = portalX;
        }

        public int Floor { get; }

        public int MinX { get; }

        public int MaxX { get; }

        public ContentId ContentType { get; }

        public int Capacity { get; }

        public int? PortalX { get; }

        public CellBounds Bounds => new CellBounds(Floor, MinX, MaxX);

        public override string ToString() => $"BuildRoom ({ContentType}) at Floor {Floor}: [{MinX}..{MaxX}] (Cap: {Capacity})";
    }
}
