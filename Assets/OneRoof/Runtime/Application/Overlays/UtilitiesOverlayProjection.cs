using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Infrastructure;

namespace OneRoof.Application.Overlays
{
    /// <summary>Combines physical utility connectivity with operational equipment condition for Overlay 8.</summary>


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
        public UtilitiesFloorProjection(int floor, float voltage, string electricalCause, float waterPressure, string waterCause, string wasteCause, int failedEquipment, int powerColumn = 0, int waterColumn = 0, int wasteColumn = 0, bool powerConnected = false, bool waterConnected = false, bool wasteConnected = false)
        { Floor = floor; Voltage = voltage; ElectricalCause = electricalCause; WaterPressure = waterPressure; WaterCause = waterCause; WasteCause = wasteCause; FailedEquipment = failedEquipment; PowerColumn = powerColumn; WaterColumn = waterColumn; WasteColumn = wasteColumn; PowerConnected = powerConnected; WaterConnected = waterConnected; WasteConnected = wasteConnected; }
        public int Floor { get; } public float Voltage { get; } public string ElectricalCause { get; } public float WaterPressure { get; } public string WaterCause { get; } public string WasteCause { get; } public int FailedEquipment { get; }
        public int PowerColumn { get; } public int WaterColumn { get; } public int WasteColumn { get; }
        public bool PowerConnected { get; } public bool WaterConnected { get; } public bool WasteConnected { get; }
        public bool HasDisruption => ElectricalCause != "None" || WaterCause != "None" || WasteCause != "None" || FailedEquipment > 0;
        public string AccessibilityLabel => $"Floor {Floor}: power {Voltage:P0} ({ElectricalCause}); water {WaterPressure:P0} ({WaterCause}); waste {WasteCause}; equipment failures {FailedEquipment}.";
    }
}
