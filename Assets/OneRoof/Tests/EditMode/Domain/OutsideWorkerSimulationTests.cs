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
    public sealed class OutsideWorkerSimulationTests
    {
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
            var fixture = CreateOutsideWorkerFixture(cashBalance: 50, settlementPeriod: 1);
            var household = fixture.Household;
            var simulation = fixture.Simulation;

            simulation.AdvanceOneTick();

            Assert.That(household.DailyOutsideWages, Is.EqualTo(TowerSimulation.OutsideDailyWage));
            Assert.That(simulation.OutsideMarket.WageOutflow, Is.EqualTo(TowerSimulation.OutsideDailyWage));
            Assert.That(household.CashBalance, Is.EqualTo(62)); // +24 outside pay, -12 residential rent.
        }

        [Test]
        public void OutsideFoodNeedThroughSimulation_BillsOnceAndRestoresHunger()
        {
            var fixture = CreateOutsideWorkerFixture(cashBalance: 50, hunger: 0.2f,
                startOutside: true, initialTick: 100);
            var household = fixture.Household;
            var person = fixture.Worker;
            var simulation = fixture.Simulation;

            for (var i = 0; i < 12; i++) simulation.AdvanceOneTick();

            Assert.That(simulation.OutsideMarket.PurchaseRevenue, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(TowerSimulation.OutsideDailyMealPrice));
            Assert.That(household.CashBalance, Is.EqualTo(50 - TowerSimulation.OutsideDailyMealPrice));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.GreaterThan(0.2f));
        }

        [Test]
        public void DailyBudgetWindow_IncludesOutsideMealOnceAfterBoundaryAndAcrossSaveLoad()
        {
            var fixture = CreateOutsideWorkerFixture(cashBalance: 50, hunger: 0.2f,
                startOutside: true, initialTick: 1439, reserveOtherApartments: true);
            var household = fixture.Household;
            var householdId = household.Id;
            var simulation = fixture.Simulation;

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
            var fixture = CreateOutsideWorkerFixture(cashBalance: 0, budget: 0f,
                settlementPeriod: 1, reserveOtherApartments: true, outsideCreditBalance: 50,
                budgetHistory: Enumerable.Repeat(-2L, HouseholdRecord.DailyBudgetHistoryCapacity).ToArray());
            var household = fixture.Household;
            var simulation = fixture.Simulation;

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
            var schedule = DailySchedule.FromBlocks(new[]
            {
                new ScheduleBlock(DailySchedule.LabelSleep, new Tick(0), new Tick(10)),
                new ScheduleBlock(DailySchedule.LabelWork, new Tick(10), new Tick(600)),
                new ScheduleBlock(DailySchedule.LabelEat, new Tick(600), new Tick(620)),
                new ScheduleBlock(DailySchedule.LabelLeisure, new Tick(620), new Tick(DailySchedule.TicksPerDay))
            });
            var fixture = CreateOutsideWorkerFixture(cashBalance: 100, schedule: schedule, settlementPeriod: 1000);
            var household = fixture.Household;
            var person = fixture.Worker;
            var simulation = fixture.Simulation;

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
            var market = new OutsideMarketState();
            var fixture = CreateOutsideWorkerFixture(cashBalance: 0, budget: 0f, hunger: 0.2f,
                startOutside: true, settlementPeriod: 1000, outsideMarket: market);
            var household = fixture.Household;
            var person = fixture.Worker;
            var simulation = fixture.Simulation;
            for (var i = 0; i < 45; i++)
                Assert.That(market.TryPurchase(household, 8, 45L * 8,
                    OutsideServiceCategory.EssentialFood).Accepted, Is.True);

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

        private sealed class OutsideWorkerFixture
        {
            public TowerSimulation Simulation;
            public HouseholdRecord Household;
            public PersonRecord Worker;
        }

        private static OutsideWorkerFixture CreateOutsideWorkerFixture(
            long cashBalance, float budget = 0.5f, float hunger = 1f, bool startOutside = false,
            long initialTick = 0, long settlementPeriod = DailySchedule.TicksPerDay,
            DailySchedule schedule = null, bool reserveOtherApartments = false,
            long outsideCreditBalance = 0, long[] budgetHistory = null, OutsideMarketState outsideMarket = null)
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var home = topology.Rooms.Values.First(room => room.ContentType == FiveFloorTopologyFixture.ResidentialContentId);
            var householdId = new EntityId(7001);
            var personId = new EntityId(7002);
            var household = new HouseholdRecord(householdId, new[] { personId }, home.Id,
                budget: budget, satisfaction: 1f, cashBalance: cashBalance,
                outsideCreditBalance: outsideCreditBalance, recentDailyBudgetNetFlows: budgetHistory);
            var trait = new PersonTrait(PersonTraitKind.EarlyBird);
            schedule = schedule ?? DailySchedule.Standard(trait, new OneRoof.Domain.Randomness.DeterministicRandomStream(42), baseSleepEnd: 15);
            var needs = new[]
            {
                new NeedState(NeedKind.Hunger, hunger), new NeedState(NeedKind.Energy, 1f),
                new NeedState(NeedKind.Hygiene, 1f), new NeedState(NeedKind.Social, 1f),
                new NeedState(NeedKind.Purpose, 1f)
            };
            var person = new PersonRecord(personId, householdId, home.Id, default,
                schedule, needs, new[] { trait }, worksOutside: true);
            if (startOutside) person.UpdateLocation(WorldLocation.Outside);
            var households = new System.Collections.Generic.List<HouseholdRecord> { household };
            if (reserveOtherApartments)
            {
                // Prevent unrelated leasing during long budget/housing scenarios.
                var nextId = 7500;
                foreach (var room in topology.Rooms.Values)
                    if (room.ContentType == FiveFloorTopologyFixture.ResidentialContentId && room.Id != home.Id)
                        households.Add(new HouseholdRecord(new EntityId(nextId++), Array.Empty<EntityId>(),
                            room.Id, budget: 0.5f, satisfaction: 1f, cashBalance: 0));
            }
            var population = new PopulationState(new[] { person }, households);
            var bank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(7003), 0, 10) });
            return new OutsideWorkerFixture
            {
                Household = household,
                Worker = person,
                Simulation = new TowerSimulation(new SimulationClock(new Tick(initialTick)), topology, population,
                    bank, new TowerEconomyState(initialTreasury: 0), settlementPeriod: settlementPeriod,
                    outsideMarket: outsideMarket)
            };
        }
    }
}
