namespace OneRoof.Domain.Weather
{
    /// <summary>
    /// Discrete weather states for the tower's atmospheric environment.
    /// </summary>
    /// <remarks>
    /// Clear   (0) – no precipitation, full visibility.<br/>
    /// Drizzle (1) – light intermittent rain, minor visibility reduction.<br/>
    /// Rain    (2) – steady precipitation, moderate visibility reduction.<br/>
    /// Storm   (3) – heavy precipitation with wind, significant impact on resident mood and transport.<br/>
    /// Fog     (4) – low-visibility moisture layer; no precipitation, heavy transport penalty.<br/>
    /// Snow    (5) – winter precipitation; accumulates and slows movement. Winter months only.
    /// </remarks>
    public enum WeatherCondition
    {
        Clear   = 0,
        Drizzle = 1,
        Rain    = 2,
        Storm   = 3,
        Fog     = 4,
        Snow    = 5,
    }
}
