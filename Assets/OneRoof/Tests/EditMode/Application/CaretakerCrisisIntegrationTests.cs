using System;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Crisis;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class CaretakerCrisisIntegrationTests
    {
        [Test]
        public void CrisisNoticeSurvivesFinancialRecoveryAndSaveWhileOrdinaryNoticeCancels()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            var forced = simulation.Population.Households[0];
            var ordinary = simulation.Population.Households[1];
            simulation.HousingLifecycle.MarkCrisisEviction(forced.Id, 0);
            simulation.HousingLifecycle.MarkNotice(ordinary.Id, 0);
            simulation.HousingLifecycleSystem.AdvanceDaily(simulation.HousingLifecycle, simulation.Population, 1);
            Assert.That(simulation.HousingLifecycle.IsMoveOutEligible(forced.Id, 1), Is.True);
            Assert.That(simulation.HousingLifecycle.HasNotice(ordinary.Id), Is.False);
            var restored = TowerSimulation.RestoreFromSaveData(simulation.ExportSaveData());
            Assert.That(restored.HousingLifecycle.HasCrisisEviction(forced.Id), Is.True);
            Assert.That(restored.HousingLifecycle.CancelNotice(forced.Id), Is.False);
        }

        [Test]
        public void CompletedDeparturePreservesLiquidCashAcrossAggregateRemovalAndReload()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            var household = simulation.Population.Households[0];
            simulation.HousingLifecycle.MarkCrisisEviction(household.Id, 0);
            foreach (var memberId in household.MemberIds)
                if (simulation.Population.TryGetPerson(memberId, out var person)) person.UpdateLocation(WorldLocation.Outside);
            var initialCash = TotalLiquidCash(simulation);
            var initialOutside = simulation.OutsideMarket.CashBalance;
            var departedCash = household.CashBalance;
            simulation.AdvanceOneTick();
            Assert.That(simulation.Population.TryGetHousehold(household.Id, out _), Is.False);
            Assert.That(simulation.OutsideMarket.CashBalance, Is.EqualTo(initialOutside + departedCash));
            Assert.That(TotalLiquidCash(simulation), Is.EqualTo(initialCash));
            var restored = TowerSimulation.RestoreFromSaveData(simulation.ExportSaveData());
            Assert.That(TotalLiquidCash(restored), Is.EqualTo(initialCash));
            restored.AdvanceOneTick();
            Assert.That(TotalLiquidCash(restored), Is.EqualTo(initialCash));
        }

        private static long TotalLiquidCash(TowerSimulation simulation)
        {
            var total = simulation.Economy.CashBalance + simulation.OutsideMarket.CashBalance;
            foreach (var household in simulation.Population.Households) total += household.CashBalance;
            foreach (var business in simulation.Businesses.Businesses) total += business.CashBalance;
            return total;
        }

        [Test]
        public void LegacyNoticeRemainsSevenDayNotice()
        {
            var lifecycle = HouseholdLeaseLifecycleState.FromSaveData(new HouseholdLeaseLifecycleSaveData
            {
                notices = new[] { new HouseholdLeaseNoticeSaveData { householdId = 101, noticeStartedDay = 5 } }
            });
            Assert.That(lifecycle.HasCrisisEviction(new EntityId(101)), Is.False);
            Assert.That(lifecycle.IsMoveOutEligible(new EntityId(101), 5), Is.False);
            Assert.That(lifecycle.IsMoveOutEligible(new EntityId(101), 12), Is.True);
        }

        [Test]
        public void SessionCombatFreezesWorldAndExitRestoresTickAdvancement()
        {
            var simulation = CreateReadyGolden();
            var session = TowerSimulationSession.FromSaveData(simulation.ExportSaveData());
            Assert.That(session.ExecuteCaretaker(CaretakerCrisisAction.Start).Accepted, Is.True);
            var tick = session.CurrentTick;
            session.AdvanceOneTick();
            Assert.That(session.CurrentTick, Is.EqualTo(tick));
            Assert.That(session.ExecuteCaretaker(CaretakerCrisisAction.Step, new CaretakerCombatInput(1)).Accepted, Is.True);
            Assert.That(session.CurrentTick, Is.EqualTo(tick));
            Assert.That(session.ExecuteCaretaker(CaretakerCrisisAction.Exit).Accepted, Is.True);
            session.AdvanceOneTick();
            Assert.That(session.CurrentTick, Is.EqualTo(tick + 1));
        }

        [Test]
        public void ElevatorOnlyTowerRejectsCrisisWithActionableStairsReason()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            SeedReady(simulation);
            var result = simulation.ExecuteCommand(new CaretakerCrisisCommand(CaretakerCrisisAction.Start));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Does.Contain("stairs"));
            Assert.That(simulation.CaretakerOwnsInput, Is.False);
        }

        [Test]
        public void ClearedEncounterEvictsOnlyAffectedHouseholdAndRestoredSaveCannotReplayEffects()
        {
            var simulation = CreateReadyGolden();
            var target = simulation.Population.Persons[0];
            var unrelatedHousehold = simulation.Population.Households[simulation.Population.Households.Count - 1].Id;
            Assert.That(simulation.ExecuteCommand(new CaretakerCrisisCommand(CaretakerCrisisAction.Start)).Accepted, Is.True);
            var data = simulation.ExportSaveData();
            // A captured cleared final floor exercises integration consequences, not combat tuning.
            var encounter = data.caretakerCrisis;
            encounter.routeIndex = encounter.route.Length - 1;
            encounter.x = encounter.route[encounter.routeIndex].exitX;
            encounter.opponents = Array.Empty<CaretakerOpponentSaveData>();
            encounter.encounterResidentIds = new[] { target.Id.Value };
            encounter.floorParticipantIds = encounter.affectedResidentIds;
            encounter.nextParticipant = encounter.affectedResidentIds.Length;
            var session = TowerSimulationSession.FromSaveData(data);
            Assert.That(session.ExecuteCaretaker(CaretakerCrisisAction.Resume).Accepted, Is.True);
            Assert.That(session.ExecuteCaretaker(CaretakerCrisisAction.Advance).Accepted, Is.True);
            var finished = session.ExportSaveData();
            var restored = TowerSimulation.RestoreFromSaveData(finished);
            Assert.That(restored.CaretakerCrisis.ConsequencesApplied, Is.True);
            Assert.That(restored.HousingLifecycle.HasCrisisEviction(target.HouseholdId) ||
                !restored.Population.TryGetHousehold(target.HouseholdId, out _), Is.True);
            Assert.That(restored.HousingLifecycle.HasCrisisEviction(unrelatedHousehold), Is.False);
            var targetThoughtCount = 0;
            if (restored.Population.TryGetPerson(target.Id, out var present)) targetThoughtCount = present.Wellbeing.ActiveThoughts.Count;
            var result = restored.ExecuteCommand(new CaretakerCrisisCommand(CaretakerCrisisAction.Advance));
            Assert.That(result.Accepted, Is.False);
            if (present != null) Assert.That(present.Wellbeing.ActiveThoughts.Count, Is.EqualTo(targetThoughtCount));
        }

        public static TowerSimulation CreateReadyGolden()
        {
            var simulation = GoldStandardCityFixture.Create();
            SeedReady(simulation);
            return simulation;
        }

        private static void SeedReady(TowerSimulation simulation)
        {
            var residents = simulation.Population.Persons;
            simulation.Topology.TryGetRoom(residents[0].HomeRoomId, out var home);
            var signals = new[] { new CivilActionSignal("faction:tenant_union", .9f, 2, "rent pressure",
                new[] { residents[0].Id.Value, residents[1].Id.Value }, new[] { home.Floor }) };
            for (var day = 1; day <= 3; day++) simulation.CaretakerCrisis.Evaluate(day * 1440, signals, simulation.Population, simulation.Topology);
        }
    }
}
