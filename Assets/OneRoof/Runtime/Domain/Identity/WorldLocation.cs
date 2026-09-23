using System;

namespace OneRoof.Domain.Identity
{
    public enum WorldLocationKind { Room, Outside }

    /// <summary>A room in the tower or the persistent street-side world endpoint.</summary>
    public readonly struct WorldLocation : IEquatable<WorldLocation>
    {
        private readonly EntityId _roomId;

        private WorldLocation(WorldLocationKind kind, EntityId roomId)
        {
            Kind = kind;
            _roomId = roomId;
        }

        public WorldLocationKind Kind { get; }
        public EntityId RoomId => IsOutside ? throw new InvalidOperationException("Outside has no room ID.") : _roomId;
        public bool IsOutside => Kind == WorldLocationKind.Outside;
        public static WorldLocation Outside => new WorldLocation(WorldLocationKind.Outside, default);
        public static WorldLocation InRoom(EntityId roomId)
        {
            roomId.EnsureValid();
            return new WorldLocation(WorldLocationKind.Room, roomId);
        }

        public bool Equals(WorldLocation other) => Kind == other.Kind && (IsOutside || _roomId.Equals(other._roomId));
        public override bool Equals(object obj) => obj is WorldLocation other && Equals(other);
        public override int GetHashCode() => IsOutside ? -1 : RoomId.GetHashCode();
        public override string ToString() => IsOutside ? "Outside" : $"Room {RoomId}";
    }
}
