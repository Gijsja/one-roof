using System;
using OneRoof.Domain.Population;

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
        public int householdLedgerVersion;
        public int residentNeedsVersion;
        public OutsideMarketSaveData outsideMarket;
        public HouseholdLeaseLifecycleSaveData householdLeaseLifecycle;
        public int policyVersion;
        public float rentCapMultiplier;
        public float commercialTaxRate;
        public bool transitSubsidyEnabled;
        public bool quietHoursEnabled;
        public long lastSettlementTick;
        public long pendingRent;
        public long pendingTax;
        public long pendingUpkeep;
        public long pendingSubsidy;
        public long pendingConstruction;
        public long pendingConstructionSalvage;
        public long lastDailyRent;
        public long lastDailyTax;
        public long lastDailyUpkeep;
        public long lastDailySubsidy;
        public long lastDailyConstruction;
        public long lastDailyConstructionSalvage;
        public int nextElevatorCarId;
        public int nextEntityId;
        public OneRoof.Domain.Trips.TransitDemandSaveData transitDemand;
        public OneRoof.Domain.Trips.DemandDayData lastTransitDemandDay;
        public bool hasLastTransitDemandDay;
        public TripGeneratorSaveData tripGenerator;
        public SimulationRandomSaveData simulationRandom;

        public FloorSlabSaveData[] floorSlabs;
        public RoomSaveData[] rooms;
        public PortalSaveData[] portals;
        public UndergroundCellSaveData[] undergroundCells;
        public UndergroundCellSaveData[] undergroundFloorCells;
        public bool undergroundGridV2;
        public bool undergroundGridV3;
        public UndergroundCellSaveData[] undergroundCorridorCells;
        public UndergroundCellSaveData[] undergroundShaftCells;
        public UndergroundCellSaveData undergroundCoreCell;
        public UndergroundRoomSaveData[] undergroundRooms;
        public UndergroundOperationsSaveData undergroundOperations;

        public HouseholdSaveData[] households;
        public PersonSaveData[] persons;

        public ElevatorBankSaveData elevatorBank;
        public ActiveTripSaveData[] activeTrips;
        public ScrutinySaveData scrutiny;
        public BusinessSaveData[] businesses;
        public BusinessLifecycleSaveData businessLifecycle;
        public UtilityOperationsSaveData utilityOperations;
        public FactionSaveData factions;
        public CivilActionSaveData civilActions;
        public CaretakerCrisisSaveData caretakerCrisis;
        public WordBattleSaveData wordBattles;
        public DecisionRecordSaveData decisions;

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
            householdLedgerVersion = householdLedgerVersion,
            residentNeedsVersion = residentNeedsVersion,
            households = households,
            persons = persons
        };

        public OutsideMarketSaveData GetOutsideMarketSaveData() => outsideMarket;

        public HouseholdLeaseLifecycleSaveData GetHouseholdLeaseLifecycleSaveData() => householdLeaseLifecycle;

        public void SetOutsideMarketSaveData(OutsideMarketSaveData market)
        {
            outsideMarket = market;
        }

        public void SetHouseholdLeaseLifecycleSaveData(HouseholdLeaseLifecycleSaveData lifecycle)
        {
            householdLeaseLifecycle = lifecycle;
        }

        public void SetPopulationSaveData(PopulationSaveData pop)
        {
            householdLedgerVersion = pop?.householdLedgerVersion ?? 0;
            residentNeedsVersion = pop?.residentNeedsVersion ?? 0;
            households = pop?.households;
            persons = pop?.persons;
        }

        public EconomySaveData GetEconomySaveData() => new EconomySaveData
        {
            cashBalance = cashBalance,
            sandboxMode = sandboxMode,
            totalRevenue = totalRevenue,
            totalExpenses = totalExpenses,
            policyVersion = policyVersion,
            rentCapMultiplier = rentCapMultiplier,
            commercialTaxRate = commercialTaxRate,
            transitSubsidyEnabled = transitSubsidyEnabled,
            quietHoursEnabled = quietHoursEnabled,
            lastSettlementTick = lastSettlementTick,
            pendingRent = pendingRent,
            pendingTax = pendingTax,
            pendingUpkeep = pendingUpkeep,
            pendingSubsidy = pendingSubsidy,
            pendingConstruction = pendingConstruction,
            pendingConstructionSalvage = pendingConstructionSalvage,
            lastDailyRent = lastDailyRent,
            lastDailyTax = lastDailyTax,
            lastDailyUpkeep = lastDailyUpkeep,
            lastDailySubsidy = lastDailySubsidy,
            lastDailyConstruction = lastDailyConstruction,
            lastDailyConstructionSalvage = lastDailyConstructionSalvage
        };

        public void SetEconomySaveData(EconomySaveData econ)
        {
            if (econ != null)
            {
                cashBalance = econ.cashBalance;
                sandboxMode = econ.sandboxMode;
                totalRevenue = econ.totalRevenue;
                totalExpenses = econ.totalExpenses;
                policyVersion = econ.policyVersion;
                rentCapMultiplier = econ.rentCapMultiplier;
                commercialTaxRate = econ.commercialTaxRate;
                transitSubsidyEnabled = econ.transitSubsidyEnabled;
                quietHoursEnabled = econ.quietHoursEnabled;
                lastSettlementTick = econ.lastSettlementTick;
                pendingRent = econ.pendingRent;
                pendingTax = econ.pendingTax;
                pendingUpkeep = econ.pendingUpkeep;
                pendingSubsidy = econ.pendingSubsidy;
                pendingConstruction = econ.pendingConstruction;
                pendingConstructionSalvage = econ.pendingConstructionSalvage;
                lastDailyRent = econ.lastDailyRent;
                lastDailyTax = econ.lastDailyTax;
                lastDailyUpkeep = econ.lastDailyUpkeep;
                lastDailySubsidy = econ.lastDailySubsidy;
                lastDailyConstruction = econ.lastDailyConstruction;
                lastDailyConstructionSalvage = econ.lastDailyConstructionSalvage;
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
    public sealed class BusinessSaveData
    {
        public int id;
        public int roomId;
        public string contentType;
        public int[] employeeIds;
        public long cashBalance;
        public long lastCustomerRevenue;
        public long lastContractRevenue;
        public long lastWages;
        public long lastOperatingCost;
        public long lastRentPaid;
        public long lastTaxPaid;
        public int arrearsDays;
        public bool wageArrears;
        public bool isInsolvent;
    }

    [Serializable]
    public sealed class BusinessLifecycleSaveData
    {
        public long reLeaseOpeningCapitalSource;
        public long reLeaseDebtWriteOffSource;
        public long reLeaseCashRetiredSink;
        public long reLeaseCount;
    }

    [Serializable]
    public sealed class UtilityOperationsSaveData
    {
        public UtilityEquipmentSaveData[] equipment;
    }

    [Serializable]
    public sealed class UtilityEquipmentSaveData
    {
        public int roomId;
        public float condition;
    }

    [Serializable]
    public sealed class FloorSlabSaveData
    {
        public int floorLevel;
        public int minX;
        public int maxX;
    }

    [Serializable]
    public sealed class UndergroundCellSaveData
    {
        public int x;
        public int depth;
    }

    [Serializable]
    public sealed class UndergroundRoomSaveData
    {
        public int id;
        public int type;
        public int x;
        public int depth;
        public int width;
        public int height;
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
        public long cashBalance;
        public int arrearsDays;
        public long dailyIncome;
        public long dailyServiceSpend;
        public long rentArrearsBalance;
        public int rentArrearsDays;
        public long outsideCreditBalance;
        public long dailyOutsideEssentialSpend;
        public long dailyOutsideQualitySpend;
        public long dailyCareSpend;
        public long dailyRentDue;
        public long dailyRentPaid;
        public long dailyOutsideWages;
        public long dailyCreditRepayment;
        public long[] recentDailyBudgetNetFlows;
        public long dailyEssentialShortfall;
        public long underprovisionExposure;
        public string[] acquiredProps;
    }

    [Serializable]
    public sealed class PersonSaveData
    {
        public int id;
        public int householdId;
        public int homeRoomId;
        public int workplaceRoomId;
        public int workplaceLocationKind;
        public int currentRoomId;
        public int currentLocationKind;
        public int currentActivity;
        public int currentPurpose;
        public long purposeStartedAtTick;
        public long purposeEndsAtTick;
        public long outsideFoodRetryAfterTick;
        public string[] scheduleLabels;
        public long[] scheduleStartTicks;
        public long[] scheduleEndTicks;
        public int routineVersion;
        public int employmentCohort;
        public uint routineSeed;
        public long[] scheduleEpisodeStartTicks;
        public long[] scheduleAppointmentStartTicks;
        public long[] scheduleAppointmentEndTicks;
        public long routineChoiceDay;
        public int routineDayChoice;
        public long workAppointmentStartTick;
        public long workAppointmentEndTick;
        public long workSegmentStartTick;
        public long workDemandEpisodeStartTick;
        public long workDepartureTick;
        public long workArrivalTick;
        public int trait;
        public int[] personalityFacets;
        public int[] socialTraits;
        public ThoughtMemorySaveData[] thoughts;
        public float wellbeingSatisfaction;
        public float wellbeingStrain;
        public string[] priorSettlementGrievances;
        public string[] socialInfluenceIssues;
        public float[] socialInfluencePressures;
        public float hungerSatisfaction;
        public float restSatisfaction;
        public float energySatisfaction;
        public float socialSatisfaction;
        public float hygieneSatisfaction;
        public float purposeSatisfaction;
        public int specialistRole;
        public int specialistTrainingRole;
        public float specialistTrainingProgress;
    }

    [Serializable]
    public sealed class ThoughtMemorySaveData
    {
        public string description;
        public float moodDelta;
        public long createdAtTick;
        public long expiresAtTick;
        public int targetResidentId;
    }

    [Serializable]
    public sealed class ElevatorBankSaveData
    {
        public int timingDataVersion;
        public ElevatorPickupSaveData[] pickups;
        public int minFloor;
        public int maxFloor;
        public int tier = 1;
        public ElevatorCarSaveData[] cars;
        public ElevatorPassengerSaveData[] queuedPassengers;
        public ElevatorPassengerSaveData[] deliveredPassengers;
        public int cumulativeDeliveredCount;
        public long cumulativeWaitTicks;
    }

    [Serializable]
    public sealed class ElevatorCarSaveData
    {
        public int timingDataVersion;
        public bool usesSeconds;
        public int floorTravelSeconds;
        public int doorCycleSeconds;
        public int dwellSeconds;
        public int timerSecondsRemaining;
        public int phaseTimingDataVersion;
        public int phaseDurationSeconds;
        public int[] targetFloors;
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
        public int movementDataVersion;
        public long checkpointTick;
        public int legRemainingSeconds;
        public int legDurationSeconds;
        public bool legacyLegProgress;
        public long lastReconsiderationTick;
        public int routeDataVersion;
        public TransitRouteSaveData plannedRoute;
        public TransitRouteSaveData executionRoute;
        public int tripId;
        public int personId;
        public int originRoomId;
        public int destinationRoomId;
        public int originLocationKind;
        public int destinationLocationKind;
        public int purpose;
        public bool outsideServiceTransactionRecorded;
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
