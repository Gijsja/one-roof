using System;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Topology
{
    /// <summary>
    /// Stable authored definitions for buildable room zones.
    /// Mutable campaign state remains in <see cref="Room"/>; this catalog only describes
    /// the initial footprint and capacity selected by a player-facing build tool.
    /// </summary>
    public static class BuildingCatalog
    {
        public static readonly BuildingRoomDefinition Apartment = new BuildingRoomDefinition("residential:apartment", 6, 5);
        public static readonly BuildingRoomDefinition Office = new BuildingRoomDefinition("commercial:office", 8, 8);
        public static readonly BuildingRoomDefinition Diner = new BuildingRoomDefinition("commercial:diner", 10, 5);
        public static readonly BuildingRoomDefinition RetailShop = new BuildingRoomDefinition("commercial:retail", 6, 6);
        public static readonly BuildingRoomDefinition Clinic = new BuildingRoomDefinition("service:clinic", 8, 12);
        public static readonly BuildingRoomDefinition MaintenanceWorkshop = new BuildingRoomDefinition("service:maintenance_workshop", 8, 6);
        public static readonly BuildingRoomDefinition SecurityStation = new BuildingRoomDefinition("service:security_station", 6, 4);
        public static readonly BuildingRoomDefinition ElectricalSubstation = new BuildingRoomDefinition("utility:electrical_substation", 4, 120);
        public static readonly BuildingRoomDefinition ElectricalRiser = new BuildingRoomDefinition("utility:electrical_riser", 2, 0);
        public static readonly BuildingRoomDefinition FloorTransformer = new BuildingRoomDefinition("utility:floor_transformer", 2, 0);

        private static readonly BuildingRoomDefinition[] RoomDefinitions =
        {
            Apartment,
            Office,
            Diner,
            RetailShop,
            Clinic,
            MaintenanceWorkshop,
            SecurityStation,
            ElectricalSubstation,
            ElectricalRiser,
            FloorTransformer
        };

        public static bool TryGetRoomDefinition(string toolId, out BuildingRoomDefinition definition)
        {
            if (!string.IsNullOrEmpty(toolId))
            {
                for (var i = 0; i < RoomDefinitions.Length; i++)
                {
                    if (string.Equals(RoomDefinitions[i].ContentType.Value, toolId, StringComparison.OrdinalIgnoreCase))
                    {
                        definition = RoomDefinitions[i];
                        return true;
                    }
                }
            }

            definition = default;
            return false;
        }
    }

    public readonly struct BuildingRoomDefinition
    {
        public BuildingRoomDefinition(string contentType, int widthInCells, int capacity)
        {
            ContentType = new ContentId(contentType);
            WidthInCells = widthInCells;
            Capacity = capacity;
        }

        public ContentId ContentType { get; }
        public int WidthInCells { get; }
        public int Capacity { get; }
    }
}
