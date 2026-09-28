using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    /// <summary>Fixed-population economic stress model at the planned City Status scale.</summary>
    public sealed class CityScaleEconomyLongRunTests
    {
        [TestCase(1f, 1f, 0f, false, 0, false, "neutral-no-outside-meals")]
        [TestCase(1f, 1f, 0f, false, 2, false, "neutral-half-outside-meals")]
        [TestCase(1f, 1f, 0f, false, 1, false, "neutral-daily-outside-meals")]
        [TestCase(0.7f, 1f, 0f, false, 0, false, "demand-shock-no-outside-meals")]
        [TestCase(1f, 1f, 0f, false, 0, true, "30-day-shock-then-recovery")]
        [TestCase(1f, 1.3f, 0.2f, true, 0, false, "high-rent-tax-quiet-no-outside-meals")]
        [Category("EconomySimulation")]
        public void ThreeHundredResidents_ThirtyFloors_365DailySettlements_ReconcileCashAndReportMargins(
            float demandFactor, float rentMultiplier, float taxRate, bool quietHours,
            int outsideMealStride, bool temporaryShock, string scenario)
        {
            var fixture = CreateFixture();
            var policy = new PolicyDecreeState(rentMultiplier, taxRate, false, quietHours);
            fixture.Treasury.SetPolicy(policy);
            Assert.That(fixture.Topology.FloorCount, Is.EqualTo(30));
            Assert.That(fixture.Population.ResidentCount, Is.EqualTo(300));
            Assert.That(fixture.Population.Households.Count, Is.EqualTo(100));

            var openingCash = TotalCash(fixture);
            var cumulativeContractRevenue = 0L;
            var cumulativeOperatingCost = 0L;
            var cumulativeUpkeep = 0L;
            var unmetMeals = 0L;
            var marginsByRoom = fixture.Businesses.Businesses.ToDictionary(b => b.RoomId.Value, b => 0L);
            for (var day = 1; day <= 365; day++)
            {
                var dailyDemand = temporaryShock && day >= 31 && day <= 60 ? 0.7f : demandFactor;
                fixture.Businesses.Advance(fixture.Topology, fixture.Population, ref fixture.NextEntityId,
                    dailyDemand, policy);
                foreach (var household in fixture.Population.Households) household.BeginDailySettlement();

                fixture.Businesses.ProcessPayroll(fixture.Population, fixture.Topology, dailyDemand);
                foreach (var person in fixture.Population.Persons)
                {
                    if (!person.WorkplaceLocation.IsOutside) continue;
                    fixture.Population.TryGetHousehold(person.HouseholdId, out var household);
                    fixture.Outside.RecordWagePayment(household, TowerSimulation.OutsideDailyWage);
                }
                fixture.Treasury.ProcessRentCycle(fixture.Topology, fixture.Population);
                foreach (var person in fixture.Population.Persons)
                {
                    if (!person.WorkplaceLocation.IsOutside || outsideMealStride == 0 ||
                        person.Id.Value % outsideMealStride != 0) continue;
                    fixture.Population.TryGetHousehold(person.HouseholdId, out var household);
                    if (!fixture.Outside.TryPurchase(household, TowerSimulation.OutsideDailyMealPrice, 0,
                            OutsideServiceCategory.EssentialFood).Accepted)
                        unmetMeals++;
                }
                fixture.Businesses.ProcessBusinessCycle(fixture.Topology, fixture.Population,
                    fixture.Treasury, dailyDemand, policy, payrollAlreadyProcessed: true);
                // Two utility cells on each floor and two elevator cars: a bounded city-size upkeep estimate.
                const long dailyUpkeep = 30 * 2 + 2 * 2;
                fixture.Treasury.ChargeDailyExpense(dailyUpkeep, false);
                fixture.Treasury.CompleteDailySettlement(day * DailySchedule.TicksPerDay);
                foreach (var household in fixture.Population.Households)
                {
                    household.UpdateArrearsDays();
                    household.CompleteDailyBudgetSettlement();
                }

                cumulativeUpkeep += dailyUpkeep;
                for (var i = 0; i < fixture.Businesses.Businesses.Count; i++)
                {
                    var business = fixture.Businesses.Businesses[i];
                    cumulativeContractRevenue += business.LastContractRevenue;
                    cumulativeOperatingCost += business.LastOperatingCost;
                    marginsByRoom[business.RoomId.Value] += business.LastCustomerRevenue +
                        business.LastContractRevenue - business.LastWages - business.LastRentPaid -
                        business.LastTaxPaid - business.LastOperatingCost;
                }
                Assert.That(TotalCash(fixture), Is.EqualTo(openingCash +
                    fixture.Outside.ExternalContractRevenue + cumulativeContractRevenue -
                    cumulativeOperatingCost - cumulativeUpkeep +
                    fixture.Businesses.TotalReLeaseOpeningCapitalSource +
                    fixture.Businesses.TotalReLeaseDebtWriteOffSource -
                    fixture.Businesses.TotalReLeaseCashRetiredSink),
                    $"{scenario}: cash ledger diverged on day {day}");

                if (day == 30 || day == 60 || day == 90 || day == 180 || day == 365)
                    TestContext.WriteLine(Describe(fixture, marginsByRoom,
                        unmetMeals, scenario, day));
            }
        }

        private static string Describe(Fixture f, Dictionary<int, long> marginsByRoom,
            long unmetMeals, string scenario, int day)
        {
            var insolvent = f.Businesses.Businesses.Count(b => b.IsInsolvent);
            var unpaidRent = f.Population.Households.Count(h => h.RentArrearsBalance > 0);
            var cashPoor = f.Population.Households.Count(h => h.CashBalance <= 0);
            var unpaidInsideHouseholds = f.Population.Households.Count(h =>
                h.MemberIds.Any(id => f.Population.TryGetPerson(id, out var person) &&
                                      !person.WorkplaceLocation.IsOutside) &&
                h.DailyIncome == h.DailyOutsideWages);
            var byType = new SortedDictionary<string, long>();
            for (var i = 0; i < f.Businesses.Businesses.Count; i++)
            {
                var type = f.Businesses.Businesses[i].ContentType.Value;
                byType.TryGetValue(type, out var sum);
                byType[type] = sum + marginsByRoom[f.Businesses.Businesses[i].RoomId.Value];
            }
            return $"ECON_CITY scenario={scenario} day={day} businessMargin={marginsByRoom.Values.Sum()} " +
                   $"insolvent={insolvent}/{marginsByRoom.Count} reLeases={f.Businesses.TotalReLeaseCount} " +
                   $"reLeaseCapital={f.Businesses.TotalReLeaseOpeningCapitalSource} " +
                   $"debtWriteOff={f.Businesses.TotalReLeaseDebtWriteOffSource} " +
                   $"householdsRentArrears={unpaidRent}/100 cashPoor={cashPoor}/100 " +
                   $"insideJobWithoutWage={unpaidInsideHouseholds}/100 " +
                   $"unmetOutsideMeals={unmetMeals} treasury={f.Treasury.CashBalance} " +
                   $"householdCash={f.Population.Households.Sum(h => h.CashBalance)} " +
                   $"businessCash={f.Businesses.Businesses.Sum(b => b.CashBalance)} " +
                   $"typeMargins={string.Join(";", byType.Select(p => p.Key + ":" + p.Value))}";
        }

        private static long TotalCash(Fixture f) => f.Treasury.CashBalance + f.Outside.CashBalance +
            f.Population.Households.Sum(h => h.CashBalance) + f.Businesses.Businesses.Sum(b => b.CashBalance);

        private static Fixture CreateFixture()
        {
            var slabs = new List<CellBounds>();
            var rooms = new List<Room>();
            var homes = new List<Room>();
            var jobs = new List<EntityId>();
            var nextRoomId = 10000;
            for (var floor = 0; floor < 30; floor++) slabs.Add(new CellBounds(floor, -20, 39));
            for (var floor = 1; floor <= 25; floor++)
            {
                foreach (var x in new[] { -20, -14, 2, 8 })
                {
                    var home = new Room(new EntityId(nextRoomId++),
                        new ContentId("residential:apartment"), new CellBounds(floor, x, x + 5),
                        Array.Empty<EntityId>(), 3);
                    homes.Add(home);
                    rooms.Add(home);
                }
            }
            var types = new[]
            {
                ("commercial:diner", 4, 10, 5), ("commercial:retail", 2, 6, 6),
                ("service:clinic", 1, 8, 12), ("commercial:office", 4, 8, 8),
                ("service:maintenance_workshop", 2, 8, 6),
                ("service:security_station", 2, 6, 4)
            };
            var slot = 0;
            foreach (var type in types)
            for (var copy = 0; copy < type.Item2; copy++)
            {
                var floor = slot / 3 == 0 ? 0 : 25 + slot / 3;
                var x = -20 + (slot % 3) * 19;
                var room = new Room(new EntityId(nextRoomId++), new ContentId(type.Item1),
                    new CellBounds(floor, x, x + type.Item3 - 1), Array.Empty<EntityId>(), type.Item4);
                rooms.Add(room);
                var tenant = new BusinessRecord(new EntityId(40000 + slot), room.Id, room.ContentType);
                var productivePositions = tenant.GetProductiveStaffCapacity(room, 1f);
                for (var worker = 0; worker < productivePositions; worker++) jobs.Add(room.Id);
                slot++;
            }
            var topology = new BuildingTopologyState();
            topology.RestoreFromData(slabs, rooms, Array.Empty<Portal>());
            var households = new List<HouseholdRecord>();
            var persons = new List<PersonRecord>();
            var template = FiftyResidentFixture.Create().Persons[0];
            for (var h = 0; h < 100; h++)
            {
                var householdId = new EntityId(20000 + h);
                var memberIds = new EntityId[3];
                for (var member = 0; member < 3; member++) memberIds[member] = new EntityId(30000 + h * 3 + member);
                households.Add(new HouseholdRecord(householdId, memberIds, homes[h].Id,
                    .5f, .8f, cashBalance: HouseholdRecord.DefaultStartingCash));
                for (var member = 0; member < 3; member++)
                {
                    var jobIndex = member * 100 + h;
                    var inside = jobIndex < jobs.Count;
                    persons.Add(new PersonRecord(memberIds[member], householdId, homes[h].Id,
                        inside ? jobs[jobIndex] : default, template.Schedule, template.Needs,
                        template.Traits, worksOutside: !inside));
                }
            }
            var population = new PopulationState(persons, households);
            var businesses = new BusinessState();
            var nextEntityId = 50000;
            businesses.Advance(topology, population, ref nextEntityId);
            return new Fixture(topology, population, businesses, new TowerEconomyState(50000),
                new OutsideMarketState(), nextEntityId);
        }

        private sealed class Fixture
        {
            public Fixture(BuildingTopologyState topology, PopulationState population,
                BusinessState businesses, TowerEconomyState treasury, OutsideMarketState outside,
                int nextEntityId)
            {
                Topology = topology;
                Population = population;
                Businesses = businesses;
                Treasury = treasury;
                Outside = outside;
                NextEntityId = nextEntityId;
            }
            public BuildingTopologyState Topology { get; }
            public PopulationState Population { get; }
            public BusinessState Businesses { get; }
            public TowerEconomyState Treasury { get; }
            public OutsideMarketState Outside { get; }
            public int NextEntityId;
        }
    }
}
