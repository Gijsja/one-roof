using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class TowerTreasuryAndPolicyTests
    {
        [Test]
        public void PolicyDecreeState_ValidatesTiersAndNormalizesFloatDrift()
        {
            var policy = new PolicyDecreeState(0.7000001f, 0.10000005f, transitSubsidyEnabled: true, quietHoursEnabled: true);
            Assert.That(policy.RentCapMultiplier, Is.EqualTo(PolicyDecreeState.LowRentCapMultiplier));
            Assert.That(policy.CommercialTaxRate, Is.EqualTo(PolicyDecreeState.StandardCommercialTaxRate));
            Assert.That(policy.TransitSubsidyEnabled, Is.True);
            Assert.That(policy.QuietHoursEnabled, Is.True);
            Assert.That(policy.IsAggressive, Is.False);

            var aggressivePolicy = new PolicyDecreeState(1.3f, 0.2f, false, false);
            Assert.That(aggressivePolicy.IsAggressive, Is.True);

            Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyDecreeState(0.5f, 0.1f, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PolicyDecreeState(1.0f, 0.15f, false, false));
        }

        [Test]
        public void SetPolicyDecreeCommand_AppliesDecreeAndRecordsScrutinyForAggressiveSettings()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var scrutinyBefore = sim.Scrutiny.Value;

            var aggressivePolicy = new PolicyDecreeState(1.3f, 0.2f, false, false);
            var aggressiveCmd = new SetPolicyDecreeCommand(aggressivePolicy);
            var result = sim.ExecuteCommand(aggressiveCmd);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events, Has.Count.EqualTo(1));
            Assert.That(result.Events[0].Type.Value, Is.EqualTo("policy:decree_changed"));
            Assert.That(sim.Economy.Policy.RentCapMultiplier, Is.EqualTo(1.3f));
            Assert.That(sim.Economy.Policy.CommercialTaxRate, Is.EqualTo(0.2f));
            Assert.That(sim.Scrutiny.RecentPolicyPressure, Is.GreaterThan(0f));
        }

        [Test]
        public void ResidentWellbeing_RentAffordability_InvertsBurdenAndEmitsGrievanceUnderHighBurden()
        {
            var wellbeing = new ResidentWellbeingSystem();

            // Household 1: High income (affordable, low rent burden)
            var p1 = CreatePerson(101, PersonalityFacetKind.Resilient, householdId: 1);
            var highIncomeHousehold = new HouseholdRecord(
                id: new EntityId(1),
                memberIds: new[] { new EntityId(101) },
                homeRoomId: new EntityId(20),
                budget: 0.9f,
                satisfaction: 0.9f,
                cashBalance: 1000,
                arrearsDays: 0);
            highIncomeHousehold.RecordDailyIncome(200); // 200 daily income vs ~12 rent

            // Household 2: Zero income (100% rent burden)
            var p2 = CreatePerson(102, PersonalityFacetKind.Resilient, householdId: 2);
            var zeroIncomeHousehold = new HouseholdRecord(
                id: new EntityId(2),
                memberIds: new[] { new EntityId(102) },
                homeRoomId: new EntityId(21),
                budget: 0.5f,
                satisfaction: 0.5f,
                cashBalance: 0,
                arrearsDays: 0);

            var population = new PopulationState(
                new List<PersonRecord> { p1, p2 },
                new List<HouseholdRecord> { highIncomeHousehold, zeroIncomeHousehold });

            wellbeing.Advance(population, null, serviceEfficiencyMultiplier: 1f, rentMultiplier: 1f);

            // Affluent resident should have high satisfaction and NO rent grievance
            Assert.That(p1.Wellbeing.Satisfaction, Is.GreaterThan(0.7f));
            Assert.That(p1.Wellbeing.Grievances, Has.None.Contains("Household budget is under rent pressure."));

            // Zero income resident should have lower satisfaction and RECEIVE a rent grievance
            Assert.That(p2.Wellbeing.Satisfaction, Is.LessThan(p1.Wellbeing.Satisfaction));
            Assert.That(p2.Wellbeing.Grievances, Has.Some.Contains("Household budget is under rent pressure."));
        }

        [Test]
        public void HouseholdRecord_SpendOnService_ClampsToCashBalance()
        {
            var household = new HouseholdRecord(
                id: new EntityId(1),
                memberIds: new[] { new EntityId(101), new EntityId(102) },
                homeRoomId: new EntityId(20),
                budget: 0.8f,
                satisfaction: 0.8f,
                cashBalance: 10,
                arrearsDays: 0); // dailyCap = 2 * 5 = 10

            var spent1 = household.SpendOnService(8);
            Assert.That(spent1, Is.EqualTo(8));
            Assert.That(household.CashBalance, Is.EqualTo(2));

            // Requesting 5 more with only 2 cash left should clamp to 2
            var spent2 = household.SpendOnService(5);
            Assert.That(spent2, Is.EqualTo(2));
            Assert.That(household.CashBalance, Is.EqualTo(0));

            // With 0 cash, further spend is 0
            var spent3 = household.SpendOnService(5);
            Assert.That(spent3, Is.EqualTo(0));
            Assert.That(household.CashBalance, Is.EqualTo(0));
        }

        [Test]
        public void HouseholdRecord_RentBurdenUsesIncomeEvenWhenCashReserveIsHigh()
        {
            var household = new HouseholdRecord(new EntityId(1), new[] { new EntityId(101) },
                new EntityId(20), budget: 1f, satisfaction: 0.8f, cashBalance: 10000);
            household.RecordDailyIncome(6);

            Assert.That(household.CalculateRentBurden(12), Is.EqualTo(1f));
            household.BeginDailySettlement();
            Assert.That(household.CalculateRentBurden(12), Is.EqualTo(0f),
                "Without a recorded paycheck, a full reserve covers the immediate rent burden.");
        }

        [Test]
        public void WalkInSpend_IsSharedAcrossEligibleHouseholds()
        {
            var worker = CreatePerson(101, PersonalityFacetKind.Resilient, householdId: 1);
            var other = CreatePerson(102, PersonalityFacetKind.Resilient, householdId: 2);
            var first = new HouseholdRecord(new EntityId(1), new[] { worker.Id }, new EntityId(20), .5f, .8f, cashBalance: 100);
            var second = new HouseholdRecord(new EntityId(2), new[] { other.Id }, new EntityId(21), .5f, .8f, cashBalance: 100);
            var population = new PopulationState(new[] { worker, other }, new[] { first, second });
            var room = new Room(new EntityId(301), new ContentId("commercial:diner"), new CellBounds(0, 0, 0), Array.Empty<EntityId>(), 1);
            var business = new BusinessRecord(new EntityId(900), room.Id, room.ContentType, cashBalance: 100);
            business.ReconcileEmployees(population, room.Capacity);

            business.ProcessCycle(population, room, null, 1f, PolicyDecreeState.Default,
                new[] { first, second }, walkInSpendBudget: 6);

            Assert.That(business.LastCustomerRevenue, Is.EqualTo(6));
            Assert.That(first.DailyServiceSpend, Is.EqualTo(3));
            Assert.That(second.DailyServiceSpend, Is.EqualTo(3));
        }

        [Test]
        public void BusinessRecord_ProcessCycle_InsolventTenantAccruesOperatingCostsAndArrears()
        {
            var population = FiftyResidentFixture.Create();
            var business = new BusinessRecord(
                id: new EntityId(9000),
                roomId: new EntityId(9001),
                contentType: new ContentId("service:clinic"),
                cashBalance: 0,
                isInsolvent: true);

            business.ProcessCycle(population);
            Assert.That(business.IsInsolvent, Is.True);
            Assert.That(business.CashBalance, Is.EqualTo(-35));
            Assert.That(business.ArrearsDays, Is.EqualTo(1));

            business.ProcessCycle(population);
            Assert.That(business.IsInsolvent, Is.True);
            Assert.That(business.CashBalance, Is.EqualTo(-70));
            Assert.That(business.ArrearsDays, Is.EqualTo(2));

            business.ProcessCycle(population);
            Assert.That(business.IsInsolvent, Is.True);
            Assert.That(business.CashBalance, Is.EqualTo(-105));
            Assert.That(business.ArrearsDays, Is.EqualTo(3));
        }

        [Test]
        public void BusinessRecord_ProcessCycle_DoesNotWrapExtremeDebtIntoPositiveCash()
        {
            var business = new BusinessRecord(
                new EntityId(9000), new EntityId(9001), new ContentId("service:clinic"),
                cashBalance: long.MinValue, isInsolvent: true);

            business.ProcessCycle(FiftyResidentFixture.Create());

            Assert.That(business.CashBalance, Is.EqualTo(-1_000_000L));
        }

        [Test]
        public void EconomySaveRoundTrip_PreservesPolicyTreasuryHouseholdsAndBusinesses()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 50);
            sim.ExecuteCommand(new SetPolicyDecreeCommand(new PolicyDecreeState(0.7f, 0.2f, true, true)));
            for (var tick = 0; tick < 50; tick++) sim.AdvanceOneTick();

            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());

            Assert.That(restored.Economy.Policy, Is.EqualTo(sim.Economy.Policy));
            Assert.That(restored.Economy.CashBalance, Is.EqualTo(sim.Economy.CashBalance));
            Assert.That(restored.Economy.LastSettlementTick, Is.EqualTo(sim.Economy.LastSettlementTick));
            Assert.That(restored.Scrutiny.RecentPolicyPressure, Is.EqualTo(sim.Scrutiny.RecentPolicyPressure));
            Assert.That(restored.Population.Households.Count, Is.EqualTo(sim.Population.Households.Count));
            for (var i = 0; i < sim.Population.Households.Count; i++)
            {
                var before = sim.Population.Households[i];
                var after = restored.Population.Households[i];
                Assert.That(after.CashBalance, Is.EqualTo(before.CashBalance));
                Assert.That(after.ArrearsDays, Is.EqualTo(before.ArrearsDays));
                Assert.That(after.Budget, Is.EqualTo(before.Budget).Within(0.0001f));
            }
            Assert.That(restored.Businesses.Businesses.Count, Is.EqualTo(sim.Businesses.Businesses.Count));
            for (var i = 0; i < sim.Businesses.Businesses.Count; i++)
            {
                var before = sim.Businesses.Businesses[i];
                var after = restored.Businesses.Businesses[i];
                Assert.That(after.CashBalance, Is.EqualTo(before.CashBalance));
                Assert.That(after.ArrearsDays, Is.EqualTo(before.ArrearsDays));
                Assert.That(after.IsInsolvent, Is.EqualTo(before.IsInsolvent));
            }
        }

        [Test]
        public void ThirtyDailySettlements_ConserveCashAcrossAllLedgersAndExplicitSourcesAndSinks()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 50);
            for (var day = 1; day <= 30; day++)
            {
                var before = TotalCash(sim);
                var householdsBefore = sim.Population.Households.Count;
                var businessesBefore = sim.Businesses.Businesses.Count;
                var outsideContractsBefore = sim.OutsideMarket.ExternalContractRevenue;
                var reLeaseCapitalBefore = sim.Businesses.TotalReLeaseOpeningCapitalSource;
                var debtWriteOffBefore = sim.Businesses.TotalReLeaseDebtWriteOffSource;
                var retiredCashBefore = sim.Businesses.TotalReLeaseCashRetiredSink;
                for (var tick = 0; tick < 50; tick++) sim.AdvanceOneTick();

                long contractRevenue = 0;
                long operatingCosts = 0;
                foreach (var business in sim.Businesses.Businesses)
                {
                    contractRevenue += business.LastContractRevenue;
                    operatingCosts += business.LastOperatingCost;
                }
                var moveInSource = (sim.Population.Households.Count - householdsBefore) * HouseholdRecord.DefaultStartingCash;
                var tenantCapitalSource = (sim.Businesses.Businesses.Count - businessesBefore) * BusinessRecord.OpeningCapital;
                var outsideContracts = sim.OutsideMarket.ExternalContractRevenue - outsideContractsBefore;
                var reLeaseCapital = sim.Businesses.TotalReLeaseOpeningCapitalSource - reLeaseCapitalBefore;
                var debtWriteOff = sim.Businesses.TotalReLeaseDebtWriteOffSource - debtWriteOffBefore;
                var retiredCash = sim.Businesses.TotalReLeaseCashRetiredSink - retiredCashBefore;
                var expected = before + moveInSource + tenantCapitalSource + outsideContracts +
                    reLeaseCapital + debtWriteOff - retiredCash + contractRevenue - operatingCosts
                    - sim.Economy.LastDailyUpkeep - sim.Economy.LastDailySubsidy;
                Assert.That(TotalCash(sim), Is.EqualTo(expected), $"Cash conservation failed on day {day}.");
                Assert.That(sim.Economy.LastSettlementTick, Is.EqualTo(day * 50));
            }
        }

        [Test]
        public void ProlongedHouseholdArrears_ProduceLeaseGrievanceAndAdditionalStrain()
        {
            var person = CreatePerson(101, PersonalityFacetKind.Resilient, householdId: 1);
            var household = new HouseholdRecord(new EntityId(1), new[] { person.Id }, new EntityId(20),
                budget: 0f, satisfaction: 0.5f, cashBalance: -100, arrearsDays: 31);
            var population = new PopulationState(new[] { person }, new[] { household });
            var wellbeing = new ResidentWellbeingSystem();

            wellbeing.Advance(population, null);
            var strainWithoutDailyPenalty = person.Wellbeing.Strain;
            wellbeing.Advance(population, null);
            var secondTickStrain = person.Wellbeing.Strain;
            wellbeing.Advance(population, null, applyDailyArrearsStrain: true);

            Assert.That(person.Wellbeing.Grievances, Has.Some.Contains("Unpaid rent puts our lease at risk."));
            Assert.That(person.Wellbeing.RentBurden, Is.LessThan(0.48f));
            Assert.That(secondTickStrain - strainWithoutDailyPenalty, Is.LessThan(0.01f));
            Assert.That(person.Wellbeing.Strain - secondTickStrain, Is.GreaterThan(0.01f));
            for (var tick = 0; tick < 300; tick++) wellbeing.Advance(population, null);
            Assert.That(person.Wellbeing.Strain, Is.LessThan(0.05f),
                "Several hours of simulated ticks must not saturate a daily strain consequence.");
        }

        private static long TotalCash(TowerSimulation sim)
        {
            long total = sim.Economy.CashBalance + sim.OutsideMarket.CashBalance;
            foreach (var household in sim.Population.Households) total += household.CashBalance;
            foreach (var business in sim.Businesses.Businesses) total += business.CashBalance;
            return total;
        }

        [Test]
        public void TryRestoreFromSaveData_RejectsMissingTopologyWithoutThrowing()
        {
            var accepted = TowerSimulation.TryRestoreFromSaveData(new OneRoof.Domain.Persistence.TowerSaveData(),
                out var simulation, out var error);

            Assert.That(accepted, Is.False);
            Assert.That(simulation, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        private static PersonRecord CreatePerson(int id, PersonalityFacetKind facet, int householdId)
        {
            return new PersonRecord(
                new EntityId(id),
                new EntityId(householdId),
                new EntityId(id + 100),
                new EntityId(id + 200),
                DailySchedule.Standard(new PersonTrait(PersonTraitKind.EarlyBird), new DeterministicRandomStream((ulong)id)),
                new[] { new NeedState(NeedKind.Hunger, 1f) },
                new[] { new PersonTrait(PersonTraitKind.EarlyBird) },
                new[] { new PersonalityFacet(facet) });
        }
    }
}
