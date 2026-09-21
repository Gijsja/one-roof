using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Infrastructure
{
    /// <summary>
    /// Derives water pressure and gravity-waste collection from built utility rooms.
    /// A ground pump feeds a continuous riser; boosters reset pressure at their floor.
    /// A continuous chute terminating at a ground collector carries waste downward.
    /// </summary>
    public sealed class WaterWasteNetworkState
    {
        public static readonly ContentId WaterPumpContentId = new ContentId("utility:water_pump");
        public static readonly ContentId WaterRiserContentId = new ContentId("utility:water_riser");
        public static readonly ContentId BoosterPumpContentId = new ContentId("utility:water_booster");
        public static readonly ContentId WasteChuteContentId = new ContentId("utility:waste_chute");
        public static readonly ContentId WasteCollectionContentId = new ContentId("utility:waste_collection");

        public const float DefaultPressureLossPerFloor = .12f;
        public const float MinimumServicePressure = .5f;
        public const float DemandPerCapacityUnit = .4f;

        public WaterWasteNetworkState(float pressureLossPerFloor = DefaultPressureLossPerFloor)
        {
            PressureLossPerFloor = Math.Max(0f, pressureLossPerFloor);
        }

        public float PressureLossPerFloor { get; }

        public WaterWasteNetworkSnapshot Evaluate(BuildingTopologyState topology)
        {
            if (topology == null) return WaterWasteNetworkSnapshot.Empty;

            var pumpCapacity = CalculateGroundCapacity(topology, WaterPumpContentId);
            var collectorCapacity = CalculateGroundCapacity(topology, WasteCollectionContentId);
            var totalDemand = CalculateTotalDemand(topology);
            var supplyFactor = totalDemand <= 0f ? 1f : Math.Min(1f, pumpCapacity / totalDemand);
            var floors = new List<WaterWasteFloorProjection>(topology.FloorCount);

            foreach (var slab in topology.FloorSlabs)
            {
                var floor = slab.Key;
                var rooms = topology.GetRoomsOnFloor(floor);
                var demand = CalculateFloorDemand(rooms);
                var waterColumn = FindContinuousColumn(topology, floor, WaterRiserContentId);
                var pressure = CalculatePressure(topology, floor, waterColumn, supplyFactor);
                var waterReason = DetermineWaterFailure(pumpCapacity, waterColumn.HasValue, pressure);
                var wasteColumn = FindContinuousColumn(topology, floor, WasteChuteContentId);
                var wasteReason = DetermineWasteFailure(collectorCapacity, wasteColumn.HasValue);
                floors.Add(new WaterWasteFloorProjection(floor, demand, pressure, waterColumn ?? 0, waterReason, wasteColumn ?? 0, wasteReason));
            }

            return new WaterWasteNetworkSnapshot(totalDemand, pumpCapacity, collectorCapacity, floors);
        }

        private float CalculatePressure(BuildingTopologyState topology, int floor, int? waterColumn, float supplyFactor)
        {
            if (!waterColumn.HasValue) return 0f;
            var sourceFloor = FindHighestBoosterFloor(topology, floor, waterColumn.Value);
            return Math.Max(0f, (1f - (floor - sourceFloor) * PressureLossPerFloor) * supplyFactor);
        }

        private static int FindHighestBoosterFloor(BuildingTopologyState topology, int targetFloor, int column)
        {
            for (var floor = targetFloor; floor > 0; floor--)
            {
                if (HasRoomOfType(topology.GetRoomsOnFloor(floor), BoosterPumpContentId) && HasRoomAtColumn(topology.GetRoomsOnFloor(floor), WaterRiserContentId, column)) return floor;
            }
            return 0;
        }

        private static float CalculateGroundCapacity(BuildingTopologyState topology, ContentId type)
        {
            if (!topology.HasFloor(0)) return 0f;
            var rooms = topology.GetRoomsOnFloor(0);
            var capacity = 0f;
            for (var i = 0; i < rooms.Count; i++) if (rooms[i].ContentType == type) capacity += Math.Max(0, rooms[i].Capacity);
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
            for (var i = 0; i < rooms.Count; i++) if (!IsInfrastructure(rooms[i].ContentType)) demand += Math.Max(1, rooms[i].Capacity) * DemandPerCapacityUnit;
            return demand;
        }

        private static bool IsInfrastructure(ContentId type) => type == WaterPumpContentId || type == WaterRiserContentId || type == BoosterPumpContentId || type == WasteChuteContentId || type == WasteCollectionContentId ||
            type == ElectricalGridState.SubstationContentId || type == ElectricalGridState.RiserContentId || type == ElectricalGridState.TransformerContentId;

        private static bool HasRoomOfType(IReadOnlyList<Room> rooms, ContentId type)
        {
            for (var i = 0; i < rooms.Count; i++) if (rooms[i].ContentType == type) return true;
            return false;
        }

        private static int? FindContinuousColumn(BuildingTopologyState topology, int targetFloor, ContentId type)
        {
            if (targetFloor < 0 || !topology.HasFloor(0)) return null;
            var groundRooms = topology.GetRoomsOnFloor(0);
            for (var roomIndex = 0; roomIndex < groundRooms.Count; roomIndex++)
            {
                var groundRoom = groundRooms[roomIndex];
                if (groundRoom.ContentType != type) continue;
                for (var x = groundRoom.Bounds.MinX; x <= groundRoom.Bounds.MaxX; x++)
                {
                    var complete = true;
                    for (var floor = 1; floor <= targetFloor; floor++)
                    {
                        if (!HasRoomAtColumn(topology.GetRoomsOnFloor(floor), type, x)) { complete = false; break; }
                    }
                    if (complete) return x;
                }
            }
            return null;
        }

        private static bool HasRoomAtColumn(IReadOnlyList<Room> rooms, ContentId type, int x)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room.ContentType == type && room.Bounds.MinX <= x && room.Bounds.MaxX >= x) return true;
            }
            return false;
        }

        private static WaterFailureReason DetermineWaterFailure(float pumpCapacity, bool hasRiser, float pressure)
        {
            if (pumpCapacity <= 0f) return WaterFailureReason.NoGroundPump;
            if (!hasRiser) return WaterFailureReason.DisconnectedRiser;
            return pressure < MinimumServicePressure ? WaterFailureReason.LowPressure : WaterFailureReason.None;
        }

        private static WasteFailureReason DetermineWasteFailure(float collectorCapacity, bool hasChute)
        {
            if (collectorCapacity <= 0f) return WasteFailureReason.NoGroundCollection;
            return hasChute ? WasteFailureReason.None : WasteFailureReason.DisconnectedChute;
        }
    }

    public enum WaterFailureReason { None, NoGroundPump, DisconnectedRiser, LowPressure }
    public enum WasteFailureReason { None, NoGroundCollection, DisconnectedChute }

    public sealed class WaterWasteNetworkSnapshot
    {
        public static readonly WaterWasteNetworkSnapshot Empty = new WaterWasteNetworkSnapshot(0f, 0f, 0f, Array.Empty<WaterWasteFloorProjection>());
        public WaterWasteNetworkSnapshot(float totalDemand, float pumpCapacity, float collectionCapacity, IReadOnlyList<WaterWasteFloorProjection> floors)
        {
            TotalDemand = totalDemand; PumpCapacity = pumpCapacity; CollectionCapacity = collectionCapacity; Floors = floors ?? Array.Empty<WaterWasteFloorProjection>();
        }
        public float TotalDemand { get; }
        public float PumpCapacity { get; }
        public float CollectionCapacity { get; }
        public IReadOnlyList<WaterWasteFloorProjection> Floors { get; }
        public bool IsPumpOverloaded => TotalDemand > PumpCapacity;
    }

    public readonly struct WaterWasteFloorProjection
    {
        public WaterWasteFloorProjection(int floor, float waterDemand, float waterPressure, int waterRiserColumn, WaterFailureReason waterFailure, int wasteChuteColumn, WasteFailureReason wasteFailure)
        {
            Floor = floor; WaterDemand = waterDemand; WaterPressure = waterPressure; WaterRiserColumn = waterRiserColumn; WaterFailure = waterFailure; WasteChuteColumn = wasteChuteColumn; WasteFailure = wasteFailure;
        }
        public int Floor { get; }
        public float WaterDemand { get; }
        public float WaterPressure { get; }
        public int WaterRiserColumn { get; }
        public WaterFailureReason WaterFailure { get; }
        public int WasteChuteColumn { get; }
        public WasteFailureReason WasteFailure { get; }
        public bool HasWaterService => WaterFailure == WaterFailureReason.None;
        public bool HasWasteCollection => WasteFailure == WasteFailureReason.None;
    }
}
