namespace OneRoof.Domain.Trips
{
    /// <summary>
    /// Lifecycle state of a single trip.
    /// Transitions: Planned → InProgress → Completed
    ///                                   → Cancelled
    /// </summary>
    public enum TripState
    {
        /// <summary>Route has been planned; the resident has not yet started moving.</summary>
        Planned,

        /// <summary>Resident is actively travelling along the route.</summary>
        InProgress,

        /// <summary>Resident has arrived at the destination.</summary>
        Completed,

        /// <summary>Trip was abandoned (e.g. destination removed, simulation reset).</summary>
        Cancelled,
    }
}
