namespace OneRoof.Domain.Trips
{
    /// <summary>
    /// The purpose of a trip, derived from the schedule block the resident is transitioning into.
    /// Determines the target room category for route planning.
    /// </summary>
    public enum TripPurpose
    {
        /// <summary>Returning to the resident's home room (Leisure → Sleep transition).</summary>
        Home,

        /// <summary>Travelling to the resident's workplace room (Sleep → Work transition).</summary>
        Work,

        /// <summary>Travelling to a food-service room (Work → Eat transition).</summary>
        Food,

        /// <summary>Travelling to a social or amenity room (Eat → Leisure transition).</summary>
        Leisure,

        /// <summary>Returning to home apartment or bathroom to freshen up when hygiene is low.</summary>
        Hygiene,
    }
}
