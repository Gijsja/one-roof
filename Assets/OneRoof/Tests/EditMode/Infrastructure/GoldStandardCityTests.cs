using System.Linq;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Topology;
using UnityEngine;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class GoldStandardCityTests
    {
        [Test]
        public void AuthoredCity_HousesThreeHundredPeopleWithoutOverlapsAndConnectsEveryHome()
        {
            var sim = GoldStandardCityFixture.Create();
            Assert.That(sim.Topology.FloorCount, Is.EqualTo(30));
            Assert.That(sim.ResidentCount, Is.EqualTo(300));
            Assert.That(sim.Population.Households.Count, Is.EqualTo(100));
            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(3));
            var ids = sim.Topology.Rooms.Values.Select(r => r.Id.Value)
                .Concat(sim.Population.Persons.Select(p => p.Id.Value))
                .Concat(sim.Population.Households.Select(h => h.Id.Value))
                .Concat(sim.ElevatorBank.Cars.Select(c => c.Id.Value)).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            for (var floor = 0; floor < 30; floor++)
            {
                var rooms = sim.Topology.GetRoomsOnFloor(floor);
                Assert.That(rooms.Count, Is.GreaterThanOrEqualTo(9));
                for (var a = 0; a < rooms.Count; a++)
                for (var b = a+1; b < rooms.Count; b++)
                    Assert.That(rooms[a].Bounds.MaxX < rooms[b].Bounds.MinX || rooms[b].Bounds.MaxX < rooms[a].Bounds.MinX,
                        Is.True, $"Overlapping rooms on floor {floor}");
            }
            var lobby = sim.Topology.Rooms.Values.First(r => r.ContentType.Value == "amenity:lobby");
            var destination = sim.Topology.TransitGraph.GetPortalNodeForRoom(lobby.Id);
            foreach (var home in sim.Population.Households)
            {
                var origin = sim.Topology.TransitGraph.GetPortalNodeForRoom(home.HomeRoomId);
                Assert.That(sim.Planner.FindRoute(origin.Id, destination.Id), Is.Not.Null);
            }
            var power = sim.ElectricalGrid.Evaluate(sim.Topology);
            var water = sim.WaterWasteNetwork.Evaluate(sim.Topology);
            foreach (var floor in power.Floors) Assert.That(floor.IsBrownout, Is.False);
            foreach (var floor in water.Floors)
            {
                Assert.That(floor.WaterFailure, Is.EqualTo(WaterFailureReason.None));
                Assert.That(floor.WasteFailure, Is.EqualTo(WasteFailureReason.None));
            }
        }

        [Test]
        public void CitySeed_IsDeterministic_AndSnapshotRestoresThePopulatedCity()
        {
            var first = GoldStandardCityFixture.Create();
            var second = GoldStandardCityFixture.Create();
            for (var tick = 0; tick < 90; tick++) { first.AdvanceOneTick(); second.AdvanceOneTick(); }
            Assert.That(JsonUtility.ToJson(first.ExportSaveData()), Is.EqualTo(JsonUtility.ToJson(second.ExportSaveData())));
            var restored = TowerSimulation.RestoreFromSaveData(first.ExportSaveData());
            Assert.That(JsonUtility.ToJson(restored.ExportSaveData()), Is.EqualTo(JsonUtility.ToJson(first.ExportSaveData())));
            // Exact subsequent trip-ID replay is an existing OR-1003 gap: the schedule
            // generator's next ID is not in the save contract. This test covers snapshot
            // restoration and safe continued operation, not the 30-day City Status gate.
            for (var tick = 0; tick < 60; tick++) restored.AdvanceOneTick();
            Assert.That(restored.Topology.FloorCount, Is.EqualTo(30));
            Assert.That(restored.ResidentCount, Is.EqualTo(300));
        }

        [Test]
        public void SessionReset_RestoresTheCityInsteadOfTheFiveFloorFixture()
        {
            var session = TowerSimulationSession.CreateGoldStandardCity();
            session.AdvanceOneTick();
            session.ResetToGoldStandardCity();
            Assert.That(session.FloorCount, Is.EqualTo(30));
            Assert.That(session.ResidentCount, Is.EqualTo(300));
            Assert.That(session.CurrentTick, Is.EqualTo(450));
        }
    }
}
