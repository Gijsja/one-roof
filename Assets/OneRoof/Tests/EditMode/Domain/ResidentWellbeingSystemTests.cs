using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ResidentWellbeingSystemTests
    {
        [Test]
        public void SameCommutePressure_CommuteSensitiveFacetAccumulatesMoreStrainThanResilientFacet()
        {
            var sensitive = CreatePerson(1, PersonalityFacetKind.CommuteSensitive);
            var resilient = CreatePerson(2, PersonalityFacetKind.Resilient);
            var population = new PopulationState(
                new[] { sensitive, resilient },
                new[]
                {
                    new HouseholdRecord(new EntityId(10), new[] { new EntityId(1) }, new EntityId(20), .4f, .8f),
                    new HouseholdRecord(new EntityId(11), new[] { new EntityId(2) }, new EntityId(21), .4f, .8f)
                });
            sensitive.UpdateActivity(ActivityKind.Commuting);
            resilient.UpdateActivity(ActivityKind.Commuting);

            new ResidentWellbeingSystem().Advance(population, null);

            Assert.That(sensitive.Wellbeing.Strain, Is.GreaterThan(resilient.Wellbeing.Strain));
            Assert.That(sensitive.Wellbeing.Grievances, Is.Not.Empty);
        }

        [Test]
        public void LowBudget_CreatesReadableRentGrievance()
        {
            var person = CreatePerson(1, PersonalityFacetKind.FinanciallyCautious);
            var population = new PopulationState(new[] { person }, new[] { new HouseholdRecord(new EntityId(10), new[] { new EntityId(1) }, new EntityId(20), .2f, .8f) });

            new ResidentWellbeingSystem().Advance(population, null);

            Assert.That(person.Wellbeing.Satisfaction, Is.LessThan(.8f));
            Assert.That(person.Wellbeing.Grievances, Has.Some.Contains("rent pressure"));
        }

        private static PersonRecord CreatePerson(int id, PersonalityFacetKind facet)
        {
            return new PersonRecord(new EntityId(id), new EntityId(id + 9), new EntityId(id + 19), new EntityId(id + 29),
                DailySchedule.Standard(new PersonTrait(PersonTraitKind.EarlyBird), new OneRoof.Domain.Randomness.DeterministicRandomStream((ulong)id)),
                new[] { new NeedState(NeedKind.Hunger, 1f) }, new[] { new PersonTrait(PersonTraitKind.EarlyBird) },
                new[] { new PersonalityFacet(facet) });
        }
    }
}
