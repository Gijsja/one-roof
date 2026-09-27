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
