using System;

namespace OneRoof.Domain.Transit
{
    /// <summary>Deterministic bank-wide transport and dispatch settings for an elevator tier.</summary>
    public readonly struct ElevatorTierProfile
    {
        private ElevatorTierProfile(int tier, int floorTravelTicks, int carCapacity, bool etaDispatch, int waitCreditCapTicks)
        {
            Tier = tier;
            Timing = new ElevatorTimingConfig(floorTravelTicks, 2, 2);
            CarCapacity = carCapacity;
            EtaDispatch = etaDispatch;
            WaitCreditCapTicks = waitCreditCapTicks;
        }

        public int Tier { get; }
        public ElevatorTimingConfig Timing { get; }
        public int CarCapacity { get; }
        internal bool EtaDispatch { get; }
        internal int WaitCreditCapTicks { get; }

        public static ElevatorTierProfile ForTier(int tier)
        {
            switch (tier)
            {
                case 1: return new ElevatorTierProfile(1, 5, 8, false, 0);
                case 2: return new ElevatorTierProfile(2, 5, 8, true, 8);
                case 3: return new ElevatorTierProfile(3, 4, 9, true, 16);
                case 4: return new ElevatorTierProfile(4, 4, 9, true, 24);
                case 5: return new ElevatorTierProfile(5, 3, 10, true, 32);
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, "Elevator tier must be between 1 and 5.");
            }
        }
    }
}
