namespace OneRoof.Application.Overlays
{
    /// <summary>
    /// Types of diagnostic overlays available in Data mode (Docs/04_UX_CONTRACT.md).
    /// Status: ElevatorWait, FootTraffic, Population, Satisfaction, BusinessHealth,
    /// and Utilities are implemented by TowerDataOverlays and Presentation
    /// presenters. Noise and FactionTension are reserved for OR-903 and have no
    /// projection or presenter yet; do not expose them in overlay UI until then.
    /// </summary>
    public enum DataOverlayKind
    {
        None = 0,
        ElevatorWait = 1, // Implemented (OR-402).
        FootTraffic = 2, // Implemented (OR-703).
        Population = 3, // Implemented (OR-603).
        Satisfaction = 4, // Implemented (OR-602).
        Noise = 5, // READY (OR-903): no service or presenter yet.
        BusinessHealth = 6, // Implemented (OR-703).
        FactionTension = 7, // READY (OR-903): no service or presenter yet.
        Utilities = 8 // Implemented (OR-803).
    }
}
