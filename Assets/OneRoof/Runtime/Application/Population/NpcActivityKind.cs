namespace OneRoof.Application.Population
{
    /// <summary>
    /// Application-level activity projection exposed to the presentation layer for view tinting.
    /// Prevents leaking Domain assembly types across the architecture boundary (ADR-013).
    /// </summary>
    public enum NpcActivityKind
    {
        Idle = 0,
        Sleeping = 1,
        Working = 2,
        Eating = 3,
        Leisure = 4
    }
}
