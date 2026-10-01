using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class HouseholdHousingLifecycleTests
    {
        private readonly HouseholdHousingLifecycleSystem _system = new HouseholdHousingLifecycleSystem();

        [TestCase(0, HousingConditionStage.Maintained, HouseholdLeasePhase.Current)]
        [TestCase(6, HousingConditionStage.Maintained, HouseholdLeasePhase.Current)]
        [TestCase(7, HousingConditionStage.Worn, HouseholdLeasePhase.Current)]
        [TestCase(13, HousingConditionStage.Worn, HouseholdLeasePhase.Current)]
        [TestCase(14, HousingConditionStage.Degraded, HouseholdLeasePhase.Current)]
        [TestCase(30, HousingConditionStage.Degraded, HouseholdLeasePhase.Current)]
        [TestCase(31, HousingConditionStage.Degraded, HouseholdLeasePhase.AtRisk)]
        public void Evaluate_UsesStableRoomAndLeaseThresholds(int arrearsDays, HousingConditionStage expectedCondition, HouseholdLeasePhase expectedPhase)
        {
            var household = HouseholdWithArrears(arrearsDays);
            var projection = _system.Evaluate(household, new HouseholdLeaseLifecycleState(), simulationDay: 0);

            Assert.That(projection.HouseholdId, Is.EqualTo(household.Id));
            Assert.That(projection.HomeRoomId, Is.EqualTo(household.HomeRoomId));
            Assert.That(projection.RentArrearsDays, Is.EqualTo(arrearsDays));
            Assert.That(projection.RoomCondition, Is.EqualTo(expectedCondition));
            Assert.That(projection.LeasePhase, Is.EqualTo(expectedPhase));
            Assert.That(projection.IsMoveOutEligible, Is.False);
        }

        [Test]
        public void AdvanceDaily_StartsNoticeAt45DaysAndSignalsDepartureAfterSevenDays()
        {
            var household = HouseholdWithArrears(45);
            var population = new PopulationState(null, new[] { household });
            var lifecycle = new HouseholdLeaseLifecycleState();

            var notice = _system.AdvanceDaily(lifecycle, population, simulationDay: 100)[0];
            var daySix = _system.AdvanceDaily(lifecycle, population, simulationDay: 106)[0];
            var daySeven = _system.AdvanceDaily(lifecycle, population, simulationDay: 107)[0];

            Assert.That(notice.LeasePhase, Is.EqualTo(HouseholdLeasePhase.Notice));
            Assert.That(notice.HasNotice, Is.True);
            Assert.That(notice.NoticeStartedDay, Is.EqualTo(100));
            Assert.That(daySix.IsMoveOutEligible, Is.False);
            Assert.That(daySeven.IsMoveOutEligible, Is.True);
        }

        [Test]
        public void ClearedRentCancelsNoticeAndRestoresDerivedRoomCondition()
        {
            var household = HouseholdWithArrears(45);
            var lifecycle = new HouseholdLeaseLifecycleState();
            var population = new PopulationState(null, new[] { household });
            _system.AdvanceDaily(lifecycle, population, simulationDay: 100);
            household.RecordRentPayment(long.MaxValue);
            household.UpdateRentArrearsDays();

            var recovered = _system.AdvanceDaily(lifecycle, population, simulationDay: 101)[0];

            Assert.That(recovered.RoomCondition, Is.EqualTo(HousingConditionStage.Maintained));
            Assert.That(recovered.LeasePhase, Is.EqualTo(HouseholdLeasePhase.Current));
            Assert.That(recovered.HasNotice, Is.False);
            Assert.That(lifecycle.PendingNoticeCount, Is.Zero);
        }

        [Test]
        public void PersistentNegativeBudgetWearsRoomAndStartsRecoverableNoticeWithoutRentArrears()
        {
            var household = new HouseholdRecord(new EntityId(12), new[] { new EntityId(19) }, new EntityId(45),
                budget: 0f, satisfaction: .7f, cashBalance: 0);
            var population = new PopulationState(null, new[] { household });
            var lifecycle = new HouseholdLeaseLifecycleState();
            for (var day = 1; day <= 30; day++)
            {
                household.BeginDailySettlement();
                household.RecordDailyIncome(1);
                Assert.That(household.TryPurchaseOutsideService(2, 100, OutsideServiceCategory.EssentialFood,
                    out _, out _), Is.True);
                household.CompleteDailyBudgetSettlement();
            }

            var distressed = _system.AdvanceDaily(lifecycle, population, simulationDay: 30)[0];
            Assert.That(household.RentArrearsBalance, Is.Zero);
            Assert.That(household.NetFinancialPosition, Is.LessThan(0));
            Assert.That(distressed.RoomCondition, Is.EqualTo(HousingConditionStage.Degraded));
            Assert.That(distressed.LeasePhase, Is.EqualTo(HouseholdLeasePhase.Notice));
            Assert.That(distressed.HasNotice, Is.True);

            for (var day = 31; day <= 32; day++)
            {
                household.BeginDailySettlement();
                household.RecordDailyIncome(20);
                household.CompleteDailyBudgetSettlement();
            }
            var recovered = _system.AdvanceDaily(lifecycle, population, simulationDay: 32)[0];
            Assert.That(recovered.HasNotice, Is.False);
            Assert.That(recovered.LeasePhase, Is.EqualTo(HouseholdLeasePhase.Current));
            Assert.That(recovered.RoomCondition, Is.EqualTo(HousingConditionStage.Maintained));
        }

        [Test]
        public void NoticeState_RoundTripsByStableHouseholdId()
        {
            var id = new EntityId(12);
            var state = new HouseholdLeaseLifecycleState();
            Assert.That(state.MarkNotice(id, 4321), Is.True);
            Assert.That(state.MarkNotice(id, 5000), Is.False);

            var restored = HouseholdLeaseLifecycleState.FromSaveData(state.ToSaveData());

            Assert.That(restored.TryGetNoticeStartedDay(id, out var startedDay), Is.True);
            Assert.That(startedDay, Is.EqualTo(4321));
            Assert.That(restored.IsMoveOutEligible(id, 4328), Is.True);
        }

        [Test]
        public void AdvanceDaily_SortsProjectionByStableHouseholdId()
        {
            var later = new HouseholdRecord(new EntityId(12), new[] { new EntityId(19) }, new EntityId(45), .5f, .7f);
            var earlier = new HouseholdRecord(new EntityId(2), new[] { new EntityId(3) }, new EntityId(4), .5f, .7f);
            var population = new PopulationState(null, new[] { later, earlier });

            var projections = _system.AdvanceDaily(new HouseholdLeaseLifecycleState(), population, simulationDay: 0);

            Assert.That(projections[0].HouseholdId, Is.EqualTo(earlier.Id));
            Assert.That(projections[1].HouseholdId, Is.EqualTo(later.Id));
        }

        [Test]
        public void DepartureHistory_RoundTripsAndKeepsDeterministicBoundedOrder()
        {
            var state = new HouseholdLeaseLifecycleState();
            Assert.That(state.RecordDeparture(new EntityId(5), 20, -7, 12), Is.True);
            Assert.That(state.RecordDeparture(new EntityId(3), 20, 14, 0), Is.True);
            Assert.That(state.RecordDeparture(new EntityId(5), 21, 0, 0), Is.False);
            for (var id = 10; id < 10 + HouseholdLeaseLifecycleState.MaxDepartureHistory; id++)
                state.RecordDeparture(new EntityId(id), id, id, id * 2L, id * 3L);

            var restored = HouseholdLeaseLifecycleState.FromSaveData(state.ToSaveData());

            Assert.That(restored.DepartureHistory.Count, Is.EqualTo(HouseholdLeaseLifecycleState.MaxDepartureHistory));
            Assert.That(restored.DepartureHistory[0].HouseholdId, Is.EqualTo(new EntityId(12)));
            Assert.That(restored.DepartureHistory[0].SimulationDay, Is.EqualTo(12));
            Assert.That(restored.DepartureHistory[0].FinalCashBalance, Is.EqualTo(12));
            Assert.That(restored.DepartureHistory[0].FinalRentArrears, Is.EqualTo(24));
            Assert.That(restored.DepartureHistory[0].FinalOutsideCreditBalance, Is.EqualTo(36));
            Assert.That(restored.DepartureHistory[63].HouseholdId, Is.EqualTo(new EntityId(73)));
        }

        [Test]
        public void Household_InitializesWithBedAndAllowsAddingProps()
        {
            var household = new HouseholdRecord(new EntityId(10), new[] { new EntityId(1) }, new EntityId(20), .8f, .8f);
            Assert.That(household.AcquiredProps.Count, Is.EqualTo(1));
            Assert.That(household.AcquiredProps[0], Is.EqualTo("prop.furniture.bed.v1"));

            var added = household.TryAddAcquiredProp("prop.decor.planter.v1");
            Assert.That(added, Is.True);
            Assert.That(household.AcquiredProps.Count, Is.EqualTo(2));
            Assert.That(household.AcquiredProps.Contains("prop.decor.planter.v1"), Is.True);

            // Duplicate cannot be added, including case-insensitive check
            var duplicate = household.TryAddAcquiredProp("PROP.DECOR.PLANTER.V1");
            Assert.That(duplicate, Is.False);

            // Enforces immutability: cannot downcast to mutable List<string> and IList modification throws
            Assert.That(household.AcquiredProps is List<string>, Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)household.AcquiredProps).Add("prop.civic.coatrack.v1"));
        }

        [Test]
        public void EvaluateOutsideFurniturePurchases_ProgressivelyBuysPropsFromOutsideMarket_WhenSolvent()
        {
            var topology = new OneRoof.Domain.Topology.BuildingTopologyState();
            var slabRes = topology.Execute(new OneRoof.Domain.Commands.BuildFloorSlabCommand(0, -10, 10), new OneRoof.Domain.Time.Tick(0));
            Assert.That(slabRes.Accepted, Is.True);
            var roomRes = topology.Execute(new OneRoof.Domain.Commands.BuildRoomCommand(0, 2, 7, new ContentId("residential:apartment"), 2), new OneRoof.Domain.Time.Tick(0));
            Assert.That(roomRes.Accepted, Is.True);
            var room = topology.Rooms.Values.First();
            var homeRoomId = room.Id;

            var household = new HouseholdRecord(
                new EntityId(50),
                new[] { new EntityId(51) },
                homeRoomId,
                budget: 0.9f,
                satisfaction: 0.9f,
                cashBalance: 500);

            var population = new PopulationState(null, new[] { household });
            var outsideMarket = new OneRoof.Domain.Economy.OutsideMarketState();

            // Initial state: only bed
            Assert.That(household.AcquiredProps.Count, Is.EqualTo(1));
            Assert.That(household.AcquiredProps[0], Is.EqualTo("prop.furniture.bed.v1"));

            // Day 1 purchase: Coatrack (cost 10)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.civic.coatrack.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(490));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(10));
            Assert.That(household.DailyOutsideQualitySpend, Is.EqualTo(10));

            // Day 2 purchase: Planter (cost 15)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.decor.planter.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(475));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(25));

            // Day 3 purchase: Sofa (cost 45, width >= 2.0m)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.furniture.sofa.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(430));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(70));

            // Day 4 purchase: Table (cost 25, if has sofa)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.furniture.table.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(405));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(95));

            // Day 5 purchase: Desk (cost 40, width >= 2.5m)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.workplace.desk.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(365));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(135));

            // Day 6 purchase: Bookcase (cost 30, width >= 2.5m)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.furniture.bookcase.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(335));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(165));

            // Day 7 purchase: Kitchenette (cost 50, width >= 3.0m)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.domestic.kitchenette.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(285));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(215));

            // Day 8 purchase: Game Table (cost 65, width >= 3.0m)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.furniture.gametable.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(220));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(280));

            // Day 9 purchase: Vanity Dresser (cost 60, width >= 3.0m)
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Contains("prop.furniture.vanitydresser.v1"), Is.True);
            Assert.That(household.CashBalance, Is.EqualTo(160));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(340));

            // Day 10: Fully furnished for its size, no further purchases
            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);
            Assert.That(household.AcquiredProps.Count, Is.EqualTo(10));
            Assert.That(household.CashBalance, Is.EqualTo(160));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(340));
        }

        [Test]
        public void EvaluateOutsideFurniturePurchases_PreservesPrudentReserveBuffer()
        {
            var topology = new OneRoof.Domain.Topology.BuildingTopologyState();
            topology.Execute(new OneRoof.Domain.Commands.BuildFloorSlabCommand(0, -10, 10), new OneRoof.Domain.Time.Tick(0));
            topology.Execute(new OneRoof.Domain.Commands.BuildRoomCommand(0, 2, 7, new ContentId("residential:apartment"), 2), new OneRoof.Domain.Time.Tick(0));
            var room = topology.Rooms.Values.First();

            // 1 member -> RentReserveTarget is 360 -> minBuffer is Max(60, 360/4) = 90
            // Cash 95: cost of coatrack is 10. 95 - 10 = 85 < 90 minBuffer -> should NOT purchase
            var household = new HouseholdRecord(
                new EntityId(60),
                new[] { new EntityId(61) },
                room.Id,
                budget: 0.9f,
                satisfaction: 0.9f,
                cashBalance: 95);

            var population = new PopulationState(null, new[] { household });
            var outsideMarket = new OneRoof.Domain.Economy.OutsideMarketState();

            _system.EvaluateOutsideFurniturePurchases(population, outsideMarket, topology);

            Assert.That(household.AcquiredProps.Count, Is.EqualTo(1));
            Assert.That(household.CashBalance, Is.EqualTo(95));
            Assert.That(outsideMarket.CashBalance, Is.EqualTo(0));
        }

        private static HouseholdRecord HouseholdWithArrears(int days)
        {
            var household = new HouseholdRecord(
                new EntityId(12), new[] { new EntityId(19) }, new EntityId(45),
                budget: .5f, satisfaction: .7f);
            if (days <= 0) return household;
            household.RecordRentDue(1);
            for (var day = 0; day < days; day++) household.UpdateRentArrearsDays();
            return household;
        }
    }
}
