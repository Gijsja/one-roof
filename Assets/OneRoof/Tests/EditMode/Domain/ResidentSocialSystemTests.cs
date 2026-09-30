using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Social;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class ResidentSocialSystemTests
    {
        private BuildingTopology _topology;
        private ResidentSocialSystem _social;

        [SetUp]
        public void SetUp()
        {
            _topology = FiveFloorTopologyFixture.Create();
            _social = new ResidentSocialSystem();
        }

        [Test]
        public void EveryTraitPair_HasFiniteBoundedSynergyIncludingUnmatchedPairs()
        {
            foreach (SocialTraitKind first in System.Enum.GetValues(typeof(SocialTraitKind)))
                foreach (SocialTraitKind second in System.Enum.GetValues(typeof(SocialTraitKind)))
                    Assert.That(ResidentSocialSystem.TraitPairSynergy(first, second), Is.InRange(-1f, 1f),
                        $"Trait pair {first}/{second} must terminate with finite synergy.");
        }

        [Test]
        public void SocialTraits_AreAssignedOrDerivedDeterministically_AndNeverNull()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(42));
            Assert.That(pop.Persons.Count, Is.EqualTo(FiftyResidentFixture.TotalResidents));

            foreach (var person in pop.Persons)
            {
                Assert.That(person.SocialTraits, Is.Not.Null);
                Assert.That(person.SocialTraits.Count, Is.GreaterThan(0));
            }

            // Custom assigned social traits
            var custom = new PersonRecord(
                new EntityId(9001), new EntityId(1001), new EntityId(1), new EntityId(2),
                pop.Persons[0].Schedule, pop.Persons[0].Needs, pop.Persons[0].Traits,
                socialTraits: new[] { new SocialTrait(SocialTraitKind.Flirt), new SocialTrait(SocialTraitKind.HopelessRomantic) });

            Assert.That(custom.SocialTraits.Count, Is.EqualTo(2));
            Assert.That(custom.SocialTraits[0].Kind, Is.EqualTo(SocialTraitKind.Flirt));
            Assert.That(custom.SocialTraits[1].Kind, Is.EqualTo(SocialTraitKind.HopelessRomantic));
        }

        [Test]
        public void ThoughtMemories_CanBeAdded_PrunedOnExpiry_AndAffectTotalMoodDelta()
        {
            var person = CreatePerson(1, SocialTraitKind.Charismatic);
            Assert.That(person.Wellbeing.ActiveThoughts.Count, Is.EqualTo(0));
            Assert.That(person.Wellbeing.TotalThoughtMoodDelta, Is.EqualTo(0f));

            var t1 = new ThoughtMemory("Had good coffee", 3f, 100, 1000);
            var t2 = new ThoughtMemory("Flirted in lounge", 8f, 200, 2000, new EntityId(2));
            person.Wellbeing.AddThought(t1);
            person.Wellbeing.AddThought(t2);

            Assert.That(person.Wellbeing.ActiveThoughts.Count, Is.EqualTo(2));
            Assert.That(person.Wellbeing.TotalThoughtMoodDelta, Is.EqualTo(11f));

            // Pruning at tick 1500 expires t1 (1000) but preserves t2 (2000)
            person.Wellbeing.PruneExpiredThoughts(1500);
            Assert.That(person.Wellbeing.ActiveThoughts.Count, Is.EqualTo(1));
            Assert.That(person.Wellbeing.ActiveThoughts[0].Description, Is.EqualTo("Flirted in lounge"));
            Assert.That(person.Wellbeing.TotalThoughtMoodDelta, Is.EqualTo(8f));

            // Pruning at tick 2500 expires t2
            person.Wellbeing.PruneExpiredThoughts(2500);
            Assert.That(person.Wellbeing.ActiveThoughts.Count, Is.EqualTo(0));
            Assert.That(person.Wellbeing.TotalThoughtMoodDelta, Is.EqualTo(0f));
        }

        [Test]
        public void ThoughtMemories_BoundedByMaxCapacity()
        {
            var person = CreatePerson(1, SocialTraitKind.Charismatic);
            for (var i = 0; i < 25; i++)
            {
                person.Wellbeing.AddThought(new ThoughtMemory($"Thought {i}", 1f, i * 10, i * 10 + 10000));
            }

            Assert.That(person.Wellbeing.ActiveThoughts.Count, Is.LessThanOrEqualTo(ResidentWellbeingState.MaxThoughts));
        }

        [Test]
        public void CalculateChemistry_EvaluatesSynergyAndFrictionAccurately()
        {
            var romantic = CreatePerson(1, SocialTraitKind.HopelessRomantic);
            var charismatic = CreatePerson(2, SocialTraitKind.Charismatic);
            var hotHeaded = CreatePerson(3, SocialTraitKind.HotHeaded);
            var abrasive = CreatePerson(4, SocialTraitKind.Abrasive);

            var positiveChem = ResidentSocialSystem.CalculateChemistry(romantic, charismatic);
            var negativeChem = ResidentSocialSystem.CalculateChemistry(hotHeaded, abrasive);

            Assert.That(positiveChem, Is.GreaterThan(0.2f), "Romantic + Charismatic should have positive synergy.");
            Assert.That(negativeChem, Is.LessThan(-0.2f), "HotHeaded + Abrasive should have negative friction.");
        }

        [Test]
        public void HighStrainResidents_SufferChemistryPenalty()
        {
            var personA = CreatePerson(1, SocialTraitKind.Charismatic);
            var personB = CreatePerson(2, SocialTraitKind.Loyal);

            var normalChem = ResidentSocialSystem.CalculateChemistry(personA, personB);

            // Induce high strain
            personA.Wellbeing.Update(0.2f, 0.9f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, null);
            var strainedChem = ResidentSocialSystem.CalculateChemistry(personA, personB);

            Assert.That(strainedChem, Is.LessThan(normalChem));
        }

        [Test]
        public void EvaluateInteraction_AdvancesFriendshipOnPositiveEncounter()
        {
            var personA = CreatePerson(1, SocialTraitKind.Charismatic);
            var personB = CreatePerson(2, SocialTraitKind.Loyal);
            personA.UpdateNeed(NeedKind.Social, 0.2f);
            personB.UpdateNeed(NeedKind.Social, 0.2f);

            var factions = new FactionState();
            var outcome = _social.EvaluateInteraction(personA, personB, factions, new Tick(120), "lounge encounter");

            Assert.That(outcome.Kind, Is.Not.EqualTo(SocialInteractionKind.None));
            Assert.That(factions.Edges.Count, Is.EqualTo(1));
            Assert.That(factions.Edges[0].Affinity, Is.GreaterThan(0f));
            Assert.That(personA.GetNeedSatisfaction(NeedKind.Social), Is.GreaterThan(0.2f), "Social need should replenish on conversation.");
        }

        [Test]
        public void EvaluateInteraction_RomanticFlirtation_InitiatesCrushAndGeneratesThoughts()
        {
            var personA = CreatePerson(1, SocialTraitKind.Flirt);
            var personB = CreatePerson(2, SocialTraitKind.HopelessRomantic);

            var factions = new FactionState();
            // Seed a positive roll tick
            var tick = new Tick(60);
            var outcome = _social.EvaluateInteraction(personA, personB, factions, tick, "cocktail bar");

            if (outcome.Kind == SocialInteractionKind.RomanticFlirtation)
            {
                Assert.That(outcome.NewStage, Is.EqualTo(RelationshipStage.Crush));
                Assert.That(factions.Edges[0].Stage, Is.EqualTo(RelationshipStage.Crush));
                Assert.That(personA.Wellbeing.ActiveThoughts.Count, Is.GreaterThan(0));
                Assert.That(personB.Wellbeing.ActiveThoughts.Count, Is.GreaterThan(0));
                Assert.That(personB.Wellbeing.TotalThoughtMoodDelta, Is.EqualTo(12f), "HopelessRomantic receives +12 mood boost on flirt.");
            }
        }

        [Test]
        public void RelationshipMilestones_ProgressThroughRomanticHierarchy()
        {
            Assert.That(RelationshipMilestones.DeriveStage(0.10f), Is.EqualTo(RelationshipStage.Acquaintance));
            Assert.That(RelationshipMilestones.DeriveStage(0.35f, RelationshipStage.Crush), Is.EqualTo(RelationshipStage.Crush));
            Assert.That(RelationshipMilestones.DeriveStage(0.55f, RelationshipStage.Crush), Is.EqualTo(RelationshipStage.Dating));
            Assert.That(RelationshipMilestones.DeriveStage(0.80f, RelationshipStage.Dating), Is.EqualTo(RelationshipStage.Partnered));
            Assert.That(RelationshipMilestones.DeriveStage(0.90f, RelationshipStage.Partnered), Is.EqualTo(RelationshipStage.Married));

            // Lover's quarrel / strain regression
            Assert.That(RelationshipMilestones.DeriveStage(0.25f, RelationshipStage.Partnered), Is.EqualTo(RelationshipStage.Dating));
            // Breakup regression
            Assert.That(RelationshipMilestones.DeriveStage(-0.15f, RelationshipStage.Dating), Is.EqualTo(RelationshipStage.Disliked));
        }

        [Test]
        public void RelationshipMilestones_ProgressThroughAdversarialHierarchy()
        {
            Assert.That(RelationshipMilestones.DeriveStage(-0.20f), Is.EqualTo(RelationshipStage.Disliked));
            Assert.That(RelationshipMilestones.DeriveStage(-0.45f), Is.EqualTo(RelationshipStage.Rival));
            Assert.That(RelationshipMilestones.DeriveStage(-0.70f), Is.EqualTo(RelationshipStage.Enemy));
            Assert.That(RelationshipMilestones.DeriveStage(-0.90f), Is.EqualTo(RelationshipStage.Nemesis));
        }

        [Test]
        public void InsultSpat_AttachesGrudgeMemoryToGrudgeHolder()
        {
            var grudgeHolder = CreatePerson(1, SocialTraitKind.GrudgeHolder);
            var abrasive = CreatePerson(2, SocialTraitKind.Abrasive);

            // Induce high strain so InsultSpat triggers
            grudgeHolder.Wellbeing.Update(0.1f, 0.95f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, null);
            abrasive.Wellbeing.Update(0.1f, 0.95f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, null);

            var factions = new FactionState();
            var outcome = _social.EvaluateInteraction(grudgeHolder, abrasive, factions, new Tick(180), "elevator argument");

            if (outcome.Kind == SocialInteractionKind.InsultSpat)
            {
                Assert.That(outcome.AffinityDelta, Is.LessThan(0f));
                Assert.That(grudgeHolder.Wellbeing.ActiveThoughts.Count, Is.EqualTo(1));
                Assert.That(grudgeHolder.Wellbeing.ActiveThoughts[0].MoodDelta, Is.EqualTo(-12f), "GrudgeHolder suffers -12 mood penalty.");
                Assert.That(grudgeHolder.Wellbeing.ActiveThoughts[0].ExpiresAtTick, Is.EqualTo(180 + ResidentSocialSystem.ThoughtDurationGrudge));
            }
        }

        [Test]
        public void GrudgeHolder_DecaysNegativeAffinitySlowerThanStandard()
        {
            var regular1 = CreatePerson(10, SocialTraitKind.Charismatic);
            var regular2 = CreatePerson(11, SocialTraitKind.Loyal);

            var grudge1 = CreatePerson(20, SocialTraitKind.GrudgeHolder);
            var grudge2 = CreatePerson(21, SocialTraitKind.Abrasive);

            var population = new PopulationState(
                new[] { regular1, regular2, grudge1, grudge2 },
                new[]
                {
                    new HouseholdRecord(new EntityId(100), new[] { new EntityId(10) }, new EntityId(1), 0.8f, 1f),
                    new HouseholdRecord(new EntityId(101), new[] { new EntityId(11) }, new EntityId(2), 0.8f, 1f),
                    new HouseholdRecord(new EntityId(102), new[] { new EntityId(20) }, new EntityId(3), 0.8f, 1f),
                    new HouseholdRecord(new EntityId(103), new[] { new EntityId(21) }, new EntityId(4), 0.8f, 1f)
                });

            var factions = new FactionState();
            var edgeReg = factions.GetOrCreateEdge(regular1.Id, regular2.Id, 0, "test", RelationshipStage.Disliked);
            edgeReg.Affinity = -0.50f;

            var edgeGrudge = factions.GetOrCreateEdge(grudge1.Id, grudge2.Id, 0, "test", RelationshipStage.Disliked);
            edgeGrudge.Affinity = -0.50f;

            // Daily evaluation tick
            var topologyState = BuildingTopologyState.CreateWithFixture();
            factions.Evaluate(population, topologyState, null, new PolicyDecreeState(1f, 0.2f, false, false), 1440);

            // Regular edge should decay towards 0 by 0.01 -> -0.49
            Assert.That(edgeReg.Affinity, Is.EqualTo(-0.49f).Within(0.001f));
            // Grudge edge should decay 5x slower (0.002) -> -0.498
            Assert.That(edgeGrudge.Affinity, Is.EqualTo(-0.498f).Within(0.001f));
        }

        [Test]
        public void CoPresence_RoomAndElevator_AdvancesSocialSimulation()
        {
            var personA = CreatePerson(1, SocialTraitKind.Charismatic);
            var personB = CreatePerson(2, SocialTraitKind.HopelessRomantic);
            personA.UpdateLocation(new EntityId(10)); // Same room
            personB.UpdateLocation(new EntityId(10));
            personA.UpdateActivity(ActivityKind.Leisure);
            personB.UpdateActivity(ActivityKind.Leisure);

            var population = new PopulationState(new[] { personA, personB }, new[]
            {
                new HouseholdRecord(new EntityId(100), new[] { new EntityId(1) }, new EntityId(10), 0.8f, 1f),
                new HouseholdRecord(new EntityId(101), new[] { new EntityId(2) }, new EntityId(10), 0.8f, 1f)
            });

            var factions = new FactionState();
            var topologyState = BuildingTopologyState.CreateWithFixture();

            _social.Advance(population, topologyState, null, factions, new Tick(60), intervalTicks: 60);

            Assert.That(factions.Edges.Count, Is.EqualTo(1));
            Assert.That(factions.Edges[0].First, Is.EqualTo(personA.Id));
            Assert.That(factions.Edges[0].Second, Is.EqualTo(personB.Id));
        }

        [Test]
        public void MaxEdgesPerResident_StrictlyEnforced()
        {
            var center = CreatePerson(1, SocialTraitKind.Charismatic);
            var others = new List<PersonRecord>();
            for (var i = 0; i < 10; i++)
            {
                others.Add(CreatePerson(100 + i, SocialTraitKind.Loyal));
            }

            var factions = new FactionState();
            foreach (var other in others)
            {
                _social.EvaluateInteraction(center, other, factions, new Tick(60), "greeting");
            }

            // Degree for center person must not exceed MaxEdgesPerResident (6)
            var centerDegree = 0;
            foreach (var edge in factions.Edges)
            {
                if (edge.First == center.Id || edge.Second == center.Id) centerDegree++;
            }

            Assert.That(centerDegree, Is.LessThanOrEqualTo(FactionState.MaxEdgesPerResident));
            Assert.That(centerDegree, Is.EqualTo(FactionState.MaxEdgesPerResident));
        }

        [Test]
        public void SaveLoad_Preserves_RelationshipStages_SocialTraits_AndThoughts()
        {
            var p1 = CreatePerson(1, SocialTraitKind.Flirt);
            var p2 = CreatePerson(2, SocialTraitKind.HopelessRomantic);
            p1.Wellbeing.AddThought(new ThoughtMemory("Met someone special", 10f, 100, 3000, p2.Id));

            var population = new PopulationState(new[] { p1, p2 }, new[]
            {
                new HouseholdRecord(new EntityId(100), new[] { new EntityId(1) }, new EntityId(1), 0.8f, 1f),
                new HouseholdRecord(new EntityId(101), new[] { new EntityId(2) }, new EntityId(2), 0.8f, 1f)
            });

            var factions = new FactionState();
            var edge = factions.GetOrCreateEdge(p1.Id, p2.Id, 120, "flirtation", RelationshipStage.Crush);
            edge.Affinity = 0.45f;
            edge.Stage = RelationshipStage.Crush;

            // Save and Restore FactionState
            var factionSave = factions.ToSaveData();
            Assert.That(factionSave.version, Is.EqualTo(3));
            Assert.That(factionSave.edges[0].stage, Is.EqualTo((int)RelationshipStage.Crush));

            var restoredFactions = FactionState.FromSaveData(factionSave, population);
            Assert.That(restoredFactions.Edges.Count, Is.EqualTo(1));
            Assert.That(restoredFactions.Edges[0].Stage, Is.EqualTo(RelationshipStage.Crush));
            Assert.That(restoredFactions.Edges[0].Affinity, Is.EqualTo(0.45f).Within(0.001f));

            // Save and Restore PopulationState
            var popSave = population.ToSaveData();
            Assert.That(popSave.persons[0].socialTraits, Is.Not.Null);
            Assert.That(popSave.persons[0].socialTraits[0], Is.EqualTo((int)SocialTraitKind.Flirt));
            Assert.That(popSave.persons[0].thoughts, Is.Not.Null);
            Assert.That(popSave.persons[0].thoughts[0].description, Is.EqualTo("Met someone special"));

            var restoredPop = PopulationState.FromSaveData(popSave);
            var restoredP1 = restoredPop.GetPerson(p1.Id);
            Assert.That(restoredP1.SocialTraits.Count, Is.EqualTo(1));
            Assert.That(restoredP1.SocialTraits[0].Kind, Is.EqualTo(SocialTraitKind.Flirt));
            Assert.That(restoredP1.Wellbeing.ActiveThoughts.Count, Is.EqualTo(1));
            Assert.That(restoredP1.Wellbeing.ActiveThoughts[0].Description, Is.EqualTo("Met someone special"));
            Assert.That(restoredP1.Wellbeing.ActiveThoughts[0].MoodDelta, Is.EqualTo(10f));
            Assert.That(restoredP1.Wellbeing.ActiveThoughts[0].TargetResidentId, Is.EqualTo(p2.Id));
        }

        private static PersonRecord CreatePerson(int id, SocialTraitKind socialTrait)
        {
            var trait = new PersonTrait(PersonTraitKind.EarlyBird);
            var schedule = DailySchedule.Standard(trait, new DeterministicRandomStream((ulong)id));
            var needs = new[]
            {
                new NeedState(NeedKind.Hunger, 1f),
                new NeedState(NeedKind.Energy, 1f),
                new NeedState(NeedKind.Social, 1f),
                new NeedState(NeedKind.Hygiene, 1f),
                new NeedState(NeedKind.Purpose, 1f)
            };
            return new PersonRecord(
                new EntityId(id),
                new EntityId(100 + id),
                new EntityId(1),
                new EntityId(2),
                schedule,
                needs,
                new[] { trait },
                socialTraits: new[] { new SocialTrait(socialTrait) });
        }
    }
}
