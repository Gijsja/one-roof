namespace OneRoof.Application.Overlays
{
    /// <summary>
    /// Application-level classification of transit congestion severity exposed to Presentation and UI.
    /// Prevents leaking Domain assembly types across the architecture boundary (ADR-013).
    /// </summary>
    public enum CongestionTier
    {
        Clear = 0,
        Moderate = 1,
        Heavy = 2,
        Severe = 3
    }
}
