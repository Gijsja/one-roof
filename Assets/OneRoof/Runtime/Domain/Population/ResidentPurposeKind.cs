namespace OneRoof.Domain.Population
{
    /// <summary>The specific activity a resident has committed to at their current location.</summary>
    public enum ResidentPurposeKind
    {
        None = 0,
        Sleeping = 1,
        WorkingInside = 2,
        WorkingOutside = 3,
        EatingAtDiner = 4,
        Sitting = 5,
        Reading = 6,
        Learning = 7,
        Chilling = 8,
        Socializing = 9
    }
}
