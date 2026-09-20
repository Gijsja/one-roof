using System;
using System.Collections.Generic;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Infrastructure
{
    /// <summary>Pure-C# electrical network model: a ground substation feeds a vertical riser and per-floor transformers.</summary>
    public sealed class ElectricalGridState
    {
        public ElectricalGridState(float substationCapacity = 120f, float riserLossPerFloor = .04f)
        {
            SubstationCapacity = Math.Max(0f, substationCapacity);
            RiserLossPerFloor = Math.Max(0f, riserLossPerFloor);
        }

        public float SubstationCapacity { get; }
        public float RiserLossPerFloor { get; }

        public ElectricalGridSnapshot Evaluate(BuildingTopologyState topology)
        {
            var floors = new List<ElectricalFloorProjection>();
            if (topology == null) return new ElectricalGridSnapshot(0f, SubstationCapacity, floors);
            var totalDemand = 0f;
            foreach (var slab in topology.FloorSlabs)
            {
                var demand = 0f;
                foreach (var room in topology.GetRoomsOnFloor(slab.Key)) demand += Math.Max(1, room.Capacity) * .5f;
                totalDemand += demand;
                var voltage = Math.Max(0f, 1f - slab.Key * RiserLossPerFloor - Math.Max(0f, totalDemand - SubstationCapacity) / Math.Max(1f, SubstationCapacity));
                floors.Add(new ElectricalFloorProjection(slab.Key, demand, voltage, voltage < .8f));
            }
            return new ElectricalGridSnapshot(totalDemand, SubstationCapacity, floors);
        }
    }

    public sealed class ElectricalGridSnapshot
    {
        public ElectricalGridSnapshot(float totalDemand, float substationCapacity, IReadOnlyList<ElectricalFloorProjection> floors) { TotalDemand = totalDemand; SubstationCapacity = substationCapacity; Floors = floors; }
        public float TotalDemand { get; } public float SubstationCapacity { get; } public IReadOnlyList<ElectricalFloorProjection> Floors { get; }
    }
    public readonly struct ElectricalFloorProjection
    {
        public ElectricalFloorProjection(int floor, float demand, float voltage, bool isBrownout) { Floor = floor; Demand = demand; Voltage = voltage; IsBrownout = isBrownout; }
        public int Floor { get; } public float Demand { get; } public float Voltage { get; } public bool IsBrownout { get; }
    }
}
