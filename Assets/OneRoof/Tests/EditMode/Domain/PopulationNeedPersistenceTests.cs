using NUnit.Framework;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class PopulationNeedPersistenceTests
    {
        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(1f)]
        public void CurrentSave_RestoresEveryNeedExactly(float satisfaction)
        {
            var population = FiftyResidentFixture.Create();
            var person = population.Persons[0];
            for (var i = 0; i < person.Needs.Count; i++)
                person.UpdateNeed(person.Needs[i].Kind, satisfaction);

            var tower = new TowerSaveData();
            tower.SetPopulationSaveData(population.ToSaveData());
            var restored = PopulationState.FromSaveData(tower.GetPopulationSaveData());

            Assert.That(tower.residentNeedsVersion, Is.EqualTo(1));
            foreach (var need in restored.Persons[0].Needs)
                Assert.That(need.Satisfaction, Is.EqualTo(satisfaction), need.Kind.ToString());
        }

        [Test]
        public void CurrentSave_ZeroEnergyDoesNotFallBackToLegacyRest()
        {
            var save = FiftyResidentFixture.Create().ToSaveData();
            save.persons[0].energySatisfaction = 0f;
            save.persons[0].restSatisfaction = 0.75f;

            var restored = PopulationState.FromSaveData(save);

            Assert.That(restored.Persons[0].GetNeedSatisfaction(NeedKind.Energy), Is.Zero);
        }

        [Test]
        public void LegacySave_MissingNeedsUseHistoricalDefaults()
        {
            var save = FiftyResidentFixture.Create().ToSaveData();
            save.residentNeedsVersion = 0;
            var person = save.persons[0];
            person.hungerSatisfaction = 0f;
            person.energySatisfaction = 0f;
            person.restSatisfaction = 0f;
            person.socialSatisfaction = 0f;
            person.hygieneSatisfaction = 0f;
            person.purposeSatisfaction = 0f;

            var restored = PopulationState.FromSaveData(save);

            foreach (var need in restored.Persons[0].Needs)
                Assert.That(need.Satisfaction, Is.EqualTo(1f), need.Kind.ToString());
        }

        [Test]
        public void LegacySave_RestFallbackAndPresentNeedsArePreserved()
        {
            var save = FiftyResidentFixture.Create().ToSaveData();
            save.residentNeedsVersion = 0;
            var person = save.persons[0];
            person.hungerSatisfaction = 0.2f;
            person.energySatisfaction = 0f;
            person.restSatisfaction = 0.3f;
            person.socialSatisfaction = 0.4f;
            person.hygieneSatisfaction = 0.5f;
            person.purposeSatisfaction = 0.6f;

            var restored = PopulationState.FromSaveData(save).Persons[0];

            Assert.That(restored.GetNeedSatisfaction(NeedKind.Hunger), Is.EqualTo(0.2f));
            Assert.That(restored.GetNeedSatisfaction(NeedKind.Energy), Is.EqualTo(0.3f));
            Assert.That(restored.GetNeedSatisfaction(NeedKind.Social), Is.EqualTo(0.4f));
            Assert.That(restored.GetNeedSatisfaction(NeedKind.Hygiene), Is.EqualTo(0.5f));
            Assert.That(restored.GetNeedSatisfaction(NeedKind.Purpose), Is.EqualTo(0.6f));
        }
    }
}
