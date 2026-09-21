namespace OneRoof.Application.Overlays
{
    /// <summary>
    /// Types of diagnostic overlays available in Data mode (Docs/04_UX_CONTRACT.md).
    /// </summary>
    public enum DataOverlayKind
    {
        None = 0,
        ElevatorWait = 1,
        FootTraffic = 2,
        Population = 3,
        Satisfaction = 4,
        Noise = 5,
        BusinessHealth = 6,
        FactionTension = 7,
        Utilities = 8
    }
}
