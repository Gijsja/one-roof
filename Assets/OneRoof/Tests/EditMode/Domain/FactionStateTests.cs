using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Social;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Persistence;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class FactionStateTests
    {
        [Test]
        public void FiftyResidentFixtureFormsDistinctGroupsAndReliefReducesTenantPressure()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 1);
            sim.ExecuteCommand(new SetPolicyDecreeCommand(new PolicyDecreeState(1f, .2f, false, false)));
            foreach (var household in sim.Population.Households) household.AdjustCashBalance(-10000);
            sim.AdvanceOneTick();
            sim.AdvanceOneTick();
            var tenant = Find(sim, FactionIds.TenantUnion);
            var merchant = Find(sim, FactionIds.MerchantGuild);
            Assert.That(tenant.SupporterCount, Is.GreaterThan(0));
            Assert.That(merchant.SupporterCount, Is.GreaterThan(0));
            Assert.That(tenant.Pressure, Is.Not.EqualTo(merchant.Pressure));
            var before = tenant.Pressure;
            foreach (var household in sim.Population.Households) household.AdjustCashBalance(30000);
            for (var i = 0; i < 5; i++) sim.AdvanceOneTick();
            Assert.That(Find(sim, FactionIds.TenantUnion).Pressure, Is.LessThan(before));
        }

        [Test]
        public void SaveLoadKeepsAffiliationAndBoundedEdges()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 1);
            sim.AdvanceOneTick(); sim.AdvanceOneTick();
            var saved = sim.ExportSaveData();
            var restored = TowerSimulation.RestoreFromSaveData(saved);
            Assert.That(restored.Factions.Supports.Count, Is.EqualTo(sim.Factions.Supports.Count));
            Assert.That(restored.Factions.Edges.Count, Is.EqualTo(sim.Factions.Edges.Count));
            var degree = new Dictionary<int, int>();
            foreach (var edge in restored.Factions.Edges)
            {
                degree.TryGetValue(edge.First.Value, out var a); degree[edge.First.Value] = a + 1;
                degree.TryGetValue(edge.Second.Value, out var b); degree[edge.Second.Value] = b + 1;
            }
            foreach (var count in degree.Values) Assert.That(count, Is.LessThanOrEqualTo(FactionState.MaxEdgesPerResident));
        }

        [Test]
        public void ThreeHundredResidentsRespectEdgeCap()
        {
            var template = TowerSimulation.CreateStandardFiveFloor();
            var people = new List<PersonRecord>();
            var first = template.Population.Persons[0];
            for (var i = 0; i < 300; i++)
                people.Add(new PersonRecord(new EntityId(5000 + i), first.HouseholdId,
                    first.HomeRoomId, first.WorkplaceRoomId, first.Schedule, first.Needs, first.Traits));
            var population = new PopulationState(people, template.Population.Households);
            var factions = new FactionState();
            factions.Evaluate(population, template.Topology, template.Businesses, template.Economy.Policy, 1440);
            var degree = new Dictionary<int, int>();
            foreach (var edge in factions.Edges)
            {
                degree.TryGetValue(edge.First.Value, out var a); degree[edge.First.Value] = a + 1;
                degree.TryGetValue(edge.Second.Value, out var b); degree[edge.Second.Value] = b + 1;
            }
            foreach (var count in degree.Values) Assert.That(count, Is.LessThanOrEqualTo(FactionState.MaxEdgesPerResident));
        }

        [Test]
        public void MissingSocialSaveUsesVersionedEmptyDefault()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var data = sim.ExportSaveData();
            data.factions = null;
            var restored = TowerSimulation.RestoreFromSaveData(data);
            Assert.That(restored.Factions.Factions.Count, Is.EqualTo(4));
            Assert.That(restored.Factions.Edges.Count, Is.Zero);
        }

        [Test]
        public void FactionDifferenceAndRepeatedEncounterDoNotCreateRivalry()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var original = sim.Population.Persons[0];
            var other = new PersonRecord(new EntityId(9001), original.HouseholdId,
                original.HomeRoomId, original.WorkplaceRoomId, original.Schedule, original.Needs, original.Traits);
            var population = new PopulationState(new List<PersonRecord> { original, other }, sim.Population.Households);
            var data = new FactionSaveData
            {
                version = 1,
                supports = new[]
                {
                    new FactionSupportSaveData { residentId = original.Id.Value, factionId = FactionIds.TenantUnion, support = .9f, sustainedDays = 2 },
                    new FactionSupportSaveData { residentId = other.Id.Value, factionId = FactionIds.CorporateCoalition, support = .9f, sustainedDays = 2 }
                }
            };
            var state = FactionState.FromSaveData(data, population);
            state.Evaluate(population, sim.Topology, sim.Businesses, sim.Economy.Policy, 1440);
            state.Evaluate(population, sim.Topology, sim.Businesses, sim.Economy.Policy, 2880);
            Assert.That(state.Edges.Count, Is.GreaterThan(0));
            Assert.That(state.Edges[0].Affinity, Is.Zero);
            Assert.That(state.Edges[0].LastMeaningfulTick, Is.Zero);
            Assert.That(state.Edges[0].Cause, Does.StartWith("encounter:"));
        }

        [Test]
        public void RepeatedNamedGrievanceStrengthensTieAndSurvivesSaveLoad()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var first = sim.Population.Persons[0];
            var second = new PersonRecord(new EntityId(9002), first.HouseholdId,
                first.HomeRoomId, first.WorkplaceRoomId, first.Schedule, first.Needs, first.Traits);
            var people = new PopulationState(new List<PersonRecord> { first, second }, sim.Population.Households);
            first.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "Unpaid rent puts our lease at risk." });
            second.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "Unpaid rent puts our lease at risk." });
            var state = new FactionState();
            state.Evaluate(people, sim.Topology, sim.Businesses, sim.Economy.Policy, 1440);
            Assert.That(state.Edges[0].Affinity, Is.Zero);
            state.Evaluate(people, sim.Topology, sim.Businesses, sim.Economy.Policy, 2880);
            Assert.That(state.Edges[0].Affinity, Is.GreaterThan(0f));
            Assert.That(state.Edges[0].Cause, Does.Contain("Unpaid rent"));
            var restored = FactionState.FromSaveData(state.ToSaveData(), people);
            Assert.That(restored.Edges[0].LastMeaningfulTick, Is.EqualTo(2880));
            Assert.That(restored.Edges[0].Affinity, Is.EqualTo(state.Edges[0].Affinity));
            Assert.That(restored.Edges[0].PreviousAffinity, Is.EqualTo(state.Edges[0].PreviousAffinity));
            first.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, null);
            second.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, null);
            restored.Evaluate(people, sim.Topology, sim.Businesses, sim.Economy.Policy, 4320);
            Assert.That(restored.Edges[0].Affinity, Is.LessThan(state.Edges[0].Affinity));
        }

        [Test]
        public void DenseContactSampleIncludesResidentsBeyondLowestIds()
        {
            var template = TowerSimulation.CreateStandardFiveFloor();
            var first = template.Population.Persons[0];
            var people = new List<PersonRecord>();
            for (var i = 0; i < 300; i++)
                people.Add(new PersonRecord(new EntityId(5000 + i), first.HouseholdId,
                    first.HomeRoomId, first.WorkplaceRoomId, first.Schedule, first.Needs, first.Traits));
            var population = new PopulationState(people, template.Population.Households);
            var factions = new FactionState();
            factions.Evaluate(population, template.Topology, template.Businesses, template.Economy.Policy, 1440);
            var highest = 0;
            foreach (var edge in factions.Edges)
                highest = System.Math.Max(highest, edge.Second.Value);
            Assert.That(highest, Is.GreaterThan(5100));
            var restored = FactionState.FromSaveData(factions.ToSaveData(), population);
            Assert.That(restored.Edges.Count, Is.EqualTo(factions.Edges.Count));
            for (var i = 0; i < factions.Edges.Count; i++)
            {
                Assert.That(restored.Edges[i].First, Is.EqualTo(factions.Edges[i].First));
                Assert.That(restored.Edges[i].Second, Is.EqualTo(factions.Edges[i].Second));
            }
        }

        [Test]
        public void ChangingSharedIssueRestartsSupportStreakAcrossSaveLoad()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var first = sim.Population.Persons[0];
            var second = new PersonRecord(new EntityId(9003), first.HouseholdId,
                first.HomeRoomId, first.WorkplaceRoomId, first.Schedule, first.Needs, first.Traits);
            var people = new PopulationState(new List<PersonRecord> { first, second }, sim.Population.Households);
            first.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "rent" });
            second.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "rent" });
            var state = new FactionState();
            state.Evaluate(people, sim.Topology, sim.Businesses, sim.Economy.Policy, 1440);
            state = FactionState.FromSaveData(state.ToSaveData(), people);
            first.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "commute" });
            second.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "commute" });
            state.Evaluate(people, sim.Topology, sim.Businesses, sim.Economy.Policy, 2880);
            Assert.That(state.Edges[0].Affinity, Is.Zero);
            Assert.That(state.Edges[0].SharedSupportDays, Is.EqualTo(1));
            state.Evaluate(people, sim.Topology, sim.Businesses, sim.Economy.Policy, 4320);
            Assert.That(state.Edges[0].Affinity, Is.GreaterThan(0f));
            Assert.That(state.Edges[0].Cause, Does.Contain("commute"));
        }

        [Test]
        public void SharedIssueAfterMissedEvaluationStartsNewStreak()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var first = sim.Population.Persons[0];
            var second = new PersonRecord(new EntityId(9004), first.HouseholdId,
                first.HomeRoomId, first.WorkplaceRoomId, first.Schedule, first.Needs, first.Traits);
            var people = new PopulationState(new List<PersonRecord> { first, second }, sim.Population.Households);
            first.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "rent" });
            second.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "rent" });
            var saved = new FactionSaveData
            {
                version = 2, lastEvaluationTick = 2880,
                edges = new[] { new RelationshipEdgeSaveData { first = first.Id.Value, second = second.Id.Value,
                    lastContactTick = 1440, sharedIssue = "rent", sharedSupportDays = 1 } }
            };
            var state = FactionState.FromSaveData(saved, people);
            state.Evaluate(people, sim.Topology, sim.Businesses, sim.Economy.Policy, 4320);
            Assert.That(state.Edges[0].SharedSupportDays, Is.EqualTo(1));
            Assert.That(state.Edges[0].Affinity, Is.Zero);
        }

        [Test]
        public void MalformedSocialSaveRestoresFiniteUniqueBoundedState()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var first = sim.Population.Persons[0];
            var people = new List<PersonRecord> { first };
            for (var i = 0; i < 8; i++)
                people.Add(new PersonRecord(new EntityId(9100 + i), first.HouseholdId,
                    first.HomeRoomId, first.WorkplaceRoomId, first.Schedule, first.Needs, first.Traits));
            var population = new PopulationState(people, sim.Population.Households);
            var edges = new List<RelationshipEdgeSaveData>();
            for (var i = 1; i < people.Count; i++)
                edges.Add(new RelationshipEdgeSaveData { first = first.Id.Value, second = people[i].Id.Value,
                    affinity = float.NaN, previousAffinity = float.PositiveInfinity, sharedIssue = "rent", sharedSupportDays = int.MaxValue });
            edges.Add(edges[0]);
            var saved = new FactionSaveData
            {
                version = 2, edges = edges.ToArray(),
                supports = new[]
                {
                    new FactionSupportSaveData { residentId = first.Id.Value, factionId = FactionIds.TenantUnion, support = float.NaN },
                    new FactionSupportSaveData { residentId = first.Id.Value, factionId = FactionIds.TenantUnion, support = 1f }
                },
                factions = new[] { new FactionRecordSaveData { id = FactionIds.TenantUnion, pressure = float.NaN, previousPressure = float.PositiveInfinity } }
            };
            var state = FactionState.FromSaveData(saved, population);
            Assert.That(state.Edges.Count, Is.EqualTo(FactionState.MaxEdgesPerResident));
            foreach (var edge in state.Edges)
            {
                Assert.That(edge.Affinity, Is.Zero);
                Assert.That(edge.PreviousAffinity, Is.Zero);
                Assert.That(edge.SharedSupportDays, Is.EqualTo(10000));
            }
            Assert.That(state.Supports.Count, Is.EqualTo(1));
            Assert.That(state.Supports[0].Support, Is.Zero);
            Assert.That(state.Factions[0].Pressure, Is.Zero);
            Assert.That(state.Factions[0].PreviousPressure, Is.Zero);
        }

        private static FactionRecord Find(TowerSimulation sim, string id)
        {
            foreach (var record in sim.Factions.Factions) if (record.Id == id) return record;
            throw new System.Exception(id);
        }
    }
}
