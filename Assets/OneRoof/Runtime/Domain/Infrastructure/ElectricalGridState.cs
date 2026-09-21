using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Infrastructure
{
    /// <summary>Derives the physical electrical network from topology: ground source, continuous riser, and floor transformers.</summary>
    public sealed class ElectricalGridState
    {
        public static readonly ContentId SubstationContentId = new ContentId("utility:electrical_substation");
        public static readonly ContentId RiserContentId = new ContentId("utility:electrical_riser");
        public static readonly ContentId TransformerContentId = new ContentId("utility:floor_transformer");

        public const float DefaultRiserLossPerFloor = .04f;
        public const float BrownoutVoltageThreshold = .8f;
        public const float DemandPerCapacityUnit = .5f;

        public ElectricalGridState(float riserLossPerFloor = DefaultRiserLossPerFloor)
        {
            RiserLossPerFloor = Math.Max(0f, riserLossPerFloor);
        }

        public float RiserLossPerFloor { get; }

        public ElectricalGridSnapshot Evaluate(BuildingTopologyState topology)
        {
            if (topology == null) return ElectricalGridSnapshot.Empty;

            var floors = new List<ElectricalFloorProjection>(topology.FloorCount);
            var totalDemand = CalculateTotalDemand(topology);
            var substationCapacity = CalculateSubstationCapacity(topology);
            var suppliedDemand = Math.Min(totalDemand, substationCapacity);
            var supplyFactor = totalDemand <= 0f ? 1f : suppliedDemand / totalDemand;

            foreach (var slab in topology.FloorSlabs)
            {
                var floor = slab.Key;
                var demand = CalculateFloorDemand(topology.GetRoomsOnFloor(floor));
                var hasTransformer = HasRoomOfType(topology.GetRoomsOnFloor(floor), TransformerContentId);
                var riserColumn = FindContinuousRiserColumn(topology, floor);
                var isConnected = substationCapacity > 0f && hasTransformer && riserColumn.HasValue;
                var voltage = isConnected ? Math.Max(0f, (1f - floor * RiserLossPerFloor) * supplyFactor) : 0f;
                var reason = DetermineBrownoutReason(substationCapacity, hasTransformer, riserColumn.HasValue, voltage);
                floors.Add(new ElectricalFloorProjection(floor, demand, voltage, isConnected, reason, riserColumn ?? 0));
            }

            return new ElectricalGridSnapshot(totalDemand, substationCapacity, suppliedDemand, floors);
        }

        private static float CalculateSubstationCapacity(BuildingTopologyState topology)
        {
            if (!topology.HasFloor(0)) return 0f;
            var capacity = 0f;
            var rooms = topology.GetRoomsOnFloor(0);
            for (var i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].ContentType == SubstationContentId) capacity += Math.Max(0, rooms[i].Capacity);
            }
            return capacity;
        }

        private static float CalculateTotalDemand(BuildingTopologyState topology)
        {
            var total = 0f;
            foreach (var slab in topology.FloorSlabs) total += CalculateFloorDemand(topology.GetRoomsOnFloor(slab.Key));
            return total;
        }

        private static float CalculateFloorDemand(IReadOnlyList<Room> rooms)
        {
            var demand = 0f;
            for (var i = 0; i < rooms.Count; i++)
            {
                if (!IsElectricalInfrastructure(rooms[i].ContentType)) demand += Math.Max(1, rooms[i].Capacity) * DemandPerCapacityUnit;
            }
            return demand;
        }

        private static bool IsElectricalInfrastructure(ContentId type) => type == SubstationContentId || type == RiserContentId || type == TransformerContentId ||
            type == WaterWasteNetworkState.WaterPumpContentId || type == WaterWasteNetworkState.WaterRiserContentId || type == WaterWasteNetworkState.BoosterPumpContentId ||
            type == WaterWasteNetworkState.WasteChuteContentId || type == WaterWasteNetworkState.WasteCollectionContentId;

        private static bool HasRoomOfType(IReadOnlyList<Room> rooms, ContentId type)
        {
            for (var i = 0; i < rooms.Count; i++) if (rooms[i].ContentType == type) return true;
            return false;
        }

        private static int? FindContinuousRiserColumn(BuildingTopologyState topology, int targetFloor)
        {
            if (targetFloor < 0 || !topology.HasFloor(0)) return null;
            var groundRooms = topology.GetRoomsOnFloor(0);
            for (var roomIndex = 0; roomIndex < groundRooms.Count; roomIndex++)
            {
                var groundRiser = groundRooms[roomIndex];
                if (groundRiser.ContentType != RiserContentId) continue;
                for (var x = groundRiser.Bounds.MinX; x <= groundRiser.Bounds.MaxX; x++)
                {
                    var complete = true;
                    for (var floor = 1; floor <= targetFloor; floor++)
                    {
                        if (!HasRiserAtColumn(topology.GetRoomsOnFloor(floor), x)) { complete = false; break; }
                    }
                    if (complete) return x;
                }
            }
            return null;
        }

        private static bool HasRiserAtColumn(IReadOnlyList<Room> rooms, int x)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room.ContentType == RiserContentId && room.Bounds.MinX <= x && room.Bounds.MaxX >= x) return true;
            }
            return false;
        }

        private static ElectricalBrownoutReason DetermineBrownoutReason(float substationCapacity, bool hasTransformer, bool hasContinuousRiser, float voltage)
        {
            if (substationCapacity <= 0f) return ElectricalBrownoutReason.NoSubstation;
            if (!hasContinuousRiser) return ElectricalBrownoutReason.DisconnectedRiser;
            if (!hasTransformer) return ElectricalBrownoutReason.MissingTransformer;
            return voltage < BrownoutVoltageThreshold ? ElectricalBrownoutReason.InsufficientVoltage : ElectricalBrownoutReason.None;
        }
    }

    public enum ElectricalBrownoutReason { None, NoSubstation, DisconnectedRiser, MissingTransformer, InsufficientVoltage }

    public sealed class ElectricalGridSnapshot
    {
        public static readonly ElectricalGridSnapshot Empty = new ElectricalGridSnapshot(0f, 0f, 0f, Array.Empty<ElectricalFloorProjection>());
        public ElectricalGridSnapshot(float totalDemand, float substationCapacity, float suppliedDemand, IReadOnlyList<ElectricalFloorProjection> floors)
        {
            TotalDemand = totalDemand; SubstationCapacity = substationCapacity; SuppliedDemand = suppliedDemand; Floors = floors ?? Array.Empty<ElectricalFloorProjection>();
        }
        public float TotalDemand { get; }
        public float SubstationCapacity { get; }
        public float SuppliedDemand { get; }
        public IReadOnlyList<ElectricalFloorProjection> Floors { get; }
        public bool IsSubstationOverloaded => TotalDemand > SubstationCapacity;
    }

    public readonly struct ElectricalFloorProjection
    {
        public ElectricalFloorProjection(int floor, float demand, float voltage, bool isConnected, ElectricalBrownoutReason brownoutReason, int riserColumn)
        {
            Floor = floor; Demand = demand; Voltage = voltage; IsConnected = isConnected; BrownoutReason = brownoutReason; RiserColumn = riserColumn;
        }
        public int Floor { get; }
        public float Demand { get; }
        public float Voltage { get; }
        public bool IsConnected { get; }
        public ElectricalBrownoutReason BrownoutReason { get; }
        public int RiserColumn { get; }
        public bool IsBrownout => BrownoutReason != ElectricalBrownoutReason.None;
    }
}
