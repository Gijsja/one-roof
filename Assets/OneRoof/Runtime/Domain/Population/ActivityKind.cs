namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Coarse activity state for a resident at any given simulation tick.
    /// Presentation may use this to select the correct NPC animation or overlay icon.
    /// </summary>
    public enum ActivityKind
    {
        /// <summary>No specific activity assigned; used as a safe default before first schedule resolution.</summary>
        Idle,

        /// <summary>Resident is asleep inside their home room.</summary>
        Sleeping,

        /// <summary>Resident is at their workplace room.</summary>
        Working,

        /// <summary>Resident is eating at a food-service room.</summary>
        Eating,

        /// <summary>Resident is in transit between rooms.</summary>
        Commuting,

        /// <summary>Resident is spending discretionary time in a social or amenity room.</summary>
        Leisure,
    }
}
