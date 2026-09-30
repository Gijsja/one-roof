using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Social;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Population
{
    public enum SocialInteractionKind
    {
        None,
        FriendlyChitchat,
        DeepConversation,
        RomanticFlirtation,
        AwkwardSlight,
        InsultSpat
    }

    public readonly struct SocialInteractionOutcome
    {
        public SocialInteractionOutcome(SocialInteractionKind kind, float affinityDelta, RelationshipStage newStage, string description)
        {
            Kind = kind;
            AffinityDelta = affinityDelta;
            NewStage = newStage;
            Description = description ?? string.Empty;
        }

        public SocialInteractionKind Kind { get; }
        public float AffinityDelta { get; }
        public RelationshipStage NewStage { get; }
        public string Description { get; }
    }

    /// <summary>
    /// Autonomous social interaction engine. Evaluates room and elevator co-presence during
    /// Leisure and Idle activity, rolling deterministic chemistry and generating inspectable thoughts.
    /// Pure C# domain class, 0 UnityEngine references.
    /// </summary>
    public sealed class ResidentSocialSystem
    {
        public const int DefaultIntervalTicks = 60;
        public const int MaxInteractionsPerRoom = 4;
        public const int MaxInteractionsPerElevator = 2;

        public const long ThoughtDurationShort = 1440;   // 1 day
        public const long ThoughtDurationMedium = 2880;  // 2 days
        public const long ThoughtDurationLong = 4320;    // 3 days
        public const long ThoughtDurationGrudge = 7200;  // 5 days

        private readonly Dictionary<int, List<PersonRecord>> _roomBuckets = new Dictionary<int, List<PersonRecord>>();
        private readonly List<PersonRecord> _scratchBucket = new List<PersonRecord>();

        public void Advance(
            PopulationState population,
            BuildingTopologyState topology,
            ElevatorBank elevatorBank,
            FactionState factions,
            Tick currentTick,
            int intervalTicks = DefaultIntervalTicks)
        {
            if (population == null || factions == null) return;
            if (intervalTicks > 1 && currentTick.Value % intervalTicks != 0) return;

            // 1. Prune expired thoughts for all live residents
            for (var i = 0; i < population.Persons.Count; i++)
            {
                population.Persons[i].Wellbeing.PruneExpiredThoughts(currentTick.Value);
            }

            // 2. Room co-presence during Leisure and Idle
            _roomBuckets.Clear();
            for (var i = 0; i < population.Persons.Count; i++)
            {
                var person = population.Persons[i];
                if (person.CurrentLocation.IsOutside || !person.CurrentRoomId.IsValid) continue;
                if (topology != null && !topology.Rooms.ContainsKey(person.CurrentRoomId)) continue;
                if (person.CurrentActivity == ActivityKind.Leisure || person.CurrentActivity == ActivityKind.Idle)
                {
                    var roomId = person.CurrentRoomId.Value;
                    if (!_roomBuckets.TryGetValue(roomId, out var list))
                    {
                        list = new List<PersonRecord>();
                        _roomBuckets[roomId] = list;
                    }
                    list.Add(person);
                }
            }

            foreach (var kvp in _roomBuckets)
            {
                var roomId = kvp.Key;
                var list = kvp.Value;
                if (list.Count < 2) continue;

                list.Sort((a, b) => CompareContact(a, b, roomId, currentTick.Value));
                var pairsProcessed = 0;
                for (var i = 0; i < list.Count - 1 && pairsProcessed < MaxInteractionsPerRoom; i += 2)
                {
                    EvaluateInteraction(list[i], list[i + 1], factions, currentTick, $"room co-presence (room {roomId})");
                    pairsProcessed++;
                }
            }

            // 3. Elevator car co-presence
            if (elevatorBank != null)
            {
                for (var carIndex = 0; carIndex < elevatorBank.Cars.Count; carIndex++)
                {
                    var car = elevatorBank.Cars[carIndex];
                    if (car.Passengers.Count < 2) continue;

                    _scratchBucket.Clear();
                    for (var pIdx = 0; pIdx < car.Passengers.Count; pIdx++)
                    {
                        var passenger = car.Passengers[pIdx];
                        if (population.TryGetPerson(passenger.PersonId, out var person))
                        {
                            _scratchBucket.Add(person);
                        }
                    }

                    if (_scratchBucket.Count < 2) continue;

                    _scratchBucket.Sort((a, b) => CompareContact(a, b, car.Id.Value, currentTick.Value));
                    var elevatorPairs = 0;
                    for (var i = 0; i < _scratchBucket.Count - 1 && elevatorPairs < MaxInteractionsPerElevator; i += 2)
                    {
                        EvaluateInteraction(_scratchBucket[i], _scratchBucket[i + 1], factions, currentTick, "elevator transit");
                        elevatorPairs++;
                    }
                }
            }
        }

        public SocialInteractionOutcome EvaluateInteraction(
            PersonRecord a,
            PersonRecord b,
            FactionState factions,
            Tick currentTick,
            string context = "encounter")
        {
            if (a == null || b == null || a.Id == b.Id)
                return new SocialInteractionOutcome(SocialInteractionKind.None, 0f, RelationshipStage.Stranger, "Self or null");

            var edge = factions?.GetOrCreateEdge(a.Id, b.Id, currentTick.Value, context);
            var currentAffinity = edge?.Affinity ?? 0f;
            var currentStage = edge?.Stage ?? RelationshipStage.Stranger;

            var chemistry = CalculateChemistry(a, b);
            var roll = DeterministicRoll(currentTick.Value, a.Id.Value, b.Id.Value);

            var strainA = a.Wellbeing.Strain;
            var strainB = b.Wellbeing.Strain;
            var satA = a.Wellbeing.Satisfaction;
            var satB = b.Wellbeing.Satisfaction;

            var hasAbrasive = HasSocialTrait(a, SocialTraitKind.Abrasive) || HasSocialTrait(b, SocialTraitKind.Abrasive);
            var hasCharismatic = HasSocialTrait(a, SocialTraitKind.Charismatic) || HasSocialTrait(b, SocialTraitKind.Charismatic);
            var hasHotHeaded = HasSocialTrait(a, SocialTraitKind.HotHeaded) || HasSocialTrait(b, SocialTraitKind.HotHeaded);
            var hasFlirt = HasSocialTrait(a, SocialTraitKind.Flirt) || HasSocialTrait(b, SocialTraitKind.Flirt);
            var hasRomantic = HasSocialTrait(a, SocialTraitKind.HopelessRomantic) || HasSocialTrait(b, SocialTraitKind.HopelessRomantic);

            var score = chemistry * 0.40f + currentAffinity * 0.35f + (roll - 0.5f) * 0.50f;
            if (strainA > 0.60f) score -= 0.15f * strainA;
            if (strainB > 0.60f) score -= 0.15f * strainB;
            if (hasAbrasive && roll < 0.35f) score -= 0.20f;
            if (hasCharismatic && roll > 0.35f) score += 0.20f;

            SocialInteractionKind kind;
            float affinityDelta;
            string description;

            if (score >= 0.30f)
            {
                var isAlreadyRomantic = RelationshipMilestones.IsRomantic(currentStage);
                var attractionCheck = isAlreadyRomantic || hasFlirt || hasRomantic || (chemistry >= 0.20f && roll > 0.35f);
                if (attractionCheck)
                {
                    kind = SocialInteractionKind.RomanticFlirtation;
                    affinityDelta = 0.08f;
                    description = "Romantic Flirtation";
                }
                else
                {
                    kind = SocialInteractionKind.DeepConversation;
                    affinityDelta = 0.06f;
                    description = "Deep Conversation";
                }
            }
            else if (score >= -0.10f)
            {
                kind = SocialInteractionKind.FriendlyChitchat;
                affinityDelta = 0.02f;
                description = "Friendly Chitchat";
            }
            else if (score >= -0.35f)
            {
                kind = SocialInteractionKind.AwkwardSlight;
                affinityDelta = -0.03f;
                description = "Awkward Slight";
            }
            else
            {
                kind = SocialInteractionKind.InsultSpat;
                affinityDelta = -0.08f;
                description = "Insult / Spat";
            }

            var nextAffinity = Math.Max(-1f, Math.Min(1f, currentAffinity + affinityDelta));
            var nextStage = currentStage;

            switch (kind)
            {
                case SocialInteractionKind.RomanticFlirtation:
                {
                    if (currentStage == RelationshipStage.Stranger || currentStage == RelationshipStage.Acquaintance || currentStage == RelationshipStage.Friend)
                    {
                        nextStage = RelationshipStage.Crush;
                    }
                    else if (currentStage == RelationshipStage.Crush && nextAffinity >= 0.50f)
                    {
                        nextStage = RelationshipStage.Dating;
                    }
                    else if (currentStage == RelationshipStage.Dating && nextAffinity >= 0.75f)
                    {
                        nextStage = RelationshipStage.Partnered;
                    }
                    else if (currentStage == RelationshipStage.Partnered && nextAffinity >= 0.85f)
                    {
                        nextStage = RelationshipStage.Married;
                    }
                    else
                    {
                        nextStage = RelationshipMilestones.DeriveStage(nextAffinity, currentStage);
                    }

                    var moodA = HasSocialTrait(a, SocialTraitKind.HopelessRomantic) ? 12f : 8f;
                    var moodB = HasSocialTrait(b, SocialTraitKind.HopelessRomantic) ? 12f : 8f;
                    a.Wellbeing.AddThought(new ThoughtMemory($"Flirted with resident {b.Id.Value}", moodA, currentTick.Value, currentTick.Value + ThoughtDurationMedium, b.Id));
                    b.Wellbeing.AddThought(new ThoughtMemory($"Flirted with resident {a.Id.Value}", moodB, currentTick.Value, currentTick.Value + ThoughtDurationMedium, a.Id));
                    ReplenishSocial(a, 0.10f);
                    ReplenishSocial(b, 0.10f);
                    break;
                }

                case SocialInteractionKind.DeepConversation:
                {
                    nextStage = RelationshipMilestones.DeriveStage(nextAffinity, currentStage);
                    a.Wellbeing.AddThought(new ThoughtMemory($"Had deep conversation with resident {b.Id.Value}", 5f, currentTick.Value, currentTick.Value + ThoughtDurationMedium, b.Id));
                    b.Wellbeing.AddThought(new ThoughtMemory($"Had deep conversation with resident {a.Id.Value}", 5f, currentTick.Value, currentTick.Value + ThoughtDurationMedium, a.Id));
                    ReplenishSocial(a, 0.12f);
                    ReplenishSocial(b, 0.12f);
                    break;
                }

                case SocialInteractionKind.FriendlyChitchat:
                {
                    nextStage = RelationshipMilestones.DeriveStage(nextAffinity, currentStage);
                    ReplenishSocial(a, 0.05f);
                    ReplenishSocial(b, 0.05f);
                    break;
                }

                case SocialInteractionKind.AwkwardSlight:
                {
                    nextStage = RelationshipMilestones.DeriveStage(nextAffinity, currentStage);
                    a.Wellbeing.AddThought(new ThoughtMemory($"Awkward encounter with resident {b.Id.Value}", -3f, currentTick.Value, currentTick.Value + ThoughtDurationShort, b.Id));
                    b.Wellbeing.AddThought(new ThoughtMemory($"Awkward encounter with resident {a.Id.Value}", -3f, currentTick.Value, currentTick.Value + ThoughtDurationShort, a.Id));
                    break;
                }

                case SocialInteractionKind.InsultSpat:
                {
                    if (currentStage == RelationshipStage.Partnered || currentStage == RelationshipStage.Married)
                    {
                        if (nextAffinity < 0.20f) nextStage = RelationshipStage.Dating; // Lover's quarrel
                    }
                    else if (currentStage == RelationshipStage.Dating)
                    {
                        if (nextAffinity < 0f) nextStage = RelationshipStage.Disliked; // Breakup
                    }
                    else
                    {
                        nextStage = RelationshipMilestones.DeriveStage(nextAffinity, currentStage);
                    }

                    var moodA = HasSocialTrait(a, SocialTraitKind.GrudgeHolder) ? -12f : (HasSocialTrait(a, SocialTraitKind.HotHeaded) ? -10f : -8f);
                    var durA = HasSocialTrait(a, SocialTraitKind.GrudgeHolder) ? ThoughtDurationGrudge : ThoughtDurationMedium;
                    var moodB = HasSocialTrait(b, SocialTraitKind.GrudgeHolder) ? -12f : (HasSocialTrait(b, SocialTraitKind.HotHeaded) ? -10f : -8f);
                    var durB = HasSocialTrait(b, SocialTraitKind.GrudgeHolder) ? ThoughtDurationGrudge : ThoughtDurationMedium;

                    a.Wellbeing.AddThought(new ThoughtMemory($"Heated argument with resident {b.Id.Value}", moodA, currentTick.Value, currentTick.Value + durA, b.Id));
                    b.Wellbeing.AddThought(new ThoughtMemory($"Heated argument with resident {a.Id.Value}", moodB, currentTick.Value, currentTick.Value + durB, a.Id));
                    break;
                }
            }

            if (edge == null && factions != null && (kind == SocialInteractionKind.RomanticFlirtation || kind == SocialInteractionKind.DeepConversation || kind == SocialInteractionKind.InsultSpat || nextStage != RelationshipStage.Stranger || Math.Abs(nextAffinity) > 0.001f))
            {
                edge = factions.GetOrCreateEdge(a.Id, b.Id, currentTick.Value, context, nextStage, nextAffinity);
            }

            if (edge != null)
            {
                edge.PreviousAffinity = edge.Affinity;
                edge.Affinity = nextAffinity;
                edge.Stage = nextStage;
                edge.LastContactTick = currentTick.Value;
                edge.Cause = $"{context}: {description.ToLowerInvariant()}";
                if (kind == SocialInteractionKind.RomanticFlirtation || kind == SocialInteractionKind.DeepConversation || kind == SocialInteractionKind.InsultSpat)
                {
                    edge.LastMeaningfulTick = currentTick.Value;
                }
            }

            return new SocialInteractionOutcome(kind, affinityDelta, nextStage, description);
        }

        public static float CalculateChemistry(PersonRecord a, PersonRecord b)
        {
            if (a == null || b == null) return 0f;

            var synergySum = 0f;
            var checks = 0;

            for (var i = 0; i < a.SocialTraits.Count; i++)
            {
                for (var j = 0; j < b.SocialTraits.Count; j++)
                {
                    synergySum += TraitPairSynergy(a.SocialTraits[i].Kind, b.SocialTraits[j].Kind);
                    checks++;
                }
            }

            var traitScore = checks > 0 ? synergySum / checks : 0f;

            var satScore = (a.Wellbeing.Satisfaction > 0.70f && b.Wellbeing.Satisfaction > 0.70f) ? 0.15f : 0f;
            var strainPenalty = 0f;
            if (a.Wellbeing.Strain > 0.60f) strainPenalty += 0.15f * a.Wellbeing.Strain;
            if (b.Wellbeing.Strain > 0.60f) strainPenalty += 0.15f * b.Wellbeing.Strain;

            var result = traitScore + satScore - strainPenalty;
            return Math.Max(-1f, Math.Min(1f, result));
        }

        public static float TraitPairSynergy(SocialTraitKind tA, SocialTraitKind tB) => TraitPairSynergy(tA, tB, true);

        private static float TraitPairSynergy(SocialTraitKind tA, SocialTraitKind tB, bool allowReverse)
        {
            if (tA == SocialTraitKind.HopelessRomantic)
            {
                if (tB == SocialTraitKind.Charismatic) return 0.35f;
                if (tB == SocialTraitKind.Flirt) return -0.20f;
                if (tB == SocialTraitKind.Abrasive) return -0.30f;
                if (tB == SocialTraitKind.Loyal) return 0.25f;
            }
            else if (tA == SocialTraitKind.Charismatic)
            {
                if (tB == SocialTraitKind.HopelessRomantic) return 0.35f;
                if (tB == SocialTraitKind.HotHeaded) return 0.15f;
                if (tB == SocialTraitKind.Jealous) return -0.25f;
                if (tB == SocialTraitKind.Abrasive) return -0.20f;
                return 0.20f;
            }
            else if (tA == SocialTraitKind.Flirt)
            {
                if (tB == SocialTraitKind.Flirt) return 0.30f;
                if (tB == SocialTraitKind.Jealous) return -0.40f;
                if (tB == SocialTraitKind.Loyal) return -0.25f;
                if (tB == SocialTraitKind.HopelessRomantic) return -0.20f;
            }
            else if (tA == SocialTraitKind.HotHeaded)
            {
                if (tB == SocialTraitKind.HotHeaded) return -0.35f;
                if (tB == SocialTraitKind.Abrasive) return -0.40f;
                if (tB == SocialTraitKind.Jealous) return -0.20f;
                if (tB == SocialTraitKind.Charismatic) return 0.15f;
            }
            else if (tA == SocialTraitKind.GrudgeHolder)
            {
                if (tB == SocialTraitKind.Loyal) return 0.30f;
                if (tB == SocialTraitKind.Abrasive) return -0.30f;
                if (tB == SocialTraitKind.HotHeaded) return -0.30f;
                if (tB == SocialTraitKind.Flirt) return -0.25f;
            }
            else if (tA == SocialTraitKind.Abrasive)
            {
                if (tB == SocialTraitKind.HopelessRomantic) return -0.30f;
                if (tB == SocialTraitKind.Charismatic) return -0.20f;
                if (tB == SocialTraitKind.HotHeaded) return -0.40f;
                if (tB == SocialTraitKind.GrudgeHolder) return -0.30f;
            }
            else if (tA == SocialTraitKind.Loyal)
            {
                if (tB == SocialTraitKind.Loyal) return 0.35f;
                if (tB == SocialTraitKind.HopelessRomantic) return 0.25f;
                if (tB == SocialTraitKind.GrudgeHolder) return 0.30f;
                if (tB == SocialTraitKind.Jealous) return 0.25f;
                if (tB == SocialTraitKind.Flirt) return -0.25f;
            }
            else if (tA == SocialTraitKind.Jealous)
            {
                if (tB == SocialTraitKind.Flirt) return -0.40f;
                if (tB == SocialTraitKind.Charismatic) return -0.25f;
                if (tB == SocialTraitKind.Loyal) return 0.25f;
            }

            // Symmetrical lookup if not directly handled
            if (allowReverse && (tB == SocialTraitKind.HopelessRomantic || tB == SocialTraitKind.Charismatic ||
                tB == SocialTraitKind.Flirt || tB == SocialTraitKind.HotHeaded ||
                tB == SocialTraitKind.GrudgeHolder || tB == SocialTraitKind.Abrasive ||
                tB == SocialTraitKind.Loyal || tB == SocialTraitKind.Jealous))
            {
                return TraitPairSynergy(tB, tA, false);
            }

            return 0f;
        }

        public static float DeterministicRoll(long tick, int idA, int idB, uint salt = 0)
        {
            var min = Math.Min(idA, idB);
            var max = Math.Max(idA, idB);
            unchecked
            {
                uint h = (uint)tick * 3266489917u ^ (uint)min * 2654435761u ^ (uint)max * 2246822519u ^ salt;
                h ^= h >> 16; h *= 2246822519u; h ^= h >> 13;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static int CompareContact(PersonRecord a, PersonRecord b, int key, long tick)
        {
            var order = ContactOrder(a.Id.Value, key, tick).CompareTo(ContactOrder(b.Id.Value, key, tick));
            return order != 0 ? order : a.Id.Value.CompareTo(b.Id.Value);
        }

        private static uint ContactOrder(int id, int key, long tick)
        {
            unchecked
            {
                var value = (uint)id * 2654435761u ^ (uint)key * 2246822519u ^ (uint)tick * 3266489917u;
                value ^= value >> 16; value *= 2246822519u; value ^= value >> 13;
                return value;
            }
        }

        public static bool HasSocialTrait(PersonRecord person, SocialTraitKind kind)
        {
            if (person?.SocialTraits == null) return false;
            for (var i = 0; i < person.SocialTraits.Count; i++)
            {
                if (person.SocialTraits[i].Kind == kind) return true;
            }
            return false;
        }

        private static void ReplenishSocial(PersonRecord person, float amount)
        {
            if (person == null) return;
            var current = person.GetNeedSatisfaction(NeedKind.Social);
            person.UpdateNeed(NeedKind.Social, Math.Min(1f, current + amount));
        }
    }
}
