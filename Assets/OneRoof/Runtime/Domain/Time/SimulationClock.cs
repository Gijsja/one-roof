using System;

namespace OneRoof.Domain.Time
{
    /// <summary>Clock advanced by simulation steps rather than render-frame time.</summary>
    public sealed class SimulationClock
    {
        public SimulationClock(Tick initialTick)
        {
            CurrentTick = initialTick;
        }

        public Tick CurrentTick { get; private set; }

        public Tick Advance()
        {
            return Advance(1);
        }

        public Tick Advance(long steps)
        {
            if (steps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), steps, "Clock advancement must be positive.");
            }

            CurrentTick = new Tick(checked(CurrentTick.Value + steps));
            return CurrentTick;
        }
    }
}
