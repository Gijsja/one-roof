namespace OneRoof.Domain.Population
{
    /// <summary>
    /// The named needs tracked per resident. Satisfaction levels influence mood,
    /// route choices, and faction alignment over time.
    /// </summary>
    public enum NeedKind
    {
        /// <summary>Need for regular food access. Drops if meal trips are missed.</summary>
        Hunger,

        /// <summary>Need for adequate sleep. Drops if sleeping hours are compressed by commute delays.</summary>
        Rest,

        /// <summary>Need for interaction with other residents. Influenced by shared space contact.</summary>
        Social,

        /// <summary>Need for adequate personal space and quiet. Inversely pressured by overcrowded routes.</summary>
        Comfort,
    }
}
