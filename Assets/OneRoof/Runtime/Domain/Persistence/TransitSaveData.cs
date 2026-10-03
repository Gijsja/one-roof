using System;

namespace OneRoof.Domain.Persistence
{
    [Serializable]
    public sealed class TripGeneratorSaveData
    {
        public int version;
        public int nextTripId;
        public TripRetrySaveData[] retries;
    }

    [Serializable]
    public sealed class TripRetrySaveData
    {
        public int personId;
        public long retryAfterTick;
    }

    [Serializable]
    public sealed class SimulationRandomSaveData
    {
        public int version;
        public ulong seed;
        public ulong state;
        public long position;
    }

    /// <summary>
    /// Serializable sub-record capturing elevator bank cars/queues and active in-flight trip executions.
    /// </summary>
    [Serializable]
    public sealed class TransitSaveData
    {
        public ElevatorBankSaveData elevatorBank;
        public ActiveTripSaveData[] activeTrips;
    }
    [Serializable]
    public sealed class TransitRouteSaveData
    {
        public int originNodeId;
        public int destinationNodeId;
        public int originX;
        public int originFloor;
        public int destinationX;
        public int destinationFloor;
        public TransitEdgeSaveData[] legs;
    }

    [Serializable]
    public sealed class ElevatorPickupSaveData
    {
        public int floor;
        public int carId;
        public int direction;
    }

    [Serializable]
    public sealed class TransitEdgeSaveData
    {
        public int fromNodeId;
        public int toNodeId;
        public int cost;
        public int mode;
        public int durationDataVersion;
        public int durationSeconds;
        public bool hasPhysicalDuration;
    }
}
