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
        public void RepeatedContactBetweenOpposingMembersCreatesAttributableRivalry()
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
            Assert.That(state.Edges.Count, Is.GreaterThan(0));
            Assert.That(state.Edges[0].Affinity, Is.LessThan(0));
            Assert.That(state.Edges[0].Cause, Does.Contain("conflicting faction priorities"));
        }

        private static FactionRecord Find(TowerSimulation sim, string id)
        {
            foreach (var record in sim.Factions.Factions) if (record.Id == id) return record;
            throw new System.Exception(id);
        }
    }
}
