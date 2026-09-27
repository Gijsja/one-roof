using System;
using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class TowerSimulationTests
    {
        [Test]
        public void CreateStandardFiveFloor_InitializesWith50ResidentsAndOneCar()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            Assert.That(sim.ResidentCount, Is.EqualTo(50));
            Assert.That(sim.Topology.FloorCount, Is.EqualTo(5));
            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(1));
            Assert.That(sim.CurrentTick, Is.EqualTo(0));
            Assert.That(sim.ActiveTripCount, Is.EqualTo(0));
        }

        [Test]
        public void AdvanceOneTick_AdvancesClock()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            sim.AdvanceOneTick();

            Assert.That(sim.CurrentTick, Is.EqualTo(1));
        }

        [Test]
        public void MorningRushHour_ResidentsFormElevatorQueuesAndBoardCars()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // Run until tick 35 (morning commute period from DailySchedule)
            for (var tick = 0; tick < 35; tick++)
            {
                sim.AdvanceOneTick();
            }

            // Morning commute should generate trips for workers
            Assert.That(sim.ActiveTripCount + sim.ElevatorBank.DeliveredCount, Is.GreaterThan(0));

            // Queues or riders should appear in the elevator bank
            var totalTransitTraffic = sim.TotalQueuedElevatorPassengers + sim.ElevatorBank.Cars[0].Passengers.Count + sim.ElevatorBank.DeliveredCount;
            Assert.That(totalTransitTraffic, Is.GreaterThan(0));
        }

        [Test]
        public void AddingElevatorCar_IncreasesCarCountAndServicingCapacity()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(1));

            sim.AddElevatorCar(capacity: 10, startingFloor: 2);

            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(2));
        }

        [Test]
        public void TransitExecutionSystem_CompletesLocalTripImmediately()
        {
            var clock = new SimulationClock(new Tick(10));
            var topology = BuildingTopologyState.CreateWithFixture();
            var population = FiftyResidentFixture.Create();
            var car = new ElevatorCar(new EntityId(501), 0, 10);
            var bank = new ElevatorBank(0, 4, new[] { car });
            var transit = new TransitExecutionSystem();

            var room0 = topology.GetRoomsOnFloor(0)[0];
            var trip = new TripRecord(
                new EntityId(900),
                new EntityId(1),
                room0.Id,
                room0.Id,
                TripPurpose.Work,
                new Tick(10),
                null);

            transit.SubmitTrip(trip, topology, new Tick(10), population);

            Assert.That(trip.State, Is.EqualTo(TripState.Completed));
            Assert.That(transit.ActiveTripCount, Is.EqualTo(0));
        }

        [Test]
        public void OutsideFoodTrip_RecordsOneTransactionAndStartsMealEpisode()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var population = FiftyResidentFixture.Create();
            var person = population.Persons[0];
            person.UpdateLocation(WorldLocation.Outside);
            person.UpdateNeed(NeedKind.Hunger, 0.2f);
            var transit = new TransitExecutionSystem();
            var trip = new TripRecord(new EntityId(901), person.Id, WorldLocation.Outside,
                WorldLocation.Outside, TripPurpose.Food, new Tick(10), null);
            var completedTransactions = 0;

            Action<TripRecord, PersonRecord> recordPurchase = (completedTrip, resident) =>
            {
                Assert.That(completedTrip, Is.SameAs(trip));
                Assert.That(resident, Is.SameAs(person));
                completedTransactions++;
            };

            transit.SubmitTrip(trip, topology, new Tick(10), population, recordPurchase);
            transit.SubmitTrip(trip, topology, new Tick(11), population, recordPurchase);

            Assert.That(trip.State, Is.EqualTo(TripState.Completed));
            Assert.That(trip.OutsideServiceTransactionRecorded, Is.True);
            Assert.That(completedTransactions, Is.EqualTo(1));
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Eating));

            new ResidentNeedsSystem().AdvancePerson(person);
            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.GreaterThan(0.2f));
        }

        [Test]
        public void DailySettlement_PaysOutsideWorkerThroughOutsideMarket()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var fixturePopulation = FiftyResidentFixture.Create();
            var template = fixturePopulation.Persons[0];
            var householdId = new EntityId(7001);
            var personId = new EntityId(7002);
            var household = new HouseholdRecord(householdId, new[] { personId }, template.HomeRoomId,
                budget: 0.5f, satisfaction: 1f, cashBalance: 50);
            var outsideWorker = new PersonRecord(personId, householdId, template.HomeRoomId, default,
                template.Schedule, template.Needs, template.Traits, worksOutside: true);
            var population = new PopulationState(new[] { outsideWorker }, new[] { household });
            var elevatorBank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(7003), 0, 10) });
            var simulation = new TowerSimulation(new SimulationClock(new Tick(0)), topology, population,
                elevatorBank, new TowerEconomyState(initialTreasury: 0), settlementPeriod: 1);

            simulation.AdvanceOneTick();

            Assert.That(household.DailyOutsideWages, Is.EqualTo(TowerSimulation.OutsideDailyWage));
            Assert.That(simulation.OutsideMarket.WageOutflow, Is.EqualTo(TowerSimulation.OutsideDailyWage));
            Assert.That(household.CashBalance, Is.EqualTo(56)); // +18 outside pay, -12 residential rent.
        }

        [Test]
        public void OutsideFoodNeedThroughSimulation_BillsOnceAndRestoresHunger()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var template = FiftyResidentFixture.Create().Persons[0];
            var householdId = new EntityId(7101);
            var personId = new EntityId(7102);
            var household = new HouseholdRecord(householdId, new[] { personId }, template.HomeRoomId,
                budget: 0.5f, satisfaction: 1f, cashBalance: 50);
            var needs = new[]
            {
                new NeedState(NeedKind.Hunger, 0.2f),
                new NeedState(NeedKind.Energy, 1f),
                new NeedState(NeedKind.Hygiene, 1f),
                new NeedState(NeedKind.Social, 1f),
                new NeedState(NeedKind.Purpose, 1f)
            };
            var person = new PersonRecord(personId, householdId, template.HomeRoomId, default,
                template.Schedule, needs, template.Traits, worksOutside: true);
            person.UpdateLocation(WorldLocation.Outside);
            var population = new PopulationState(new[] { person }, new[] { household });
            var elevatorBank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(7103), 0, 10) });
            var simulation = new TowerSimulation(new SimulationClock(new Tick(100)), topology, population,
                elevatorBank, new TowerEconomyState(initialTreasury: 0));

            for (var i = 0; i < 12; i++) simulation.AdvanceOneTick();

            Assert.That(simulation.OutsideMarket.PurchaseRevenue, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));
            Assert.That(household.CashBalance, Is.EqualTo(50 - TowerSimulation.OutsideDailyMealPrice));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.GreaterThan(0.2f));
        }

        [Test]
        public void DailyBudgetWindow_IncludesOutsideMealOnceAfterBoundaryAndAcrossSaveLoad()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var template = FiftyResidentFixture.Create().Persons[0];
            var householdId = new EntityId(7201);
            var personId = new EntityId(7202);
            var household = new HouseholdRecord(householdId, new[] { personId }, template.HomeRoomId,
                budget: 0.5f, satisfaction: 1f, cashBalance: 50);
            var needs = new[]
            {
                new NeedState(NeedKind.Hunger, 0.2f),
                new NeedState(NeedKind.Energy, 1f),
                new NeedState(NeedKind.Hygiene, 1f),
                new NeedState(NeedKind.Social, 1f),
                new NeedState(NeedKind.Purpose, 1f)
            };
            var person = new PersonRecord(personId, householdId, template.HomeRoomId, default,
                template.Schedule, needs, template.Traits, worksOutside: true);
            person.UpdateLocation(WorldLocation.Outside);

            // Reserve every apartment so the normal leasing-demand loop cannot add unrelated
            // households while this small deterministic integration fixture runs.
            var households = new System.Collections.Generic.List<HouseholdRecord> { household };
            var nextPlaceholderId = 7300;
            foreach (var room in topology.Rooms.Values)
            {
                if (!room.ContentType.Value.StartsWith("residential:", StringComparison.Ordinal) ||
                    room.Id.Equals(template.HomeRoomId)) continue;
                households.Add(new HouseholdRecord(new EntityId(nextPlaceholderId++),
                    Array.Empty<EntityId>(), room.Id, budget: 0.5f, satisfaction: 1f, cashBalance: 0));
            }

            var population = new PopulationState(new[] { person }, households);
            var elevatorBank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(7203), 0, 10) });
            var simulation = new TowerSimulation(new SimulationClock(new Tick(1439)), topology, population,
                elevatorBank, new TowerEconomyState(initialTreasury: 0));

            // Tick 1440 opens the first funded budget window, then the outside meal completes
            // inside that same interval.
            simulation.AdvanceOneTick();
            for (var i = 0; i < 30 && simulation.OutsideMarket.PurchaseRevenue == 0; i++)
                simulation.AdvanceOneTick();
            Assert.That(simulation.OutsideMarket.PurchaseRevenue, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));

            // The partially accumulated interval must survive a save before its closing tick.
            simulation = TowerSimulation.RestoreFromSaveData(simulation.ExportSaveData());
            household = simulation.Population.GetHousehold(householdId);
            while (simulation.CurrentTick < 2879) simulation.AdvanceOneTick();

            var intervalIncome = household.DailyIncome;
            var intervalRent = household.DailyRentDue;
            var intervalSpend = household.DailyServiceSpend + household.DailyOutsideEssentialSpend +
                                household.DailyOutsideQualitySpend + household.DailyCareSpend;
            Assert.That(intervalIncome, Is.EqualTo(TowerSimulation.OutsideDailyWage));
            Assert.That(intervalRent, Is.EqualTo(12));
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(simulation.OutsideMarket.PurchaseRevenue));

            simulation.AdvanceOneTick(); // Tick 2880 closes the window before resetting counters.

            Assert.That(household.DailyBudgetNetFlow, Is.EqualTo(intervalIncome - intervalRent - intervalSpend));
            Assert.That(household.Rolling30DayNetFlow, Is.EqualTo(household.DailyBudgetNetFlow));
            Assert.That(household.DailyIncome, Is.EqualTo(TowerSimulation.OutsideDailyWage),
                "The new interval starts with its own payroll after the previous interval was recorded.");
        }

        [Test]
        public void AdvanceOneTick_PersistentNegativeBudgetAffectsHousingLifecycle()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var home = topology.Rooms.Values.First(room => room.ContentType.Value.StartsWith("residential:", StringComparison.Ordinal));
            var personId = new EntityId(7402);
            var householdId = new EntityId(7401);
            var negativeBudgetHistory = Enumerable.Repeat(-2L, HouseholdRecord.DailyBudgetHistoryCapacity).ToArray();
            var household = new HouseholdRecord(householdId, new[] { personId }, home.Id,
                budget: 0f, satisfaction: 1f, cashBalance: 0, outsideCreditBalance: 10,
                recentDailyBudgetNetFlows: negativeBudgetHistory);
            var template = FiftyResidentFixture.Create().Persons[0];
            var person = new PersonRecord(personId, householdId, home.Id, default,
                template.Schedule, template.Needs, template.Traits, worksOutside: true);
            var households = new System.Collections.Generic.List<HouseholdRecord> { household };
            var nextPlaceholderId = 7500;
            foreach (var room in topology.Rooms.Values)
            {
                if (!room.ContentType.Value.StartsWith("residential:", StringComparison.Ordinal) || room.Id.Equals(home.Id)) continue;
                households.Add(new HouseholdRecord(new EntityId(nextPlaceholderId++),
                    Array.Empty<EntityId>(), room.Id, budget: 0.5f, satisfaction: 1f, cashBalance: 0));
            }

            var simulation = new TowerSimulation(new SimulationClock(new Tick(0)), topology,
                new PopulationState(new[] { person }, households),
                new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(7403), 0, 10) }),
                new TowerEconomyState(initialTreasury: 0), settlementPeriod: 1);

            simulation.AdvanceOneTick();

            var housing = simulation.HousingLifecycleSystem.Evaluate(household,
                simulation.HousingLifecycle, simulation.CurrentSimulationDay);
            Assert.That(household.Rolling7DayNetFlow, Is.LessThan(0));
            Assert.That(housing.RoomCondition, Is.EqualTo(HousingConditionStage.Degraded));
            Assert.That(housing.HasNotice, Is.True);
        }

        [Test]
        public void OutsideWorker_CommutesEatsMidShiftResumesWorkAndReturnsHome()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var template = FiftyResidentFixture.Create().Persons[0];
            var householdId = new EntityId(7201);
            var personId = new EntityId(7202);
            var household = new HouseholdRecord(householdId, new[] { personId }, template.HomeRoomId,
                budget: 0.5f, satisfaction: 1f, cashBalance: 100);
            var schedule = DailySchedule.FromBlocks(new[]
            {
                new ScheduleBlock(DailySchedule.LabelSleep, new Tick(0), new Tick(10)),
                new ScheduleBlock(DailySchedule.LabelWork, new Tick(10), new Tick(600)),
                new ScheduleBlock(DailySchedule.LabelEat, new Tick(600), new Tick(620)),
                new ScheduleBlock(DailySchedule.LabelLeisure, new Tick(620), new Tick(DailySchedule.TicksPerDay))
            });
            var person = new PersonRecord(personId, householdId, template.HomeRoomId, default,
                schedule, template.Needs, template.Traits, worksOutside: true);
            var population = new PopulationState(new[] { person }, new[] { household });
            var elevatorBank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(7203), 0, 10) });
            var simulation = new TowerSimulation(new SimulationClock(new Tick(0)), topology, population,
                elevatorBank, new TowerEconomyState(initialTreasury: 0), settlementPeriod: 1000);

            for (var i = 0; i < 60 && !(person.CurrentLocation.IsOutside && person.CurrentActivity == ActivityKind.Working); i++)
                simulation.AdvanceOneTick();
            Assert.That(person.CurrentLocation.IsOutside, Is.True, "work schedule should complete an outside commute");
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Working));

            person.UpdateNeed(NeedKind.Hunger, 0.2f);
            simulation.AdvanceOneTick();
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Eating));
            Assert.That(simulation.OutsideMarket.PurchaseRevenue, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));

            for (var i = 0; i < 40 && person.CurrentActivity != ActivityKind.Working; i++)
                simulation.AdvanceOneTick();
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Working), "meal should release back to the active outside shift");
            Assert.That(person.CurrentLocation.IsOutside, Is.True);
            Assert.That(simulation.OutsideMarket.PurchaseRevenue, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice),
                "the same meal episode must not bill twice");

            for (var i = 0; i < 600 && !person.CurrentLocation.Equals(WorldLocation.InRoom(person.HomeRoomId)); i++)
                simulation.AdvanceOneTick();
            Assert.That(person.CurrentLocation, Is.EqualTo(WorldLocation.InRoom(person.HomeRoomId)),
                "the outside worker should leave the city when the work block ends");
        }

        [Test]
        public void ExhaustedOutsideFoodCredit_RecordsShortfallAndThrottlesRetries()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var template = FiftyResidentFixture.Create().Persons[0];
            var householdId = new EntityId(7301);
            var personId = new EntityId(7302);
            var household = new HouseholdRecord(householdId, new[] { personId }, template.HomeRoomId,
                budget: 0f, satisfaction: 1f, cashBalance: 0);
            var market = new OutsideMarketState();
            for (var i = 0; i < 45; i++)
                Assert.That(market.TryPurchase(household, 8, 45L * 8,
                    OutsideServiceCategory.EssentialFood).Accepted, Is.True);
            var needs = new[]
            {
                new NeedState(NeedKind.Hunger, 0.2f),
                new NeedState(NeedKind.Energy, 1f),
                new NeedState(NeedKind.Hygiene, 1f),
                new NeedState(NeedKind.Social, 1f),
                new NeedState(NeedKind.Purpose, 1f)
            };
            var person = new PersonRecord(personId, householdId, template.HomeRoomId, default,
                template.Schedule, needs, template.Traits, worksOutside: true);
            person.UpdateLocation(WorldLocation.Outside);
            var population = new PopulationState(new[] { person }, new[] { household });
            var elevatorBank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(7303), 0, 10) });
            var simulation = new TowerSimulation(new SimulationClock(new Tick(0)), topology, population,
                elevatorBank, new TowerEconomyState(initialTreasury: 0), settlementPeriod: 1000, outsideMarket: market);

            simulation.AdvanceOneTick();
            Assert.That(household.DailyEssentialShortfall, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));
            Assert.That(person.OutsideFoodRetryAfterTick, Is.EqualTo(TowerSimulation.OutsideFoodRetryCooldownTicks + 1));
            for (var i = 0; i < TowerSimulation.OutsideFoodRetryCooldownTicks - 1; i++) simulation.AdvanceOneTick();
            Assert.That(household.DailyEssentialShortfall, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice),
                "rejected food must not be retried on every tick");

            simulation.AdvanceOneTick();
            Assert.That(household.DailyEssentialShortfall, Is.EqualTo(2 * TowerSimulation.OutsideDailyMealPrice),
                "a later bounded retry should still expose persistent food insecurity");
        }

        [Test]
        public void TransitExecutionSystem_AdvancesWalkLegAndElevatorLeg()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(enqueueMorningRush: true);

            // Advance through morning rush hour to let residents complete full commutes
            for (var tick = 0; tick < 100; tick++)
            {
                sim.AdvanceOneTick();
            }

            // Elevator bank should have delivered passengers
            Assert.That(sim.ElevatorBank.DeliveredCount, Is.GreaterThan(0));
        }

        [Test]
        public void AllResidentsInitiallyLiveInTheirHomeRooms()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // At tick 0 before commute starts, all residents must be InRoom in their home apartment
            for (var p = 0; p < sim.ResidentCount; p++)
            {
                var person = sim.Population.Persons[p];
                var spatial = sim.GetResidentPosition(person.Id);

                Assert.That(spatial.Phase, Is.EqualTo(ResidentMovementPhase.InRoom));
                Assert.That(spatial.RoomId, Is.EqualTo(person.HomeRoomId));
                Assert.That(spatial.Floor, Is.GreaterThan(0), "Home apartments in FiveFloor fixture are on floors 1-4.");
                Assert.That(spatial.X, Is.Not.Zero);
            }
        }

        [Test]
        public void ActiveCommuteTransitionsThroughWalkingQueuedAndRidingPhases()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            var observedWalking = false;
            var observedQueued = false;
            var observedRiding = false;

            for (var tick = 0; tick < 60; tick++)
            {
                sim.AdvanceOneTick();

                for (var p = 0; p < sim.ResidentCount; p++)
                {
                    var person = sim.Population.Persons[p];
                    var spatial = sim.GetResidentPosition(person.Id);

                    if (spatial.Phase == ResidentMovementPhase.Walking) observedWalking = true;
                    if (spatial.Phase == ResidentMovementPhase.Queued) observedQueued = true;
                    if (spatial.Phase == ResidentMovementPhase.Riding) observedRiding = true;
                }
            }

            Assert.That(observedWalking, Is.True, "Should observe residents walking along corridors.");
            Assert.That(observedQueued, Is.True, "Should observe residents queued at elevator.");
            Assert.That(observedRiding, Is.True, "Should observe residents riding elevator car.");
        }

        [Test]
        public void InitialState_ElevatorIsIdleAndNoPassengersQueued()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            Assert.That(sim.TotalQueuedElevatorPassengers, Is.EqualTo(0));
            Assert.That(sim.ElevatorBank.Cars[0].Phase, Is.EqualTo(ElevatorCarPhase.Idle));
            Assert.That(sim.ElevatorBank.Cars[0].Passengers.Count, Is.EqualTo(0));
            Assert.That(sim.ElevatorBank.Cars[0].HasRequests, Is.False);
        }

        [Test]
        public void BuildingRoom_DynamicallySynchronizesPlannerAndProducesElevatorRoute()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(new TowerEconomyState(50000));

            // Build a second diner on Floor 0 at unoccupied cells [-14..-11]
            var buildCmd = new OneRoof.Domain.Commands.BuildRoomCommand(
                0,
                -14,
                -11,
                new ContentId("commercial:diner"),
                capacity: 10);

            var result = sim.BuildRoom(buildCmd);
            Assert.That(result.Accepted, Is.True);

            // Locate newly allocated room
            Room newDiner = null;
            foreach (var r in sim.Topology.GetRoomsOnFloor(0))
            {
                if (r.Bounds.MinX == -14 && r.Bounds.MaxX == -11)
                {
                    newDiner = r;
                    break;
                }
            }
            Assert.That(newDiner, Is.Not.Null);

            // Plan route from a residential apartment on Floor 3 to the new Diner on Floor 0
            var floor3Rooms = sim.Topology.GetRoomsOnFloor(3);
            var aptFloor3 = floor3Rooms[1]; // apartment on floor 3

            var originNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(aptFloor3.Id);
            var destNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(newDiner.Id);

            Assert.That(originNode, Is.Not.Null);
            Assert.That(destNode, Is.Not.Null);

            var route = sim.Planner.FindRoute(originNode.Id, destNode.Id);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Legs.Count, Is.EqualTo(3));
            Assert.That(route.Legs[0].Mode, Is.EqualTo(TransitMode.Walk));
            Assert.That(route.Legs[1].Mode, Is.EqualTo(TransitMode.Elevator));
            Assert.That(route.Legs[2].Mode, Is.EqualTo(TransitMode.Walk));
        }

        [Test]
        public void UnroutableTrip_IsCancelledWithoutTeleportingResident()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var person = sim.Population.Persons[0];
            var initialLocation = person.CurrentRoomId;

            // Submit trip with null route to unreachable room ID 9999
            var unroutableTrip = new TripRecord(
                new EntityId(8888),
                person.Id,
                initialLocation,
                new EntityId(9999),
                TripPurpose.Work,
                sim.Clock.CurrentTick,
                plannedRoute: null);

            sim.Transit.SubmitTrip(unroutableTrip, sim.Topology, sim.Clock.CurrentTick, sim.Population);

            Assert.That(unroutableTrip.State, Is.EqualTo(TripState.Cancelled));
            Assert.That(person.CurrentRoomId, Is.EqualTo(initialLocation), "Resident must not teleport to destination on routing failure.");
            Assert.That(sim.Transit.ActiveTripCount, Is.EqualTo(0));
        }

        [Test]
        public void RestoringTripWithMissingRouteCancelsItInsteadOfEnqueueingInvalidExecution()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var room = topology.GetRoomsOnFloor(0)[0];
            var trip = new TripRecord(
                new EntityId(8890), new EntityId(1), room.Id, room.Id,
                TripPurpose.Work, new Tick(0), plannedRoute: null);
            var transit = new TransitExecutionSystem();

            transit.RestoreTripExecution(trip, 0, 1, false, false, 0, 0f);

            Assert.That(trip.State, Is.EqualTo(TripState.Cancelled));
            Assert.That(transit.ActiveTripCount, Is.Zero);
        }
    }
}
