namespace OneRoof.Application.Modes
{
    /// <summary>
    /// Primary interaction verbs available to the player in the tower interface (Docs/04_UX_CONTRACT.md).
    /// </summary>
    public enum InteractionMode
    {
        /// <summary>Observing and querying people, households, businesses, rooms, and systems.</summary>
        Inspect = 0,

        /// <summary>Placing and configuring structure, rooms, transit, utilities, and zones.</summary>
        Build = 1,

        /// <summary>Displaying diagnostic overlays, flows, heatmaps, and service coverage.</summary>
        Data = 2,

        /// <summary>Managing policies, leasing, staff, and emergency priorities.</summary>
        Manage = 3
    }
}
