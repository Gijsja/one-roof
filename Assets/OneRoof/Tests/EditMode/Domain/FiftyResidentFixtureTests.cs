using NUnit.Framework;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class FiftyResidentFixtureTests
    {
        private BuildingTopology _topology;

        [SetUp]
        public void SetUp()
        {
            _topology = FiveFloorTopologyFixture.Create();
        }

        // ── Count checks ──────────────────────────────────────────────────────

        [Test]
        public void FixtureCreatesFiftyPersons()
        {
            var state = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));

            Assert.That(state.PersonCount, Is.EqualTo(FiftyResidentFixture.TotalResidents));
        }

        [Test]
        public void FixtureCreatesSixteenHouseholds()
        {
            var state = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));

            Assert.That(state.HouseholdCount, Is.EqualTo(FiftyResidentFixture.TotalHouseholds));
        }

        // ── Referential integrity ─────────────────────────────────────────────

        [Test]
        public void AllPersonsHaveValidHomeRoomIds()
        {
            var state = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(2));

            foreach (var person in state.Persons)
            {
                Assert.That(
                    _topology.TryGetRoom(person.HomeRoomId, out _),
                    Is.True,
                    $"Person {person.Id} has HomeRoomId {person.HomeRoomId} which does not exist in the topology.");
            }
        }

        [Test]
        public void AllPersonsHaveValidWorkplaceRoomIds()
        {
            var state = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(3));

            foreach (var person in state.Persons)
            {
                Assert.That(
                    _topology.TryGetRoom(person.WorkplaceRoomId, out _),
                    Is.True,
                    $"Person {person.Id} has WorkplaceRoomId {person.WorkplaceRoomId} which does not exist in the topology.");
            }
        }

        [Test]
        public void HouseholdMemberIdsMatchPersonRecords()
        {
            var state = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(4));

            foreach (var household in state.Households)
            {
                foreach (var memberId in household.MemberIds)
                {
                    Assert.That(
                        state.TryGetPerson(memberId, out var person),
                        Is.True,
                        $"Household {household.Id} references member {memberId} which does not exist.");

                    Assert.That(
                        person.HouseholdId,
                        Is.EqualTo(household.Id),
                        $"Person {memberId} HouseholdId does not match household {household.Id}.");
                }
            }
        }

        [Test]
        public void EachHouseholdHasAtLeastOneAndAtMostFourMembers()
        {
            var state = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(5));

            foreach (var household in state.Households)
            {
                Assert.That(household.MemberIds.Count, Is.GreaterThanOrEqualTo(1),
                    $"Household {household.Id} has no members.");
                Assert.That(household.MemberIds.Count, Is.LessThanOrEqualTo(4),
                    $"Household {household.Id} has more than 4 members ({household.MemberIds.Count}).");
            }
        }

        // ── Determinism ───────────────────────────────────────────────────────

        [Test]
        public void SameSeedProducesIdenticalScheduleSequence()
        {
            const uint seed = 99999;

            var stateA = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(seed));
            var stateB = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(seed));

            // Compare schedule block boundaries for all persons.
            for (var i = 0; i < stateA.Persons.Count; i++)
            {
                var schedA = stateA.Persons[i].Schedule;
                var schedB = stateB.Persons[i].Schedule;

                Assert.That(schedA.Blocks.Count, Is.EqualTo(schedB.Blocks.Count));
                for (var b = 0; b < schedA.Blocks.Count; b++)
                {
                    Assert.That(schedA.Blocks[b].StartTick, Is.EqualTo(schedB.Blocks[b].StartTick),
                        $"Person index {i}, block {b} StartTick differs between identical seeds.");
                    Assert.That(schedA.Blocks[b].EndTick, Is.EqualTo(schedB.Blocks[b].EndTick),
                        $"Person index {i}, block {b} EndTick differs between identical seeds.");
                }
            }
        }

        [Test]
        public void DifferentSeedsProduceDifferentSchedules()
        {
            var stateA = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var stateB = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(2));

            // At least one person's sleep block should differ.
            var anyDifference = false;
            for (var i = 0; i < stateA.Persons.Count; i++)
            {
                if (stateA.Persons[i].Schedule.Blocks[0].EndTick !=
                    stateB.Persons[i].Schedule.Blocks[0].EndTick)
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.That(anyDifference, Is.True,
                "Different seeds should produce at least one different schedule boundary.");
        }

        // ── Need & activity defaults ──────────────────────────────────────────

        [Test]
        public void AllPersonsStartWithFullNeedsAndIdleActivity()
        {
            var state = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(7));

            foreach (var person in state.Persons)
            {
                Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Idle),
                    $"Person {person.Id} should start Idle.");

                foreach (var need in person.Needs)
                {
                    Assert.That(need.Satisfaction, Is.EqualTo(1f).Within(0.001f),
                        $"Person {person.Id}, need {need.Kind} should start fully satisfied.");
                }
            }
        }
    }
}
