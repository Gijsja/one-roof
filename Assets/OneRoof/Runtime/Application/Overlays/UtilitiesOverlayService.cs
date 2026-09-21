using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Infrastructure;

namespace OneRoof.Application.Overlays
{
    /// <summary>Combines physical utility connectivity with operational equipment condition for Overlay 8.</summary>
    public sealed class UtilitiesOverlayService
    {
        public UtilitiesOverlayProjection CreateOverlay(TowerSimulationSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var power = session.ElectricalGridProjection(); var waterWaste = session.WaterWasteNetworkProjection(); var operations = session.UtilityOperationsProjection();
            var failedByFloor = new Dictionary<int, int>();
            foreach (var item in operations.Equipment) if (item.IsFailed) failedByFloor[item.Floor] = failedByFloor.TryGetValue(item.Floor, out var count) ? count + 1 : 1;
            var floors = new List<UtilitiesFloorProjection>();
            for (var floor = 0; floor < session.FloorCount; floor++)
            {
                var electrical = power.Floors[floor]; var plumbing = waterWaste.Floors[floor];
                failedByFloor.TryGetValue(floor, out var failures);
                floors.Add(new UtilitiesFloorProjection(floor, electrical.Voltage, electrical.BrownoutReason.ToString(), plumbing.WaterPressure, plumbing.WaterFailure.ToString(), plumbing.WasteFailure.ToString(), failures));
            }
            return new UtilitiesOverlayProjection(floors, operations.Equipment);
        }
    }

    public sealed class UtilitiesOverlayProjection
    {
        public UtilitiesOverlayProjection(IReadOnlyList<UtilitiesFloorProjection> floors, IReadOnlyList<UtilityEquipmentProjection> equipment)
        { Floors = floors ?? Array.Empty<UtilitiesFloorProjection>(); Equipment = equipment ?? Array.Empty<UtilityEquipmentProjection>(); }
        public IReadOnlyList<UtilitiesFloorProjection> Floors { get; }
        public IReadOnlyList<UtilityEquipmentProjection> Equipment { get; }
        public int FailedEquipmentCount { get { var count = 0; foreach (var item in Equipment) if (item.IsFailed) count++; return count; } }
    }

    public readonly struct UtilitiesFloorProjection
    {
        public UtilitiesFloorProjection(int floor, float voltage, string electricalCause, float waterPressure, string waterCause, string wasteCause, int failedEquipment)
        { Floor = floor; Voltage = voltage; ElectricalCause = electricalCause; WaterPressure = waterPressure; WaterCause = waterCause; WasteCause = wasteCause; FailedEquipment = failedEquipment; }
        public int Floor { get; } public float Voltage { get; } public string ElectricalCause { get; } public float WaterPressure { get; } public string WaterCause { get; } public string WasteCause { get; } public int FailedEquipment { get; }
        public bool HasDisruption => ElectricalCause != "None" || WaterCause != "None" || WasteCause != "None" || FailedEquipment > 0;
        public string AccessibilityLabel => $"Floor {Floor}: power {Voltage:P0} ({ElectricalCause}); water {WaterPressure:P0} ({WaterCause}); waste {WasteCause}; equipment failures {FailedEquipment}.";
    }
}
