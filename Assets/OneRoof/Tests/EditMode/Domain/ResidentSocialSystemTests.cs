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

        [TestCaseSource(nameof(TraitPairs))]
        public void EveryTraitPair_HasFiniteBoundedSynergyIncludingUnmatchedPairs(
            SocialTraitKind first, SocialTraitKind second)
        {
            Assert.That(ResidentSocialSystem.TraitPairSynergy(first, second), Is.InRange(-1f, 1f));
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
            var personA = CreatePerson(1, SocialTraitKind.Charismatic);
            var personB = CreatePerson(2, SocialTraitKind.HopelessRomantic);

            var factions = new FactionState();
            // Compatible traits and a fixed favorable encounter exercise romance directly.
            var tick = new Tick(28);
            var outcome = _social.EvaluateInteraction(personA, personB, factions, tick, "cocktail bar");

            Assert.That(outcome.Kind, Is.EqualTo(SocialInteractionKind.RomanticFlirtation));
            Assert.That(outcome.NewStage, Is.EqualTo(RelationshipStage.Crush));
            Assert.That(factions.Edges[0].Stage, Is.EqualTo(RelationshipStage.Crush));
            Assert.That(personA.Wellbeing.ActiveThoughts.Count, Is.GreaterThan(0));
            Assert.That(personB.Wellbeing.ActiveThoughts.Count, Is.GreaterThan(0));
            Assert.That(personB.Wellbeing.TotalThoughtMoodDelta, Is.EqualTo(12f), "HopelessRomantic receives +12 mood boost on flirt.");
        }

        [TestCase(0.10f, RelationshipStage.Stranger, RelationshipStage.Acquaintance)]
        [TestCase(0.35f, RelationshipStage.Crush, RelationshipStage.Crush)]
        [TestCase(0.55f, RelationshipStage.Crush, RelationshipStage.Dating)]
        [TestCase(0.80f, RelationshipStage.Dating, RelationshipStage.Partnered)]
        [TestCase(0.90f, RelationshipStage.Partnered, RelationshipStage.Married)]
        [TestCase(0.25f, RelationshipStage.Partnered, RelationshipStage.Dating)]
        [TestCase(-0.15f, RelationshipStage.Dating, RelationshipStage.Disliked)]
        public void RelationshipMilestones_RomanticProgressionAndRegression(
            float affinity, RelationshipStage priorStage, RelationshipStage expectedStage)
        {
            Assert.That(RelationshipMilestones.DeriveStage(affinity, priorStage), Is.EqualTo(expectedStage));
        }

        [TestCase(-0.20f, RelationshipStage.Disliked)]
        [TestCase(-0.45f, RelationshipStage.Rival)]
        [TestCase(-0.70f, RelationshipStage.Enemy)]
        [TestCase(-0.90f, RelationshipStage.Nemesis)]
        public void RelationshipMilestones_AdversarialProgression(float affinity, RelationshipStage expectedStage)
        {
            Assert.That(RelationshipMilestones.DeriveStage(affinity), Is.EqualTo(expectedStage));
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

            Assert.That(outcome.Kind, Is.EqualTo(SocialInteractionKind.InsultSpat));
            Assert.That(outcome.AffinityDelta, Is.LessThan(0f));
            Assert.That(grudgeHolder.Wellbeing.ActiveThoughts.Count, Is.EqualTo(1));
            Assert.That(grudgeHolder.Wellbeing.ActiveThoughts[0].MoodDelta, Is.EqualTo(-12f), "GrudgeHolder suffers -12 mood penalty.");
            Assert.That(grudgeHolder.Wellbeing.ActiveThoughts[0].ExpiresAtTick, Is.EqualTo(180 + ResidentSocialSystem.ThoughtDurationGrudge));
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
            personA.UpdateLocation(new EntityId(6)); // Same room
            personB.UpdateLocation(new EntityId(6));
            personA.UpdateActivity(ActivityKind.Leisure);
            personB.UpdateActivity(ActivityKind.Leisure);

            var population = new PopulationState(new[] { personA, personB }, new[]
            {
                new HouseholdRecord(new EntityId(101), new[] { new EntityId(1) }, new EntityId(6), 0.8f, 1f),
                new HouseholdRecord(new EntityId(102), new[] { new EntityId(2) }, new EntityId(6), 0.8f, 1f)
            });

            var factions = new FactionState();
            var topologyState = BuildingTopologyState.FromBuildingTopology(_topology);

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

        [TestCaseSource(nameof(TraitPairs))]
        public void TraitChemistry_IsSymmetric_ForEveryTraitPair(SocialTraitKind first, SocialTraitKind second)
        {
            Assert.That(ResidentSocialSystem.TraitPairSynergy(first, second),
                Is.EqualTo(ResidentSocialSystem.TraitPairSynergy(second, first)).Within(0.0001f));
        }

        private static IEnumerable<TestCaseData> TraitPairs()
        {
            foreach (SocialTraitKind first in System.Enum.GetValues(typeof(SocialTraitKind)))
                foreach (SocialTraitKind second in System.Enum.GetValues(typeof(SocialTraitKind)))
                    yield return new TestCaseData(first, second);
        }

        [Test]
        public void ThoughtMemories_DirectlyModulateResidentSatisfactionAndStrain()
        {
            var p1 = CreatePerson(1, SocialTraitKind.Charismatic);
            var pop = new PopulationState(new[] { p1 }, new[]
            {
                new HouseholdRecord(new EntityId(101), new[] { new EntityId(1) }, new EntityId(1), 0.8f, 1f)
            });

            var wellbeingSystem = new ResidentWellbeingSystem();
            wellbeingSystem.Advance(pop, null);
            var baseSatisfaction = p1.Wellbeing.Satisfaction;

            // Add strong positive thoughts
            p1.Wellbeing.AddThought(new ThoughtMemory("In love", 12f, 0, 1000));
            wellbeingSystem.Advance(pop, null);
            Assert.That(p1.Wellbeing.Satisfaction, Is.GreaterThan(baseSatisfaction));

            // Clear thoughts and add severe negative thoughts
            p1.Wellbeing.ClearThoughts();
            p1.Wellbeing.AddThought(new ThoughtMemory("Furious argument", -12f, 0, 1000));
            wellbeingSystem.Advance(pop, null);
            Assert.That(p1.Wellbeing.Satisfaction, Is.LessThan(baseSatisfaction));
        }

        [Test]
        public void EdgeEviction_EvictsDormantNeutralEdges_WhenMeaningfulInteractionOccursAtCap()
        {
            var pMain = CreatePerson(1, SocialTraitKind.Charismatic);
            var neighbors = new List<PersonRecord>();
            for (var i = 2; i <= 7; i++)
            {
                neighbors.Add(CreatePerson(i, SocialTraitKind.Loyal));
            }

            var factions = new FactionState();
            // Fill 6 neutral edges for resident 1
            for (var i = 0; i < 6; i++)
            {
                var edge = factions.GetOrCreateEdge(pMain.Id, neighbors[i].Id, 0, "neighbor", RelationshipStage.Acquaintance);
                Assert.That(edge, Is.Not.Null);
                Assert.That(edge.Affinity, Is.EqualTo(0f));
                Assert.That(edge.LastMeaningfulTick, Is.Zero);
            }
            Assert.That(factions.Edges.Count, Is.EqualTo(6));

            // An attempt to create another neutral acquaintance edge should fail (degree capped)
            var newPerson = CreatePerson(99, SocialTraitKind.HopelessRomantic);
            var neutralEdge = factions.GetOrCreateEdge(pMain.Id, newPerson.Id, 0, "neutral encounter", RelationshipStage.Stranger);
            Assert.That(neutralEdge, Is.Null, "Neutral interaction should not evict dormant edges");

            // A meaningful interaction (e.g. Crush) should evict a dormant edge and succeed
            var meaningfulEdge = factions.GetOrCreateEdge(pMain.Id, newPerson.Id, 100, "romantic spark", RelationshipStage.Crush, 0.40f);
            Assert.That(meaningfulEdge, Is.Not.Null, "Meaningful edge must evict dormant edge");
            Assert.That(meaningfulEdge.Stage, Is.EqualTo(RelationshipStage.Crush));
            Assert.That(meaningfulEdge.Affinity, Is.EqualTo(0.40f));
            Assert.That(factions.Edges.Count, Is.EqualTo(6), "Edge cap must remain strictly respected");
        }

        [Test]
        public void RoomGrouping_SkipsResidentsInNonexistentTopologyRooms()
        {
            var p1 = CreatePerson(1, SocialTraitKind.Flirt);
            var p2 = CreatePerson(2, SocialTraitKind.HopelessRomantic);
            // Room 9999 does not exist in topology
            p1.UpdateLocation(new EntityId(9999));
            p2.UpdateLocation(new EntityId(9999));
            p1.UpdateActivity(ActivityKind.Leisure);
            p2.UpdateActivity(ActivityKind.Leisure);

            var pop = new PopulationState(new[] { p1, p2 }, new[]
            {
                new HouseholdRecord(new EntityId(101), new[] { new EntityId(1) }, new EntityId(1), 0.8f, 1f),
                new HouseholdRecord(new EntityId(102), new[] { new EntityId(2) }, new EntityId(2), 0.8f, 1f)
            });

            var factions = new FactionState();
            _social.Advance(pop, BuildingTopologyState.FromBuildingTopology(_topology), null, factions, new Tick(60));

            // Since room 9999 is not in _topology, no room interaction occurs
            Assert.That(factions.Edges.Count, Is.Zero);
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
            Assert.That(factionSave.version, Is.EqualTo(4));
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

        [Test]
        public void Influence_UsesPriorDirectGrievances_AndDoesNotCascade()
        {
            var a = CreatePerson(1, SocialTraitKind.Loyal);
            var b = CreatePerson(2, SocialTraitKind.Loyal);
            var c = CreatePerson(3, SocialTraitKind.Loyal);
            var pop = new PopulationState(new[] { c, b, a }, new HouseholdRecord[0]);
            var factions = new FactionState();
            factions.GetOrCreateEdge(a.Id, b.Id, 10, "conversation").Affinity = .8f;
            factions.GetOrCreateEdge(b.Id, c.Id, 10, "conversation").Affinity = .8f;
            a.Wellbeing.Update(.4f, 0f, 1f, 1f, 1f, .2f, 1f, 1f, new[] { "Rent pressure" });
            var influence = new ResidentSocialInfluenceSystem();
            influence.AdvanceSettlement(pop, factions, 10);
            Assert.That(b.Wellbeing.SocialPressure, Is.Zero, "Current-boundary grievances must wait for the next settlement.");
            influence.AdvanceSettlement(pop, factions, 20);
            Assert.That(b.Wellbeing.SocialInfluences[0].Issue, Is.EqualTo("Rent pressure"));
            Assert.That(b.Wellbeing.Grievances, Is.Empty, "Social influence must not create direct exposure.");
            Assert.That(c.Wellbeing.SocialPressure, Is.Zero);
            var restored = PopulationState.FromSaveData(pop.ToSaveData());
            Assert.That(restored.GetPerson(b.Id).Wellbeing.SocialPressure, Is.EqualTo(.02f).Within(.0001f));
            Assert.That(restored.GetPerson(a.Id).Wellbeing.PriorSettlementGrievances[0], Is.EqualTo("Rent pressure"));
            a.Wellbeing.Update(1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, null);
            influence.AdvanceSettlement(pop, factions, 30);
            influence.AdvanceSettlement(pop, factions, 40);
            Assert.That(b.Wellbeing.SocialPressure, Is.Zero, "Measured direct relief reverses shared concern.");
        }

        [Test]
        public void Influence_RejectsWeakStaleTies_AndCapsMultipleNamedIssues()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(42));
            var a = pop.Persons[0]; var b = pop.Persons[1];
            a.Wellbeing.Update(.4f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, new[] { "A", "B", "C", "D", "E", "F", "G" });
            a.Wellbeing.CaptureSettlementGrievances();
            var factions = new FactionState();
            var edge = factions.GetOrCreateEdge(a.Id, b.Id, 10, "conversation");
            var influence = new ResidentSocialInfluenceSystem();
            edge.Affinity = .39f;
            influence.AdvanceSettlement(pop, factions, 10);
            Assert.That(b.Wellbeing.SocialPressure, Is.Zero);
            edge.Affinity = .8f;
            influence.AdvanceSettlement(pop, factions, ResidentSocialInfluenceSystem.RecentContactTicks + 11);
            Assert.That(b.Wellbeing.SocialPressure, Is.Zero);
            edge.LastContactTick = 5000;
            influence.AdvanceSettlement(pop, factions, 5000);
            Assert.That(b.Wellbeing.SocialPressure, Is.EqualTo(ResidentSocialInfluenceSystem.MaxPressure).Within(.0001f));
            foreach (var item in b.Wellbeing.SocialInfluences) Assert.That(item.Pressure, Is.LessThanOrEqualTo(ResidentSocialInfluenceSystem.MaxIssuePressure));
        }

        [Test]
        public void Influence_IsIndependentOfPopulationAndEdgeInsertionOrder()
        {
            var a = CreatePerson(1, SocialTraitKind.Loyal); var b = CreatePerson(2, SocialTraitKind.Loyal); var c = CreatePerson(3, SocialTraitKind.Loyal);
            a.Wellbeing.RestoreSocialInfluence(new[] { "Rent" }, null);
            c.Wellbeing.RestoreSocialInfluence(new[] { "Services" }, null);
            var forward = new PopulationState(new[] { a, b, c }, new HouseholdRecord[0]);
            var reverse = PopulationState.FromSaveData(forward.ToSaveData());
            reverse = new PopulationState(new[] { reverse.GetPerson(c.Id), reverse.GetPerson(b.Id), reverse.GetPerson(a.Id) }, new HouseholdRecord[0]);
            var first = new FactionState(); var second = new FactionState();
            first.GetOrCreateEdge(a.Id, b.Id, 10, "conversation").Affinity = .8f;
            first.GetOrCreateEdge(b.Id, c.Id, 10, "conversation").Affinity = .8f;
            second.GetOrCreateEdge(b.Id, c.Id, 10, "conversation").Affinity = .8f;
            second.GetOrCreateEdge(a.Id, b.Id, 10, "conversation").Affinity = .8f;
            new ResidentSocialInfluenceSystem().AdvanceSettlement(forward, first, 10);
            new ResidentSocialInfluenceSystem().AdvanceSettlement(reverse, second, 10);
            var expected = forward.GetPerson(b.Id).Wellbeing.SocialInfluences;
            var actual = reverse.GetPerson(b.Id).Wellbeing.SocialInfluences;
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (var index = 0; index < expected.Count; index++) { Assert.That(actual[index].Issue, Is.EqualTo(expected[index].Issue)); Assert.That(actual[index].Pressure, Is.EqualTo(expected[index].Pressure)); }
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
