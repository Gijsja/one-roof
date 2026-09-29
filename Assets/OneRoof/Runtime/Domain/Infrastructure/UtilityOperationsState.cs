using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Infrastructure
{
    /// <summary>Owns operational wear for installed utility equipment. Physical connectivity remains topology-derived.</summary>
    public sealed class UtilityOperationsState
    {
        public const float FailureThreshold = .30f;
        public const float WearPerTick = .006f;
        public const float RepairPerTechnicianPerTick = .045f;

        private readonly Dictionary<EntityId, float> _conditionByRoom = new Dictionary<EntityId, float>();
        private long _syncedRoomsVersion = long.MinValue;
        private UtilityOperationsSnapshot _cachedSnapshot;

        public void Advance(BuildingTopologyState topology, PopulationState population, float undergroundRepairBoost = 0f)
        {
            if (topology == null) return;
            SyncEquipmentIfTopologyChanged(topology);
            var technicians = CountTechnicians(population);
            foreach (var room in topology.Rooms.Values)
            {
                if (!IsUtilityEquipment(room)) continue;
                var condition = _conditionByRoom[room.Id];
                if (condition <= FailureThreshold && technicians > 0)
                    condition = Math.Min(1f, condition + technicians * RepairPerTechnicianPerTick + Math.Max(0f, undergroundRepairBoost));
                else
                    condition = Math.Max(0f, condition - WearPerTick);
                _conditionByRoom[room.Id] = condition;
            }
            _cachedSnapshot = null;
        }

        public UtilityOperationsSnapshot Snapshot(BuildingTopologyState topology)
        {
            if (topology == null) return UtilityOperationsSnapshot.Empty;
            SyncEquipmentIfTopologyChanged(topology);
            if (_cachedSnapshot != null) return _cachedSnapshot;
            var equipment = new List<UtilityEquipmentProjection>();
            foreach (var room in topology.Rooms.Values)
            {
                if (!IsUtilityEquipment(room)) continue;
                var condition = _conditionByRoom[room.Id];
                equipment.Add(new UtilityEquipmentProjection(room.Id.Value, room.Floor, room.ContentType.Value, condition, condition <= FailureThreshold));
            }
            equipment.Sort((a, b) => a.RoomId.CompareTo(b.RoomId));
            _cachedSnapshot = new UtilityOperationsSnapshot(equipment);
            return _cachedSnapshot;
        }

        private void SyncEquipment(BuildingTopologyState topology)
        {
            var removed = new List<EntityId>();
            foreach (var id in _conditionByRoom.Keys)
                if (!topology.Rooms.ContainsKey(id) || !IsUtilityEquipment(topology.Rooms[id])) removed.Add(id);
            for (var i = 0; i < removed.Count; i++) _conditionByRoom.Remove(removed[i]);
            foreach (var room in topology.Rooms.Values)
                if (IsUtilityEquipment(room) && !_conditionByRoom.ContainsKey(room.Id)) _conditionByRoom.Add(room.Id, 1f);
        }

        private void SyncEquipmentIfTopologyChanged(BuildingTopologyState topology)
        {
            if (_syncedRoomsVersion == topology.RoomsVersion) return;
            SyncEquipment(topology);
            _syncedRoomsVersion = topology.RoomsVersion;
            _cachedSnapshot = null;
        }

        public UtilityOperationsSaveData ToSaveData()
        {
            var ids = new List<EntityId>(_conditionByRoom.Keys);
            ids.Sort((a, b) => a.Value.CompareTo(b.Value));
            var equipment = new List<UtilityEquipmentSaveData>(ids.Count);
            for (var i = 0; i < ids.Count; i++)
                equipment.Add(new UtilityEquipmentSaveData { roomId = ids[i].Value, condition = _conditionByRoom[ids[i]] });
            return new UtilityOperationsSaveData { equipment = equipment.ToArray() };
        }

        public static UtilityOperationsState FromSaveData(UtilityOperationsSaveData data)
        {
            var state = new UtilityOperationsState();
            if (data?.equipment == null) return state;
            for (var i = 0; i < data.equipment.Length; i++)
            {
                var entry = data.equipment[i];
                if (entry == null) continue;
                state._conditionByRoom[new EntityId(entry.roomId)] = Math.Max(0f, Math.Min(1f, entry.condition));
            }
            return state;
        }

        private static int CountTechnicians(PopulationState population)
        {
            if (population == null) return 0;
            var count = 0;
            for (var i = 0; i < population.Persons.Count; i++)
                if (population.Persons[i].Specialization.Role == SpecialistRole.Maintenance) count++;
            return count;
        }

        private static bool IsUtilityEquipment(Room room)
        {
            var id = room.ContentType;
            return id == ElectricalGridState.SubstationContentId || id == ElectricalGridState.RiserContentId || id == ElectricalGridState.TransformerContentId ||
                   id == WaterWasteNetworkState.WaterPumpContentId || id == WaterWasteNetworkState.WaterRiserContentId || id == WaterWasteNetworkState.BoosterPumpContentId ||
                   id == WaterWasteNetworkState.WasteChuteContentId || id == WaterWasteNetworkState.WasteCollectionContentId;
        }
    }

    public sealed class UtilityOperationsSnapshot
    {
        public static readonly UtilityOperationsSnapshot Empty = new UtilityOperationsSnapshot(Array.Empty<UtilityEquipmentProjection>());
        private readonly HashSet<int> _failedRoomIds = new HashSet<int>();

        public UtilityOperationsSnapshot(IReadOnlyList<UtilityEquipmentProjection> equipment)
        {
            Equipment = equipment ?? Array.Empty<UtilityEquipmentProjection>();
            for (var i = 0; i < Equipment.Count; i++)
                if (Equipment[i].IsFailed) _failedRoomIds.Add(Equipment[i].RoomId);
        }

        public IReadOnlyList<UtilityEquipmentProjection> Equipment { get; }

        public bool IsFailed(int roomId) => _failedRoomIds.Contains(roomId);
    }

    public readonly struct UtilityEquipmentProjection
    {
        public UtilityEquipmentProjection(int roomId, int floor, string contentId, float condition, bool isFailed)
        { RoomId = roomId; Floor = floor; ContentId = contentId ?? string.Empty; Condition = condition; IsFailed = isFailed; }
        public int RoomId { get; }
        public int Floor { get; }
        public string ContentId { get; }
        public float Condition { get; }
        public bool IsFailed { get; }
    }
}
