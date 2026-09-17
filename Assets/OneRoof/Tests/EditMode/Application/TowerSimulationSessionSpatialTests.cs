using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;
using System.Collections.Generic;

namespace OneRoof.Application.Tests.EditMode
{
    [TestFixture]
    public sealed class TowerSimulationSessionSpatialTests
    {
        [Test]
        public void TransitProjection_InitialState_ProjectsAllResidentsAsInRoomWithSlots()
        {
            var session = new TowerSimulationSession();
            var projection = session.TransitProjection();

            Assert.That(projection.Residents.Count, Is.EqualTo(FiftyResidentFixture.TotalResidents));

            var slotsByRoom = new Dictionary<int, HashSet<int>>();

            for (var i = 0; i < projection.Residents.Count; i++)
            {
                var r = projection.Residents[i];
                Assert.That(r.Status, Is.EqualTo(TransitResidentStatus.InRoom),
                    $"Resident {r.ResidentId} should start InRoom.");
                Assert.That(r.RoomId.HasValue, Is.True,
                    $"Resident {r.ResidentId} should have a valid RoomId.");
                Assert.That(r.Floor, Is.GreaterThan(0),
                    $"Resident {r.ResidentId} should start on apartment floors 1-4.");

                var roomId = r.RoomId.Value;
                if (!slotsByRoom.TryGetValue(roomId, out var set))
                {
                    set = new HashSet<int>();
                    slotsByRoom[roomId] = set;
                }

                Assert.That(set.Contains(r.SlotInRoom), Is.False,
                    $"Room {roomId} already assigned slot {r.SlotInRoom} to another resident.");
                set.Add(r.SlotInRoom);
            }

            Assert.That(slotsByRoom.Count, Is.EqualTo(FiftyResidentFixture.TotalHouseholds),
                "Should have exactly 16 rooms occupied by households.");
        }

        [Test]
        public void TransitProjection_DuringCommute_ExposesWalkingAndQueuedStatus()
        {
            var session = new TowerSimulationSession();

            var observedCommuting = false;

            for (var tick = 0; tick < 50; tick++)
            {
                session.AdvanceOneTick();
                var projection = session.TransitProjection();

                for (var i = 0; i < projection.Residents.Count; i++)
                {
                    var status = projection.Residents[i].Status;
                    if (status == TransitResidentStatus.Walking ||
                        status == TransitResidentStatus.Queued ||
                        status == TransitResidentStatus.Riding)
                    {
                        observedCommuting = true;
                        break;
                    }
                }

                if (observedCommuting) break;
            }

            Assert.That(observedCommuting, Is.True, "Residents should be observed commuting via Walking, Queued, or Riding.");
        }
    }
}
