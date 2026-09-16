using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Population;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Trips;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class NpcVisibilityPolicyTests
    {
        private BuildingTopology _topology;
        private PopulationState _population;
        private PopulationProjectionService _projectionService;
        private NpcVisibilityPolicy _policy;

        [SetUp]
        public void SetUp()
        {
            _topology = FiveFloorTopologyFixture.Create();
            _population = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(42));
            _projectionService = new PopulationProjectionService(_topology);
            _policy = new NpcVisibilityPolicy(40);
        }

        [Test]
        public void PopulationProjectionService_ProjectsAllFiftyResidents()
        {
            var projections = _projectionService.ProjectAll(_population);

            Assert.That(projections.Count, Is.EqualTo(FiftyResidentFixture.TotalResidents));
            Assert.That(projections.Count, Is.EqualTo(50));

            for (var i = 0; i < projections.Count; i++)
            {
                var person = _population.Persons[i];
                var projection = projections[i];
                Assert.That(projection.PersonId, Is.EqualTo(person.Id.Value));
                Assert.That(projection.HouseholdId, Is.EqualTo(person.HouseholdId.Value));
                Assert.That(projection.Floor, Is.InRange(0, 4));
            }
        }

        [Test]
        public void SelectVisibleNpcs_FiftyResidentsAllFloorsVisible_CapsAtForty()
        {
            var allProjections = _projectionService.ProjectAll(_population);
            Assert.That(allProjections.Count, Is.EqualTo(50), "Fifty persistent residents must exist.");

            var visibleProjections = _policy.SelectVisibleNpcs(allProjections, VisibleFloorRange.All(4));

            Assert.That(visibleProjections.Count, Is.EqualTo(40),
                "The visibility policy must cap active views at 40 while 50 persist.");
        }

        [Test]
        public void SelectVisibleNpcs_VisibleFloorRange_PrioritizesResidentsOnVisibleFloors()
        {
            var allProjections = _projectionService.ProjectAll(_population);

            // Restrict camera to Floor 0 only
            var singleFloorRange = VisibleFloorRange.SingleFloor(0);
            var selected = _policy.SelectVisibleNpcs(allProjections, singleFloorRange);

            // All residents whose floor is 0 must appear before any residents on other floors
            var seenNonZeroFloor = false;
            foreach (var npc in selected)
            {
                if (npc.Floor != 0)
                {
                    seenNonZeroFloor = true;
                }
                else if (seenNonZeroFloor)
                {
                    Assert.Fail("Resident on visible Floor 0 appeared after a resident on non-visible floor.");
                }
            }
        }

        [Test]
        public void SelectVisibleNpcs_CommutersInTransit_PrioritizedOverSleepingResidents()
        {
            var testProjections = new List<NpcProjection>
            {
                new NpcProjection(1, 1, floor: 2, roomId: 201, ActivityKind.Sleeping, isInTransit: false, null, null, 0, 0f),
                new NpcProjection(2, 1, floor: 2, roomId: 201, ActivityKind.Working, isInTransit: true, destinationFloor: 0, destinationRoomId: 101, waitTicks: 15, 0f)
            };

            var selected = _policy.SelectVisibleNpcs(testProjections, VisibleFloorRange.All(4));

            Assert.That(selected[0].PersonId, Is.EqualTo(2), "Commuter in transit must be prioritized ahead of sleeping resident.");
            Assert.That(selected[1].PersonId, Is.EqualTo(1));
        }

        [Test]
        public void SelectVisibleNpcs_IsDeterministicAcrossCalls()
        {
            var allProjections = _projectionService.ProjectAll(_population);

            var run1 = _policy.SelectVisibleNpcs(allProjections, VisibleFloorRange.All(4));
            var run2 = _policy.SelectVisibleNpcs(allProjections, VisibleFloorRange.All(4));

            Assert.That(run1.Count, Is.EqualTo(run2.Count));
            for (var i = 0; i < run1.Count; i++)
            {
                Assert.That(run1[i].PersonId, Is.EqualTo(run2[i].PersonId), $"Order mismatch at index {i}.");
            }
        }

        [Test]
        public void SelectVisibleNpcs_FewerThanFortyCandidates_ReturnsAllCandidates()
        {
            var smallList = new List<NpcProjection>();
            for (var i = 1; i <= 25; i++)
            {
                smallList.Add(new NpcProjection(i, 1, floor: 1, roomId: 101, ActivityKind.Working, false, null, null, 0, 0f));
            }

            var selected = _policy.SelectVisibleNpcs(smallList, VisibleFloorRange.All(4));

            Assert.That(selected.Count, Is.EqualTo(25));
        }
    }
}
