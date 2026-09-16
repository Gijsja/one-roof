using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Commands
{
    /// <summary>
    /// Domain command to demolish an existing room and its associated entrance portals.
    /// </summary>
    public sealed class DemolishRoomCommand
    {
        public DemolishRoomCommand(EntityId roomId, bool force = false)
        {
            roomId.EnsureValid();
            RoomId = roomId;
            Force = force;
        }

        public EntityId RoomId { get; }

        public bool Force { get; }

        public override string ToString() => $"DemolishRoom ({RoomId}, Force={Force})";
    }
}
