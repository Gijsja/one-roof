namespace OneRoof.Domain.Population
{
    /// <summary>
    /// The named needs tracked per resident. Satisfaction levels influence mood,
    /// route choices, and faction alignment over time.
    /// </summary>
    public enum NeedKind
    {
        /// <summary>Need for regular food access. Drops over time; satisfied at diners/kitchens.</summary>
        Hunger = 0,

        /// <summary>Need for adequate rest and physical energy. Drops while awake; restored by sleeping.</summary>
        Energy = 1,

        /// <summary>Backwards-compatible alias for Energy.</summary>
        Rest = 1,

        /// <summary>Need for interaction with other residents. Influenced by shared space contact.</summary>
        Social = 2,

        /// <summary>Need for personal cleanliness and sanitation. Restored in bathrooms/apartments.</summary>
        Hygiene = 3,

        /// <summary>Need for productive activity and occupation. Restored by working or civic duties.</summary>
        Purpose = 4,

        /// <summary>Need for adequate personal space and quiet. Preserved for backward compatibility.</summary>
        Comfort = 5,
    }
}
