using System;
using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Topology;
using OneRoof.Infrastructure.Persistence;
using UnityEngine;

namespace OneRoof.Infrastructure.Tests.EditMode
{
    public sealed class CrisisCheckpointJsonTests
    {
        private static string Canonical(TowerSaveData data)
        {
            foreach (var person in data.persons)
                if (person.currentLocationKind == (int)WorldLocationKind.Outside) person.currentRoomId = 0;
            if (data.factions?.supports != null)
                Array.Sort(data.factions.supports, (a, b) =>
                {
                    var resident = a.residentId.CompareTo(b.residentId);
                    return resident != 0 ? resident : string.CompareOrdinal(a.factionId, b.factionId);
                });
            return JsonUtility.ToJson(data);
        }

        [Test]
        public void ProductionJsonPreservesDepletedNeedsReroutesQueuesAndFutureState()
        {
            var sim = GoldStandardCityFixture.Create();
            for (var i = 0; i < 7200; i++) sim.AdvanceOneTick();
            var serializer = new JsonSaveSerializer();
            var metadata = new SaveEnvelopeMetadata(new SchemaVersion(1), sim.Clock.CurrentTick,
                ((DeterministicRandomStream)sim.RandomStream).State, "2026-10-02T00:00:00Z", "c0-checkpoint");
            var json = serializer.Serialize(new SaveEnvelope<TowerSaveData>(metadata, sim.ExportSaveData()));
            var loaded = serializer.Deserialize<TowerSaveData>(json, new SchemaVersion(1));
            Assert.That(loaded.IsSuccess, Is.True);
            var restored = TowerSimulation.RestoreFromSaveData(loaded.Value.StatePayload);
            Assert.That(Canonical(restored.ExportSaveData()), Is.EqualTo(Canonical(sim.ExportSaveData())));
            for (var i = 0; i < 30; i++)
            {
                sim.AdvanceOneTick(); restored.AdvanceOneTick();
                Assert.That(Canonical(restored.ExportSaveData()), Is.EqualTo(Canonical(sim.ExportSaveData())), "Future tick " + i);
            }
        }
    }
}
