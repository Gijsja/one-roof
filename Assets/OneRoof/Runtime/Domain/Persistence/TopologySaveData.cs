using System;

namespace OneRoof.Domain.Persistence
{
    /// <summary>
    /// Serializable sub-record capturing building floor slabs, rooms, and portals.
    /// Owned and serialized directly by BuildingTopologyState.
    /// </summary>
    [Serializable]
    public sealed class TopologySaveData
    {
        public FloorSlabSaveData[] floorSlabs;
        public RoomSaveData[] rooms;
        public PortalSaveData[] portals;
    }
}
