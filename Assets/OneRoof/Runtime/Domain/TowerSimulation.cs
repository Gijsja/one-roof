using System;
using System.Collections.Generic;
using OneRoof.Domain.Commands;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Decisions;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Events;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Scrutiny;
using OneRoof.Domain.Social;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;
using OneRoof.Domain.Underground;

namespace OneRoof.Domain
{
    /// <summary>
    /// Master pure C# domain simulation orchestrator for the vertical city tower.
    /// Drives simulation clock, routine-based trip generation, hierarchical routing,
    /// leg-by-leg transit execution, economy, and demand-driven leasing.
    /// </summary>
    public sealed class TowerSimulation
    {
        private int _nextElevatorCarId = 500;
        private int _nextEntityId = 3000;
        private readonly long _settlementPeriod;
        private readonly List<PersonRecord> _outsideWorkers = new List<PersonRecord>();
        private readonly List<HouseholdRecord> _settlementHouseholds = new List<HouseholdRecord>();

        public TowerSimulation(
            SimulationClock clock,
            BuildingTopologyState topology,
            PopulationState population,
            ElevatorBank elevatorBank,
            TowerEconomyState economy = null,
            IRandomStream randomStream = null,
            ScrutinyState scrutiny = null,
            long settlementPeriod = DailySchedule.TicksPerDay,
            OutsideMarketState outsideMarket = null)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Topology = topology ?? throw new ArgumentNullException(nameof(topology));
            Population = population ?? throw new ArgumentNullException(nameof(population));
            ElevatorBank = elevatorBank ?? throw new ArgumentNullException(nameof(elevatorBank));
            if (settlementPeriod <= 0) throw new ArgumentOutOfRangeException(nameof(settlementPeriod));
            _settlementPeriod = settlementPeriod;
            Economy = economy ?? new TowerEconomyState();
            OutsideMarket = outsideMarket ?? new OutsideMarketState();
            RandomStream = randomStream ?? new DeterministicRandomStream(1337);

            Planner = new TransitRoutePlanner(Topology.TransitGraph);
            TripGenerator = new ScheduleTripGenerator(Topology.ToSnapshot(), Topology.TransitGraph, Planner);
            Transit = new TransitExecutionSystem();
            Leasing = new LeasingDemandSystem();
            Needs = new ResidentNeedsSystem();
            Specialists = new SpecialistRoleSystem();
            Businesses = new BusinessState();
            ElectricalGrid = new ElectricalGridState();
            WaterWasteNetwork = new WaterWasteNetworkState();
            UtilityOperations = new UtilityOperationsState();
            Wellbeing = new ResidentWellbeingSystem();
            Scrutiny = scrutiny ?? new ScrutinyState();
            Factions = new FactionState();
            CivilActions = new CivilActionState();
            Decisions = new DecisionRecordState();
            HousingLifecycle = new HouseholdLeaseLifecycleState();
            HousingLifecycleSystem = new HouseholdHousingLifecycleSystem();
            Social = new ResidentSocialSystem();
            Operations = new UndergroundOperationsState();
        }

        public SimulationClock Clock { get; }

        public BuildingTopologyState Topology { get; }

        public UndergroundDigState Underground { get; private set; } = new UndergroundDigState();
        public UndergroundOperationsState Operations { get; private set; }

        public PopulationState Population { get; }

        public ElevatorBank ElevatorBank { get; }

        public TowerEconomyState Economy { get; }

        public OutsideMarketState OutsideMarket { get; private set; }

        public LeasingDemandSystem Leasing { get; }

        public IRandomStream RandomStream { get; }

        public TransitRoutePlanner Planner { get; private set; }

        public ScheduleTripGenerator TripGenerator { get; private set; }

        public TransitExecutionSystem Transit { get; }

        public ResidentNeedsSystem Needs { get; }
        public SpecialistRoleSystem Specialists { get; }
        public BusinessState Businesses { get; private set; }
        public ElectricalGridState ElectricalGrid { get; }
        public WaterWasteNetworkState WaterWasteNetwork { get; }
        public UtilityOperationsState UtilityOperations { get; private set; }
        public ResidentSocialSystem Social { get; }
        public ResidentWellbeingSystem Wellbeing { get; }
        public ScrutinyState Scrutiny { get; }
        public FactionState Factions { get; private set; }
        public CivilActionState CivilActions { get; private set; }
        public DecisionRecordState Decisions { get; private set; }
        public HouseholdLeaseLifecycleState HousingLifecycle { get; private set; }
        public HouseholdHousingLifecycleSystem HousingLifecycleSystem { get; }

        private List<CivilActionSignal> BuildCivilActionSignals()
        {
            var signals = new List<CivilActionSignal>(Factions.Factions.Count);
            foreach (var faction in Factions.Factions)
            {
                var people = new List<int>();
                var floors = new SortedSet<int>();
                foreach (var support in Factions.Supports)
                {
                    if (support.FactionId != faction.Id || !support.IsMember) continue;
                    people.Add(support.ResidentId.Value);
                    floors.Add(support.HomeFloor);
                }
                people.Sort();
                var floorArray = new int[floors.Count];
                floors.CopyTo(floorArray);
                signals.Add(new CivilActionSignal(faction.Id, faction.Pressure, faction.MemberCount,
                    faction.TopGrievance, people.ToArray(), floorArray));
            }
            return signals;
        }

        public long CurrentTick => Clock.CurrentTick.Value;

        public long CurrentSimulationDay => CurrentTick / _settlementPeriod;

        /// <summary>Pure calendar view over the tick clock for the day/night presentation clock.</summary>
        public DayPhase DayPhase => DayClock.FromTick(CurrentTick);

        public int ResidentCount => Population.ResidentCount;

        public int ActiveTripCount => Transit.ActiveTripCount;

        public int TotalQueuedElevatorPassengers => ElevatorBank.TotalQueuedCount;

        public float AverageElevatorWaitTicks => ElevatorBank.AverageWaitTicks;

        /// <summary>Immutable electrical state derived from the authoritative topology at the time of request.</summary>
        public ElectricalGridSnapshot ElectricalGridSnapshot() =>
            ElectricalGrid.Evaluate(Topology, Operations.BackupPowerCapacity, UtilityOperations.Snapshot(Topology));

        /// <summary>Immutable water pressure and gravity-waste collection state derived from the authoritative topology.</summary>
        public WaterWasteNetworkSnapshot WaterWasteNetworkSnapshot() =>
            WaterWasteNetwork.Evaluate(Topology, UtilityOperations.Snapshot(Topology));

        /// <summary>Mutable operational condition of installed utility equipment, projected without Unity dependencies.</summary>
        public UtilityOperationsSnapshot UtilityOperationsSnapshot() => UtilityOperations.Snapshot(Topology);

        public void AdvanceOneTick()
        {
            var previousTick = Clock.CurrentTick;
            Clock.Advance();
            var currentTick = Clock.CurrentTick;

            // 0. Advance resident needs (decay and replenishment based on activity)
            Needs.Advance(Population, currentTick);
            Specialists.Advance(Population, Topology, currentTick, (int)Math.Ceiling(Operations.TrainingBoost * 10f));
            UtilityOperations.Advance(Topology, Population, Operations.RepairBoost);
            Social.Advance(Population, Topology, ElevatorBank, Factions, currentTick);
            Wellbeing.Advance(Population, ElevatorBank, Specialists.ServiceEfficiencyMultiplier + Operations.CareBoost, Economy.Policy.RentCapMultiplier, Economy.Policy.TransitSubsidyEnabled,
                applyDailyArrearsStrain: currentTick.Value % _settlementPeriod == 0,
                dayFraction: 1f / _settlementPeriod,
                quietHoursEnabled: Economy.Policy.QuietHoursEnabled && DayPhase.IsNight,
                commonsMoraleBoost: Operations.CommonsMoraleBoost);
            Scrutiny.Advance(Topology, Population, Specialists.CrisisResponseMultiplier + Operations.ShelterCapacity * .001f);
            Operations.AdvanceTick(Underground, Economy, Scrutiny, currentTick.Value);

            // 1. Periodic autonomous leasing demand evaluation (every 10 ticks)
            if (currentTick.Value % 10 == 0)
            {
                var occupancyFactor = CalculateResidentialOccupancyFactor() * CivilActions.BusinessOutputMultiplier;
                var nextIdBeforeBusinessAdvance = _nextEntityId;
                var hadBusinesses = Businesses.Businesses.Count > 0;
                Businesses.Advance(Topology, Population, ref _nextEntityId, occupancyFactor, Economy.Policy);
                if (ReassignUnproductiveLegacyWorkers())
                    Businesses.Advance(Topology, Population, ref _nextEntityId, occupancyFactor, Economy.Policy);
                // Existing Outside jobs are valid livelihoods. Recruit them only when a
                // new or replacement tenant creates openings, not on every leasing pulse.
                if (hadBusinesses && _nextEntityId > nextIdBeforeBusinessAdvance &&
                    Leasing.MatchExistingOutsideWorkers(Topology, Population, Businesses,
                        occupancyFactor, person => !Transit.IsPersonTravelling(person.Id)) > 0)
                    Businesses.Advance(Topology, Population, ref _nextEntityId, occupancyFactor, Economy.Policy);
                var arrivals = Leasing.EvaluateLeasingDemand(Topology, Population, ElevatorBank,
                    RandomStream, currentTick, ref _nextEntityId, Businesses,
                    occupancyFactor);
                foreach (var person in arrivals)
                {
                    var moveIn = TripGenerator.CreateMoveInTrip(person, currentTick);
                    if (moveIn != null) Transit.SubmitTrip(moveIn, Topology, currentTick, Population);
                }
            }

            // 2. Daily cash settlement (default one in-game day; injectable for focused domain fixtures).
            if (currentTick.Value % _settlementPeriod == 0)
            {
                // Close the interval that ended at this boundary before its counters are reset.
                // It includes income/rent from the previous settlement plus purchases and
                // essential shortfalls recorded by trips during the interval. The first
                // boundary closes the empty initial interval, then opens the first full day.
                for (var i = 0; i < Population.Households.Count; i++) Population.Households[i].CompleteDailyBudgetSettlement();
                for (var i = 0; i < Population.Households.Count; i++) Population.Households[i].BeginDailySettlement();
                Businesses.ProcessPayroll(Population, Topology,
                    CalculateResidentialOccupancyFactor() * CivilActions.BusinessOutputMultiplier);
                ProcessOutsidePayroll();
                RecordDailyCreditRepayments();
                Economy.ProcessRentCycle(Topology, Population, CivilActions.ResidentialRentCollectionMultiplier);
                Businesses.ProcessBusinessCycle(Topology, Population, Economy,
                    CalculateResidentialOccupancyFactor() * CivilActions.BusinessOutputMultiplier,
                    Economy.Policy, payrollAlreadyProcessed: true);
                Operations.AdvanceDaily(Underground, Population, Economy, OutsideMarket, Scrutiny, currentTick.Value);
                var utilityCellCount = CountUtilityCells();
                var upkeep = utilityCellCount + (ElevatorBank.Cars.Count * 2L);
                Economy.ChargeDailyExpense(upkeep, subsidy: false);
                Economy.ChargeDailyExpense(Economy.Policy.DailyTransitSubsidy, subsidy: true);
                for (var i = 0; i < Population.Households.Count; i++)
                {
                    Population.Households[i].UpdateArrearsDays();
                }
                var simulationDay = currentTick.Value / _settlementPeriod;
                var housing = HousingLifecycleSystem.AdvanceDaily(HousingLifecycle, Population, simulationDay);
                StartEligibleMoveOuts(housing, currentTick);
                Economy.CompleteDailySettlement(currentTick.Value);
                Factions.Evaluate(Population, Topology, Businesses, Economy.Policy, currentTick.Value);
                CivilActions.Evaluate(currentTick.Value, BuildCivilActionSignals(), Scrutiny.Value);
                Decisions.ObserveSettlement(this);
                Decisions.RecordCivilPhases(this);
            }

            // 3. Generate scheduled routine trips when schedule blocks transition
            var simulationDayNow = currentTick.Value / _settlementPeriod;
            var trips = TripGenerator.GenerateTripsForTick(previousTick, currentTick, Population,
                person => !HousingLifecycle.IsMoveOutEligible(person.HouseholdId, simulationDayNow));
            for (var i = 0; i < trips.Count; i++)
            {
                Transit.SubmitTrip(trips[i], Topology, currentTick, Population, HandleOutsideTripCompleted);
            }

            // 4. Advance transit execution (walking legs, elevator queues, riding cars)
            Transit.Advance(currentTick, Topology, ElevatorBank, Population, CivilActions.LobbyCapacityMultiplier,
                HandleOutsideTripCompleted);
            CompleteDepartedHouseholds(simulationDayNow);
        }

        private void ProcessOutsidePayroll()
        {
            _outsideWorkers.Clear();
            for (var i = 0; i < Population.Persons.Count; i++)
            {
                var person = Population.Persons[i];
                if (person.WorkplaceLocation.IsOutside) _outsideWorkers.Add(person);
            }
            _outsideWorkers.Sort((left, right) => left.Id.CompareTo(right.Id));
            for (var i = 0; i < _outsideWorkers.Count; i++)
            {
                var person = _outsideWorkers[i];
                if (HousingLifecycle.IsMoveOutEligible(person.HouseholdId,
                    Clock.CurrentTick.Value / _settlementPeriod)) continue;
                if (Population.TryGetHousehold(person.HouseholdId, out var household))
                    OutsideMarket.RecordWagePayment(household, OutsideDailyWage);
            }
        }

        /// <summary>
        /// Migrates old saves whose room rosters exceed the number of jobs a room can use
        /// at full demand. A temporary output shock or insolvent tenant does not erase a
        /// valid job slot; those workers can return when demand or the tenant recovers.
        /// </summary>
        private bool ReassignUnproductiveLegacyWorkers()
        {
            var changed = false;
            for (var personIndex = 0; personIndex < Population.Persons.Count; personIndex++)
            {
                var person = Population.Persons[personIndex];
                if (person.WorkplaceLocation.IsOutside || Transit.IsPersonTravelling(person.Id)) continue;
                BusinessRecord tenant = null;
                Room room = null;
                for (var businessIndex = 0; businessIndex < Businesses.Businesses.Count; businessIndex++)
                {
                    var candidate = Businesses.Businesses[businessIndex];
                    if (!candidate.RoomId.Equals(person.WorkplaceRoomId)) continue;
                    tenant = candidate;
                    Topology.TryGetRoom(candidate.RoomId, out room);
                    break;
                }
                if (tenant != null && tenant.IsInsolvent) continue;
                var hasProductiveSlot = false;
                if (tenant != null && room != null)
                {
                    var slots = tenant.GetProductiveStaffCapacity(room, 1f);
                    for (var slot = 0; slot < slots && slot < tenant.EmployeeIds.Count; slot++)
                        if (tenant.EmployeeIds[slot].Equals(person.Id))
                        {
                            hasProductiveSlot = true;
                            break;
                        }
                }
                if (!hasProductiveSlot && person.ReassignToOutsideWork()) changed = true;
            }
            return changed;
        }

        private void RecordDailyCreditRepayments()
        {
            _settlementHouseholds.Clear();
            for (var i = 0; i < Population.Households.Count; i++) _settlementHouseholds.Add(Population.Households[i]);
            _settlementHouseholds.Sort((left, right) => left.Id.CompareTo(right.Id));
            for (var i = 0; i < _settlementHouseholds.Count; i++)
            {
                var household = _settlementHouseholds[i];
                OutsideMarket.RecordCreditRepayment(household, household.DailyCreditRepayment);
            }
        }

        private void HandleOutsideTripCompleted(TripRecord trip, PersonRecord person)
        {
            if (trip == null || person == null || !trip.Destination.IsOutside) return;
            if (trip.Purpose == TripPurpose.MoveOut)
            {
                CompleteDepartedHouseholds(Clock.CurrentTick.Value / _settlementPeriod);
                return;
            }
            if (trip.Purpose != TripPurpose.Food) return;
            if (!Population.TryGetHousehold(person.HouseholdId, out var household)) return;
            var creditLimit = household.MemberIds.Count * OutsideDailyMealPrice * 45L;
            var purchase = OutsideMarket.TryPurchase(household, OutsideDailyMealPrice, creditLimit, OutsideServiceCategory.EssentialFood);
            if (!purchase.Accepted)
            {
                household.RecordEssentialShortfall(OutsideDailyMealPrice);
                person.BlockOutsideFoodUntil(Clock.CurrentTick.Value + OutsideFoodRetryCooldownTicks);
                var activeLabel = person.Schedule.ActiveLabelAt(Clock.CurrentTick);
                person.UpdateActivity(person.WorkplaceLocation.IsOutside &&
                    activeLabel == DailySchedule.LabelWork ? ActivityKind.Working : ActivityKind.Idle);
            }
        }

        private void StartEligibleMoveOuts(IReadOnlyList<HouseholdHousingLifecycleProjection> housing, Tick tick)
        {
            if (housing == null) return;
            for (var i = 0; i < housing.Count; i++)
            {
                if (!housing[i].IsMoveOutEligible || !Population.TryGetHousehold(housing[i].HouseholdId, out var household)) continue;
                for (var memberIndex = 0; memberIndex < household.MemberIds.Count; memberIndex++)
                {
                    if (!Population.TryGetPerson(household.MemberIds[memberIndex], out var person) ||
                        person.CurrentLocation.IsOutside || Transit.IsPersonTravelling(person.Id)) continue;
                    var trip = TripGenerator.CreateMoveOutTrip(person, tick);
                    if (trip != null) Transit.SubmitTrip(trip, Topology, tick, Population, HandleOutsideTripCompleted);
                }
            }
        }

        private void CompleteDepartedHouseholds(long simulationDay)
        {
            for (var householdIndex = Population.Households.Count - 1; householdIndex >= 0; householdIndex--)
            {
                var household = Population.Households[householdIndex];
                if (!HousingLifecycle.IsMoveOutEligible(household.Id, simulationDay)) continue;
                var allOutside = household.MemberIds.Count > 0;
                for (var memberIndex = 0; memberIndex < household.MemberIds.Count; memberIndex++)
                {
                    if (!Population.TryGetPerson(household.MemberIds[memberIndex], out var person) ||
                        !person.CurrentLocation.IsOutside || Transit.IsPersonTravelling(person.Id))
                    {
                        allOutside = false;
                        break;
                    }
                }
                if (!allOutside) continue;

                HousingLifecycle.RecordDeparture(household.Id, simulationDay, household.CashBalance,
                    household.RentArrearsBalance, household.OutsideCreditBalance);
                if (Population.RemoveHouseholdAndMembers(household.Id, out _))
                    HousingLifecycle.RemoveHousehold(household.Id);
            }
        }

        public const long OutsideDailyWage = 24;
        public const long OutsideDailyMealPrice = 8;
        public const long OutsideFoodRetryCooldownTicks = 30;

        public void SyncTransitServices()
        {
            var currentGraph = Topology.TransitGraph;
            Planner = new TransitRoutePlanner(currentGraph);
            TripGenerator.UpdateTopology(Topology.ToSnapshot(), currentGraph, Planner);
        }

        private float CalculateResidentialOccupancyFactor()
        {
            var residentialCapacity = 0;
            foreach (var room in Topology.Rooms.Values)
            {
                var content = room.ContentType.Value ?? string.Empty;
                if (content.StartsWith("residential:", StringComparison.Ordinal)) residentialCapacity += room.Capacity;
            }

            return residentialCapacity <= 0 ? 0f : Math.Min(1f, Population.ResidentCount / (float)residentialCapacity);
        }

        private int CountUtilityCells()
        {
            var cells = 0;
            foreach (var room in Topology.Rooms.Values)
            {
                var content = room.ContentType.Value ?? string.Empty;
                if (content.StartsWith("utility:", StringComparison.Ordinal)) cells += room.Bounds.Width;
            }
            return cells;
        }

        // ── Command Seam & Validation ─────────────────────────────────────────

        public CommandResult CanExecute(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            // Scrutiny is an event-pressure mechanic, never an instant build
            // blocker: high scrutiny raises inspection-event likelihood
            // (ExternalEventPressure, consumed by the crisis-event system) but
            // expansion commands are always validated on economy and topology.
            switch (command)
            {
                case BuildFloorSlabCommand slabCmd:
                {
                    if (slabCmd.MinX > slabCmd.MaxX) return InvalidBoundsRejection();
                    var cost = Economy.CalculateFloorSlabCost(slabCmd.Bounds);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for floor slab ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(slabCmd);
                }

                case ExpandGroundSlabCommand groundExpansionCmd:
                {
                    if (groundExpansionCmd.MinX > groundExpansionCmd.MaxX) return InvalidBoundsRejection();
                    if (!Topology.TryGetFloorSlab(0, out var existingGround)) return Topology.CanExecute(groundExpansionCmd);
                    var cost = Economy.CalculateGroundSlabExpansionCost(existingGround, groundExpansionCmd.Bounds);
                    if (!Economy.CanAfford(cost))
                        return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for ground slab expansion ({cost} required, Treasury: {Economy.CashBalance}).") });
                    return Topology.CanExecute(groundExpansionCmd);
                }

                case DigUndergroundCommand digCmd:
                {
                    if (!Underground.CanExcavate(digCmd.X, digCmd.Depth, digCmd.Size))
                        return CommandResult.Fail("The square dig brush must fit inside the underground earth grid.", "underground:outside_bounds");
                    return CommandResult.Success();
                }

                case BuildUndergroundFloorCommand floorCmd:
                {
                    if (!Underground.CanBuildFloor(floorCmd.X, floorCmd.Depth, floorCmd.Size))
                        return CommandResult.Fail("Lair floor can only be built in an excavated, unfloored square.", "underground:floor_requires_open_earth");
                    return CommandResult.Success();
                }

                case BuildUndergroundCoreCommand coreCmd:
                {
                    if (!Underground.CanBuildCore(coreCmd.X, coreCmd.Depth))
                        return CommandResult.Fail("Access core requires a continuous floored column at the lobby center.", "underground:core_requires_floor");
                    var cost = 100L * (coreCmd.Depth + 1 - Underground.ServiceShaftCells.Count);
                    return Economy.CanAfford(cost) ? CommandResult.Success() : CommandResult.Fail("Insufficient funds for access core.", "economy:insufficient_funds");
                }

                case BuildUndergroundCorridorCommand corridorCmd:
                {
                    if (!Underground.CanBuildCorridor(corridorCmd.X, corridorCmd.Depth, corridorCmd.Width))
                        return CommandResult.Fail("Corridor requires floored cells joined to the access network.", "underground:corridor_requires_access");
                    var newCells = 0;
                    for (var dx = 0; dx < corridorCmd.Width; dx++) if (!Underground.IsCorridor(corridorCmd.X + dx, corridorCmd.Depth)) newCells++;
                    return Economy.CanAfford(30L * newCells) ? CommandResult.Success() : CommandResult.Fail("Insufficient funds for corridor.", "economy:insufficient_funds");
                }

                case ZoneUndergroundRoomCommand zoneCmd:
                {
                    if (!Underground.CanZoneRoom(zoneCmd.Type, zoneCmd.X, zoneCmd.Depth, zoneCmd.Width, zoneCmd.Height))
                        return CommandResult.Fail("Room requires a clear floored footprint and corridor entrance.", "underground:room_requires_access");
                    var cost = UndergroundRoomCatalog.Get(zoneCmd.Type).BaseCost + 10L * zoneCmd.Width * zoneCmd.Height;
                    return Economy.CanAfford(cost) ? CommandResult.Success() : CommandResult.Fail("Insufficient funds for underground room.", "economy:insufficient_funds");
                }

                case SetUndergroundPolicyCommand policyCmd:
                    return policyCmd.CoverPriority >= 0 && policyCmd.CoverPriority <= 2 &&
                           policyCmd.StaffingPriority >= 0 && policyCmd.StaffingPriority <= 2 &&
                           policyCmd.SecurityPosture >= 0 && policyCmd.SecurityPosture <= 2
                        ? CommandResult.Success()
                        : CommandResult.Fail("Underground policy priorities must be between 0 and 2.", "underground:invalid_policy");

                case BuildRoomCommand roomCmd:
                {
                    if (roomCmd.MinX > roomCmd.MaxX) return InvalidBoundsRejection();
                    var cost = Economy.CalculateRoomCost(roomCmd.ContentType, roomCmd.Bounds);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for room ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(roomCmd);
                }

                case AddElevatorShaftCommand shaftCmd:
                {
                    var newFloors = CountNewShaftFloors(shaftCmd);
                    if (newFloors <= 0)
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("transit:shaft_exists"), "An identical elevator shaft already spans these floors.")
                        });
                    }
                    var cost = Economy.CalculateElevatorShaftCost(newFloors, shaftCmd.ShaftMaxX - shaftCmd.ShaftMinX + 1);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for elevator shaft ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(shaftCmd);
                }

                case BuildStairwellCommand stairCmd:
                {
                    var newFloors = CountNewStairFloors(stairCmd);
                    if (newFloors <= 0)
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("transit:stair_exists"), "An identical stairwell already spans these floors.")
                        });
                    }
                    var cost = Economy.CalculateStairwellCost(newFloors, stairCmd.StairMaxX - stairCmd.StairMinX + 1);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for stairwell ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(stairCmd);
                }

                case DemolishRoomCommand demoCmd:
                {
                    if (!Topology.Rooms.TryGetValue(demoCmd.RoomId, out var room))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("topology:room_not_found"), $"Room {demoCmd.RoomId} not found.")
                        });
                    }

                    var contentVal = room.ContentType.Value ?? "";
                    if (contentVal.Contains("lobby"))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("demolish:protected"), "Cannot demolish main reception lobby.")
                        });
                    }

                    if (contentVal.Contains("elevator_shaft"))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("demolish:protected"), "Elevator shafts cannot be demolished with room bulldozer.")
                        });
                    }

                    if (HasResidentsOrTripsReferencingRoom(demoCmd.RoomId))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("demolish:occupied"),
                                "Cannot demolish a room used as a resident's home or current location, or referenced by an active trip.")
                        });
                    }

                    return Topology.CanExecute(demoCmd);
                }

                case AddElevatorCarCommand carCmd:
                {
                    if (carCmd.StartingFloor < 0 || carCmd.StartingFloor >= Topology.FloorCount)
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("transit:invalid_floor"), $"Elevator car must be placed within active tower floors (0..{Topology.FloorCount - 1}).")
                        });
                    }

                    if (ElevatorBank.Cars.Count >= ElevatorBank.MaxCarsPerBank)
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("transit:max_cars"), $"Elevator bank has reached maximum capacity ({ElevatorBank.MaxCarsPerBank} cars).")
                        });
                    }

                    if (!Economy.CanAfford(TowerEconomyState.ElevatorCarCost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for elevator car ({TowerEconomyState.ElevatorCarCost} required).")
                        });
                    }

                    return CommandResult.Success();
                }

                case SetPolicyDecreeCommand:
                    return CommandResult.Success();

                default:
                    return CommandResult.Reject(new[]
                    {
                        new CommandRejectionReason(new ContentId("command:unknown"), $"Unsupported command type '{command.GetType().Name}'.")
                    });
            }
        }

        private static CommandResult InvalidBoundsRejection() => CommandResult.Reject(new[]
        {
            new CommandRejectionReason(new ContentId("topology:invalid_bounds"), "MinX cannot exceed MaxX.")
        });

        public CommandResult ExecuteCommand(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            switch (command)
            {
                case BuildFloorSlabCommand slabCmd:
                    return BuildFloorSlab(slabCmd);
                case ExpandGroundSlabCommand groundExpansionCmd:
                    return ExpandGroundSlab(groundExpansionCmd);
                case DigUndergroundCommand digCmd:
                {
                    var canDig = CanExecute(digCmd);
                    if (!canDig.Accepted) return canDig;
                    if (!Underground.Excavate(digCmd.X, digCmd.Depth, digCmd.Size)) return CommandResult.Success();
                    var evt = new DomainEvent(new EntityId(_nextEntityId++), new ContentId("event:earth_excavated"), Clock.CurrentTick);
                    return CommandResult.Accept(new[] { evt });
                }
                case BuildUndergroundFloorCommand floorCmd:
                {
                    var canBuild = CanExecute(floorCmd);
                    if (!canBuild.Accepted) return canBuild;
                    if (!Underground.BuildFloor(floorCmd.X, floorCmd.Depth, floorCmd.Size)) return CommandResult.Success();
                    var evt = new DomainEvent(new EntityId(_nextEntityId++), new ContentId("event:lair_floor_built"), Clock.CurrentTick);
                    return CommandResult.Accept(new[] { evt });
                }
                case BuildUndergroundCoreCommand coreCmd:
                {
                    var result = CanExecute(coreCmd);
                    if (!result.Accepted) return result;
                    var cost = 100L * (coreCmd.Depth + 1 - Underground.ServiceShaftCells.Count);
                    Economy.TryDeduct(cost);
                    Underground.BuildCore(coreCmd.X, coreCmd.Depth);
                    return CommandResult.Success();
                }
                case BuildUndergroundCorridorCommand corridorCmd:
                {
                    var result = CanExecute(corridorCmd);
                    if (!result.Accepted) return result;
                    var newCells = 0;
                    for (var dx = 0; dx < corridorCmd.Width; dx++) if (!Underground.IsCorridor(corridorCmd.X + dx, corridorCmd.Depth)) newCells++;
                    Economy.TryDeduct(30L * newCells);
                    Underground.BuildCorridor(corridorCmd.X, corridorCmd.Depth, corridorCmd.Width);
                    return CommandResult.Success();
                }
                case ZoneUndergroundRoomCommand zoneCmd:
                {
                    var result = CanExecute(zoneCmd);
                    if (!result.Accepted) return result;
                    Economy.TryDeduct(UndergroundRoomCatalog.Get(zoneCmd.Type).BaseCost + 10L * zoneCmd.Width * zoneCmd.Height);
                    Underground.ZoneRoom(_nextEntityId++, zoneCmd.Type, zoneCmd.X, zoneCmd.Depth, zoneCmd.Width, zoneCmd.Height);
                    return CommandResult.Success();
                }
                case SetUndergroundPolicyCommand policyCmd:
                {
                    var result = CanExecute(policyCmd);
                    if (!result.Accepted) return result;
                    Operations.SetPolicy(policyCmd.CoverPriority, policyCmd.StaffingPriority, policyCmd.SecurityPosture);
                    return CommandResult.Success();
                }
                case BuildRoomCommand roomCmd:
                    return BuildRoom(roomCmd);
                case AddElevatorShaftCommand shaftCmd:
                    return AddElevatorShaft(shaftCmd);
                case BuildStairwellCommand stairCmd:
                    return BuildStairwell(stairCmd);
                case DemolishRoomCommand demoCmd:
                    return DemolishRoom(demoCmd);
                case AddElevatorCarCommand carCmd:
                {
                    var canExec = CanExecute(carCmd);
                    if (!canExec.Accepted) return canExec;
                    AddElevatorCarUnchecked(carCmd.Capacity, carCmd.StartingFloor);
                    return CommandResult.Success();
                }
                case SetPolicyDecreeCommand policyCmd:
                    return ApplyPolicyDecree(policyCmd);
                default:
                    return CommandResult.Reject(new[]
                    {
                        new CommandRejectionReason(new ContentId("command:unknown"), $"Unsupported command type '{command.GetType().Name}'.")
                    });
            }
        }

        private CommandResult ApplyPolicyDecree(SetPolicyDecreeCommand command)
        {
            var nextPolicy = command.Policy;
            if (Economy.Policy == nextPolicy) return CommandResult.Success();

            var oldPolicy = Economy.Policy;
            Economy.SetPolicy(nextPolicy);
            if (nextPolicy.IsAggressive)
            {
                var severity = (nextPolicy.RentCapMultiplier == PolicyDecreeState.HighRentCapMultiplier ? .08f : 0f) +
                               (nextPolicy.CommercialTaxRate == PolicyDecreeState.HighCommercialTaxRate ? .08f : 0f);
                Scrutiny.RecordAggressivePolicy(severity);
            }
            Decisions.RecordDecree(this, oldPolicy, nextPolicy);

            var changeEvent = new DomainEvent(
                new EntityId(_nextEntityId++),
                new ContentId("policy:decree_changed"),
                Clock.CurrentTick);
            return CommandResult.Accept(new[] { changeEvent });
        }

        public CommandResult BuildFloorSlab(BuildFloorSlabCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateFloorSlabCost(cmd.Bounds);
            if (!Economy.TryDeduct(cost))
            {
                return CommandResult.Reject(new[]
                {
                    new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for floor slab ({cost} required, Treasury: {Economy.CashBalance}).")
                });
            }
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (!result.Accepted)
            {
                Economy.RefundExpense(cost);
                return result;
            }
            Scrutiny.RecordExpansion(cmd.Bounds.Width);
            SyncTransitServices();

            return result;
        }

        public CommandResult ExpandGroundSlab(ExpandGroundSlabCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;
            if (!Topology.TryGetFloorSlab(0, out var existingGround))
            {
                return validation.Accepted ? Topology.CanExecute(cmd) : validation;
            }
            var cost = Economy.CalculateGroundSlabExpansionCost(existingGround, cmd.Bounds);
            var addedCells = Math.Max(0, cmd.Bounds.Width - existingGround.Width);
            if (!Economy.TryDeduct(cost))
            {
                return CommandResult.Reject(new[]
                {
                    new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for ground slab expansion ({cost} required, Treasury: {Economy.CashBalance}).")
                });
            }
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (!result.Accepted)
            {
                Economy.RefundExpense(cost);
                return result;
            }
            Scrutiny.RecordExpansion(addedCells);
            SyncTransitServices();
            return result;
        }

        public CommandResult BuildRoom(BuildRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateRoomCost(cmd.ContentType, cmd.Bounds);
            if (!Economy.TryDeduct(cost))
            {
                return CommandResult.Reject(new[]
                {
                    new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for room ({cost} required, Treasury: {Economy.CashBalance}).")
                });
            }
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (!result.Accepted)
            {
                Economy.RefundExpense(cost);
                return result;
            }
            Scrutiny.RecordExpansion(cmd.Bounds.Width);
            var content = cmd.ContentType.Value ?? string.Empty;
            if (content.Contains("diner") || content.Contains("amenity") || content.Contains("service")) Scrutiny.RecordCapacityOrServiceInvestment();
            SyncTransitServices();

            return result;
        }

        public CommandResult AddElevatorShaft(AddElevatorShaftCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateElevatorShaftCost(Math.Max(1, CountNewShaftFloors(cmd)), cmd.ShaftMaxX - cmd.ShaftMinX + 1);
            if (!Economy.TryDeduct(cost))
            {
                return CommandResult.Reject(new[]
                {
                    new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for elevator shaft ({cost} required, Treasury: {Economy.CashBalance}).")
                });
            }
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (!result.Accepted)
            {
                Economy.RefundExpense(cost);
                return result;
            }
            Scrutiny.RecordExpansion(cmd.FloorSpan);
            ElevatorBank.ExpandFloorRange(cmd.BottomFloor, cmd.TopFloor);
            SyncTransitServices();

            return result;
        }

        public CommandResult BuildStairwell(BuildStairwellCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateStairwellCost(Math.Max(1, CountNewStairFloors(cmd)), cmd.StairMaxX - cmd.StairMinX + 1);
            if (!Economy.TryDeduct(cost))
            {
                return CommandResult.Reject(new[]
                {
                    new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for stairwell ({cost} required, Treasury: {Economy.CashBalance}).")
                });
            }
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (!result.Accepted)
            {
                Economy.RefundExpense(cost);
                return result;
            }
            Scrutiny.RecordExpansion(cmd.FloorSpan);
            SyncTransitServices();

            return result;
        }

        public CommandResult DemolishRoom(DemolishRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            if (Topology.Rooms.TryGetValue(cmd.RoomId, out var room))
            {
                var cost = Economy.CalculateRoomCost(room.ContentType, room.Bounds);
                var salvageRefund = cost / 2;

                var result = Topology.Execute(cmd, Clock.CurrentTick);
                if (result.Accepted)
                {
                    if (salvageRefund > 0)
                    {
                        Economy.RecordConstructionSalvage(salvageRefund);
                    }
                    Businesses.RemoveBusinessForRoom(cmd.RoomId);
                    ReassignUnproductiveLegacyWorkers();
                    SyncTransitServices();
                }

                return result;
            }

            var fallbackResult = Topology.Execute(cmd, Clock.CurrentTick);
            if (fallbackResult.Accepted)
            {
                Businesses.RemoveBusinessForRoom(cmd.RoomId);
                ReassignUnproductiveLegacyWorkers();
                SyncTransitServices();
            }

            return fallbackResult;
        }

        private bool HasResidentsOrTripsReferencingRoom(EntityId roomId)
        {
            var people = Population.Persons;
            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person.HomeRoomId.Equals(roomId) ||
                    (!person.CurrentLocation.IsOutside && person.CurrentLocation.RoomId.Equals(roomId)))
                {
                    return true;
                }
            }

            var graph = Topology.TransitGraph;
            var activeTrips = Transit.ActiveTrips;
            for (var i = 0; i < activeTrips.Count; i++)
            {
                var trip = activeTrips[i].Trip;
                if (LocationReferencesRoom(trip.Origin, roomId) || LocationReferencesRoom(trip.Destination, roomId))
                    return true;

                // A trip may use this room's portal as an intermediate corridor node,
                // even when the room is neither its origin nor its destination.
                var legs = activeTrips[i].Route?.Legs;
                if (legs == null) continue;
                for (var legIndex = 0; legIndex < legs.Count; legIndex++)
                {
                    if (NodeReferencesRoom(graph, legs[legIndex].FromNodeId, roomId) ||
                        NodeReferencesRoom(graph, legs[legIndex].ToNodeId, roomId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool LocationReferencesRoom(WorldLocation location, EntityId roomId) =>
            !location.IsOutside && location.RoomId.Equals(roomId);

        private static bool NodeReferencesRoom(HierarchicalTransitGraph graph, EntityId nodeId, EntityId roomId) =>
            graph != null && graph.TryGetNode(nodeId, out var node) && node.RoomId.HasValue &&
            node.RoomId.Value.Equals(roomId);

        public CommandResult AddElevatorCar(int capacity = 10, int startingFloor = 0)
        {
            return ExecuteCommand(new AddElevatorCarCommand(capacity, startingFloor));
        }

        private void AddElevatorCarUnchecked(int capacity, int startingFloor)
        {
            var carId = new EntityId(_nextElevatorCarId++);
            var car = new ElevatorCar(carId, startingFloor, capacity);
            ElevatorBank.AddCar(car);
            Economy.TryDeduct(TowerEconomyState.ElevatorCarCost);
            Scrutiny.RecordCapacityOrServiceInvestment();
        }

        /// <summary>
        /// Counts floors in the shaft span that lack an identical shaft room, so
        /// extensions only charge for new construction instead of the full span.
        /// </summary>
        private int CountNewShaftFloors(AddElevatorShaftCommand cmd)
        {
            var shaftContentType = new ContentId("transit:elevator_shaft");
            var count = 0;
            for (var floor = cmd.BottomFloor; floor <= cmd.TopFloor; floor++)
            {
                var rooms = Topology.GetRoomsOnFloor(floor);
                var hasIdentical = false;
                for (var i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].ContentType == shaftContentType &&
                        rooms[i].Bounds.MinX == cmd.ShaftMinX &&
                        rooms[i].Bounds.MaxX == cmd.ShaftMaxX)
                    {
                        hasIdentical = true;
                        break;
                    }
                }
                if (!hasIdentical) count++;
            }
            return count;
        }

        private int CountNewStairFloors(BuildStairwellCommand cmd)
        {
            var stairContentType = new ContentId("amenity:stairwell");
            var count = 0;
            for (var floor = cmd.BottomFloor; floor <= cmd.TopFloor; floor++)
            {
                var rooms = Topology.GetRoomsOnFloor(floor);
                var hasIdentical = false;
                for (var i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].ContentType == stairContentType &&
                        rooms[i].Bounds.MinX == cmd.StairMinX &&
                        rooms[i].Bounds.MaxX == cmd.StairMaxX)
                    {
                        hasIdentical = true;
                        break;
                    }
                }
                if (!hasIdentical) count++;
            }
            return count;
        }

        public ResidentSpatialPosition GetResidentPosition(EntityId personId)
        {
            return Transit.GetResidentPosition(personId, Population, Topology);
        }

        public (long MaxWaitTicks, float AverageWaitTicks) GetFloorWaitMetrics(int floor)
        {
            return ElevatorBank.GetFloorWaitMetrics(floor);
        }

        public int GetQueueLength(int floor)
        {
            return ElevatorBank.GetQueueLength(floor);
        }

        /// <summary>
        /// Seeds the elevator bank with a synthetic morning-rush queue (one passenger per floor cycle)
        /// using entity IDs allocated from the simulation's own counter so they cannot collide with
        /// real <see cref="PopulationState"/> person IDs.  Only runs when the bank queue is empty;
        /// calling it multiple times is safe.
        /// </summary>
        public void SeedMorningRush()
        {
            if (ElevatorBank.TotalQueuedCount > 0)
            {
                return;
            }

            var floorRange = ElevatorBank.MaxFloor - ElevatorBank.MinFloor;
            if (floorRange <= 0) return;

            const int residentCount = 50;
            for (var i = 0; i < residentCount; i++)
            {
                var passengerEntityId = new EntityId(_nextEntityId++);
                var destinationFloor = ElevatorBank.MinFloor + 1 + (i % floorRange);
                ElevatorBank.EnqueuePassenger(
                    new ElevatorPassenger(passengerEntityId, ElevatorBank.MinFloor, destinationFloor));
            }
        }


        public static TowerSimulation CreateStandardFiveFloor(
            TowerEconomyState economy = null,
            IRandomStream randomStream = null,
            bool enqueueMorningRush = false,
            long settlementPeriod = DailySchedule.TicksPerDay)
        {
            var clock = new SimulationClock(new Tick(0));
            var topologyState = BuildingTopologyState.CreateWithFixture();
            var population = FiftyResidentFixture.Create();

            var car = new ElevatorCar(new EntityId(501), startingFloor: 0, capacity: 10);
            var elevatorBank = new ElevatorBank(minFloor: 0, maxFloor: 4, new[] { car });

            var sim = new TowerSimulation(
                clock,
                topologyState,
                population,
                elevatorBank,
                economy,
                randomStream,
                settlementPeriod: settlementPeriod);

            if (enqueueMorningRush)
            {
                // Generate real morning commute trips for residents from their apartments on Floors 1-4
                // down to their workplace (Diner on Floor 0) using the full transit system.
                EntityId? dinerId = population.Persons.Count > 0 ? population.Persons[0].WorkplaceRoomId : (EntityId?)null;
                var nextTripId = 1000;

                foreach (var person in population.Persons)
                {
                    var originRoomId = person.HomeRoomId;
                    var destinationRoomId = person.WorkplaceRoomId.IsValid
                        ? person.WorkplaceRoomId
                        : (dinerId ?? originRoomId);

                    if (!originRoomId.Equals(destinationRoomId))
                    {
                        var originNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(originRoomId);
                        var destNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(destinationRoomId);
                        TransitRoute route = null;
                        if (originNode != null && destNode != null)
                        {
                            route = sim.Planner.FindRoute(originNode.Id, destNode.Id);
                        }

                        if (route != null)
                        {
                            var trip = new TripRecord(
                                new EntityId(nextTripId++),
                                person.Id,
                                originRoomId,
                                destinationRoomId,
                                TripPurpose.Work,
                                clock.CurrentTick,
                                route);

                            sim.Transit.SubmitTrip(trip, topologyState, clock.CurrentTick, population);
                        }
                    }
                }
            }

            return sim;
        }

        /// <summary>
        /// Ground-floor start for from-scratch play: a single ground slab with a
        /// lobby shell and elevator shaft, zero residents, and a funded treasury.
        /// The player expands upward, adds power/water, zones rooms, and the
        /// demand-driven leasing system moves residents in as homes and
        /// workplaces appear. Economy, utilities, commute, routines, and
        /// expansion all run through the standard tick loop from tick zero.
        /// </summary>
        public static TowerSimulation CreateGroundFloorStart(long startingTreasury = TowerEconomyState.DefaultStartingTreasury, IRandomStream randomStream = null, long settlementPeriod = DailySchedule.TicksPerDay)
        {
            var clock = new SimulationClock(new Tick(0));
            var topologyState = new BuildingTopologyState(startingEntityId: 2000);

            var slab = new CellBounds(0, -14, 17);
            var shaftPortal = new Portal(new EntityId(11), PortalType.ElevatorShaftDoor, new CellCoordinate(0, 0), new EntityId(13));
            var lobbyPortal = new Portal(new EntityId(12), PortalType.Door, new CellCoordinate(14, 0), new EntityId(14));
            var shaftRoom = new Room(new EntityId(13), FiveFloorTopologyFixture.ElevatorShaftContentId, new CellBounds(0, 0, 1), new[] { shaftPortal.Id }, 10);
            var lobbyRoom = new Room(new EntityId(14), FiveFloorTopologyFixture.LobbyContentId, new CellBounds(0, 2, 14), new[] { lobbyPortal.Id }, 50);
            topologyState.RestoreFromData(
                new[] { slab },
                new[] { shaftRoom, lobbyRoom },
                new[] { shaftPortal, lobbyPortal });

            var population = new PopulationState(
                Array.Empty<PersonRecord>(),
                Array.Empty<HouseholdRecord>());

            var car = new ElevatorCar(new EntityId(501), startingFloor: 0, capacity: 10);
            var elevatorBank = new ElevatorBank(minFloor: 0, maxFloor: 0, new[] { car });

            return new TowerSimulation(
                clock,
                topologyState,
                population,
                elevatorBank,
                new TowerEconomyState(startingTreasury),
                randomStream,
                null,
                settlementPeriod);
        }

        public TowerSaveData ExportSaveData()
        {
            var data = new TowerSaveData
            {
                simulationTick = Clock.CurrentTick.Value,
                nextElevatorCarId = _nextElevatorCarId,
                nextEntityId = _nextEntityId
            };

            data.SetEconomySaveData(Economy.ToSaveData());
            data.undergroundCells = Underground.ToSaveData();
            data.undergroundFloorCells = Underground.FloorsToSaveData();
            data.undergroundGridV2 = true;
            data.undergroundGridV3 = true;
            data.undergroundCorridorCells = Underground.CorridorsToSaveData();
            data.undergroundShaftCells = Underground.ShaftToSaveData();
            data.undergroundCoreCell = Underground.AccessCore.HasValue
                ? new UndergroundCellSaveData { x = Underground.AccessCore.Value.X, depth = Underground.AccessCore.Value.Depth } : null;
            data.undergroundRooms = Underground.RoomsToSaveData();
            data.undergroundOperations = Operations.ToSaveData();
            data.SetTopologySaveData(Topology.ToSaveData());
            data.SetPopulationSaveData(Population.ToSaveData());
            data.SetOutsideMarketSaveData(OutsideMarket.ToSaveData());
            data.SetHouseholdLeaseLifecycleSaveData(HousingLifecycle.ToSaveData());
            data.elevatorBank = ElevatorBank.ToSaveData();
            data.activeTrips = Transit.ToSaveData();
            data.scrutiny = new ScrutinySaveData { value = Scrutiny.Value, previousValue = Scrutiny.PreviousValue, recentExpansionPressure = Scrutiny.RecentExpansionPressure, recentPolicyPressure = Scrutiny.RecentPolicyPressure };
            data.businesses = Businesses.ToSaveData();
            data.businessLifecycle = Businesses.ToLifecycleSaveData();
            data.utilityOperations = UtilityOperations.ToSaveData();
            data.factions = Factions.ToSaveData();
            data.civilActions = CivilActions.ToSaveData();
            data.decisions = Decisions.ToSaveData();

            return data;
        }

        public static TowerSimulation RestoreFromSaveData(TowerSaveData data, IRandomStream randomStream = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.floorSlabs == null || data.rooms == null || data.portals == null ||
                data.households == null || data.persons == null)
                throw new ArgumentException("Save payload is missing required tower data.", nameof(data));
            var tick = data.simulationTick >= 0 ? data.simulationTick : 0;
            var clock = new SimulationClock(new Tick(tick));
            var topology = BuildingTopologyState.FromSaveData(data.GetTopologySaveData(), data.nextEntityId > 0 ? data.nextEntityId : 3000);
            var population = PopulationState.FromSaveData(data.GetPopulationSaveData(), randomStream);
            var elevatorBank = ElevatorBank.FromSaveData(data.elevatorBank);
            var economy = TowerEconomyState.FromSaveData(data.GetEconomySaveData());
            var scrutiny = data.scrutiny == null ? new ScrutinyState() : new ScrutinyState(data.scrutiny.value, data.scrutiny.previousValue, data.scrutiny.recentExpansionPressure, data.scrutiny.recentPolicyPressure);

            var sim = new TowerSimulation(clock, topology, population, elevatorBank, economy, randomStream, scrutiny);
            topology.TryGetFloorSlab(0, out var groundSlab);
            sim.Underground = UndergroundDigState.FromSaveData(data.undergroundCells, data.undergroundFloorCells,
                !data.undergroundGridV2, groundSlab, !data.undergroundGridV3,
                data.undergroundCorridorCells, data.undergroundShaftCells, data.undergroundCoreCell, data.undergroundRooms);
            sim.Operations = UndergroundOperationsState.FromSaveData(data.undergroundOperations);
            sim.Businesses = BusinessState.FromSaveData(data.businesses);
            sim.Businesses.RestoreLifecycleTotals(data.businessLifecycle);
            sim.UtilityOperations = UtilityOperationsState.FromSaveData(data.utilityOperations);
            sim.OutsideMarket = OutsideMarketState.FromSaveData(data.GetOutsideMarketSaveData());
            sim.HousingLifecycle = HouseholdLeaseLifecycleState.FromSaveData(data.GetHouseholdLeaseLifecycleSaveData());
            sim.Factions = FactionState.FromSaveData(data.factions, population);
            sim.CivilActions = CivilActionState.FromSaveData(data.civilActions);
            sim.Decisions = DecisionRecordState.FromSaveData(data.decisions);
            sim._nextElevatorCarId = data.nextElevatorCarId > 0 ? data.nextElevatorCarId : 500;
            sim._nextEntityId = data.nextEntityId > 0 ? data.nextEntityId : 3000;

            sim.Transit.RestoreFromSaveData(data.activeTrips, sim.Topology, sim.Planner);
            sim.SyncTransitServices();
            return sim;
        }

        /// <summary>Safe boundary for loading untrusted or damaged save payloads.</summary>
        public static bool TryRestoreFromSaveData(TowerSaveData data, out TowerSimulation simulation, out string error,
            IRandomStream randomStream = null)
        {
            simulation = null;
            error = null;
            try
            {
                simulation = RestoreFromSaveData(data, randomStream);
                return true;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                error = "Save payload is invalid or damaged.";
                return false;
            }
        }
    }
}
