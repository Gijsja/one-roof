using System;

namespace OneRoof.Domain.Persistence
{
    /// <summary>
    /// Serializable sub-record capturing elevator bank cars/queues and active in-flight trip executions.
    /// </summary>
    [Serializable]
    public sealed class TransitSaveData
    {
        public ElevatorBankSaveData elevatorBank;
        public ActiveTripSaveData[] activeTrips;
    }
}
