using System;

namespace OneRoof.Domain.Persistence
{
    /// <summary>
    /// Root serializable data transfer object for the complete unified TowerSimulation state.
    /// Serialized directly by JsonSaveSerializer and stored inside versioned SaveEnvelope.
    /// </summary>
    [Serializable]
    public sealed class TowerSaveData
    {
        public long simulationTick;
        public long cashBalance;
        public bool sandboxMode;
        public long totalRevenue;
        public long totalExpenses;
        public int nextElevatorCarId;
        public int nextEntityId;

        public FloorSlabSaveData[] floorSlabs;
        public RoomSaveData[] rooms;
        public PortalSaveData[] portals;

        public HouseholdSaveData[] households;
        public PersonSaveData[] persons;

        public ElevatorBankSaveData elevatorBank;
        public ActiveTripSaveData[] activeTrips;
        public ScrutinySaveData scrutiny;

        public TopologySaveData GetTopologySaveData() => new TopologySaveData
        {
            floorSlabs = floorSlabs,
            rooms = rooms,
            portals = portals
        };

        public void SetTopologySaveData(TopologySaveData top)
        {
            floorSlabs = top?.floorSlabs;
            rooms = top?.rooms;
            portals = top?.portals;
        }

        public PopulationSaveData GetPopulationSaveData() => new PopulationSaveData
        {
            households = households,
            persons = persons
        };

        public void SetPopulationSaveData(PopulationSaveData pop)
        {
            households = pop?.households;
            persons = pop?.persons;
        }

        public EconomySaveData GetEconomySaveData() => new EconomySaveData
        {
            cashBalance = cashBalance,
            sandboxMode = sandboxMode,
            totalRevenue = totalRevenue,
            totalExpenses = totalExpenses
        };

        public void SetEconomySaveData(EconomySaveData econ)
        {
            if (econ != null)
            {
                cashBalance = econ.cashBalance;
                sandboxMode = econ.sandboxMode;
                totalRevenue = econ.totalRevenue;
                totalExpenses = econ.totalExpenses;
            }
        }
    }

    [Serializable]
    public sealed class ScrutinySaveData
    {
        public float value;
        public float previousValue;
        public float recentExpansionPressure;
        public float recentPolicyPressure;
    }

    [Serializable]
    public sealed class FloorSlabSaveData
    {
        public int floorLevel;
        public int minX;
        public int maxX;
    }

    [Serializable]
    public sealed class RoomSaveData
    {
        public int id;
        public string contentType;
        public int floor;
        public int minX;
        public int maxX;
        public int capacity;
        public int[] portalIds;
    }

    [Serializable]
    public sealed class PortalSaveData
    {
        public int id;
        public int portalType;
        public int floor;
        public int x;
        public int roomId;
        public int targetPortalId;
    }

    [Serializable]
    public sealed class HouseholdSaveData
    {
        public int id;
        public int homeRoomId;
        public int[] memberIds;
        public float budget;
        public float satisfaction;
    }

    [Serializable]
    public sealed class PersonSaveData
    {
        public int id;
        public int householdId;
        public int homeRoomId;
        public int workplaceRoomId;
        public int currentRoomId;
        public int currentActivity;
        public int trait;
        public int[] personalityFacets;
        public float wellbeingSatisfaction;
        public float wellbeingStrain;
        public float hungerSatisfaction;
        public float restSatisfaction;
        public float energySatisfaction;
        public float socialSatisfaction;
        public float hygieneSatisfaction;
        public float purposeSatisfaction;
    }

    [Serializable]
    public sealed class ElevatorBankSaveData
    {
        public int minFloor;
        public int maxFloor;
        public ElevatorCarSaveData[] cars;
        public ElevatorPassengerSaveData[] queuedPassengers;
        public ElevatorPassengerSaveData[] deliveredPassengers;
        public int cumulativeDeliveredCount;
        public long cumulativeWaitTicks;
    }

    [Serializable]
    public sealed class ElevatorCarSaveData
    {
        public int id;
        public int currentFloor;
        public int capacity;
        public int phase;
        public int direction;
        public int timerTicksRemaining;
        public ElevatorPassengerSaveData[] passengers;
    }

    [Serializable]
    public sealed class ElevatorPassengerSaveData
    {
        public int personId;
        public int originFloor;
        public int destinationFloor;
        public long waitTicks;
        public long rideTicks;
    }

    [Serializable]
    public sealed class ActiveTripSaveData
    {
        public int tripId;
        public int personId;
        public int originRoomId;
        public int destinationRoomId;
        public int purpose;
        public long departureTick;
        public int state;
        public int waitTicks;
        public int currentLegIndex;
        public int legRemainingTicks;
        public bool isQueuedInElevator;
        public bool isRidingElevator;
        public int currentFloor;
        public float currentX;
    }
}
