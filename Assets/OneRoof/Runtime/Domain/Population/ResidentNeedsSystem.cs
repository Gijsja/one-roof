using System;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Pure C# domain system that evaluates and updates the five core needs
    /// (Hunger, Energy, Social, Hygiene, Purpose) for each resident on every tick.
    ///
    /// Need satisfaction values are clamped to [0.0, 1.0].
    /// Replenishment occurs when residents occupy rooms or engage in activities that satisfy needs.
    /// Depletion rates depend on current activity and personality traits (ADR-047).
    /// </summary>
    public sealed class ResidentNeedsSystem
    {
        // ── Replenishment rates per tick ───────────────────────────────────────
        public const float SleepEnergyRecoveryRate = 0.015f;
        public const float EatHungerRecoveryRate   = 0.030f;
        public const float WorkPurposeRecoveryRate = 0.015f;
        public const float LeisureSocialRecoveryRate = 0.020f;
        public const float HomeHygieneRecoveryRate = 0.025f;

        // ── Base decay rates per tick ──────────────────────────────────────────
        public const float DefaultHungerDecayRate   = 0.0015f;
        public const float DefaultEnergyDecayRate   = 0.0010f;
        public const float DefaultSocialDecayRate   = 0.0010f;
        public const float DefaultHygieneDecayRate  = 0.0008f;
        public const float DefaultPurposeDecayRate  = 0.0008f;

        /// <summary>
        /// Advances need dynamics for all persons in the population for the current tick.
        /// </summary>
        public void Advance(PopulationState population, Tick currentTick)
        {
            if (population == null) return;

            var persons = population.Persons;
            for (var i = 0; i < persons.Count; i++)
            {
                AdvancePerson(persons[i]);
            }
        }

        /// <summary>
        /// Updates the need satisfaction levels for an individual resident based on
        /// their current activity, location, and personality traits.
        /// </summary>
        public void AdvancePerson(PersonRecord person)
        {
            if (person == null) return;

            var hunger = person.GetNeedSatisfaction(NeedKind.Hunger);
            var energy = person.GetNeedSatisfaction(NeedKind.Energy);
            var social = person.GetNeedSatisfaction(NeedKind.Social);
            var hygiene = person.GetNeedSatisfaction(NeedKind.Hygiene);
            var purpose = person.GetNeedSatisfaction(NeedKind.Purpose);

            var socialDecayMultiplier = 1.0f;
            for (var i = 0; i < person.Traits.Count; i++)
            {
                if (person.Traits[i].Kind == PersonTraitKind.Introvert)
                {
                    socialDecayMultiplier = 0.5f;
                }
                else if (person.Traits[i].Kind == PersonTraitKind.Extrovert)
                {
                    socialDecayMultiplier = 1.5f;
                }
            }

            switch (person.CurrentActivity)
            {
                case ActivityKind.Sleeping:
                    energy += SleepEnergyRecoveryRate;
                    hunger -= DefaultHungerDecayRate * 0.5f;
                    hygiene -= DefaultHygieneDecayRate * 0.5f;
                    social -= DefaultSocialDecayRate * 0.25f * socialDecayMultiplier;
                    break;

                case ActivityKind.Eating:
                    hunger += EatHungerRecoveryRate;
                    energy -= DefaultEnergyDecayRate * 0.5f;
                    hygiene -= DefaultHygieneDecayRate * 0.5f;
                    social += DefaultSocialDecayRate * 0.5f; // eating near others provides minor social contact
                    break;

                case ActivityKind.Working:
                    purpose += WorkPurposeRecoveryRate;
                    energy -= DefaultEnergyDecayRate * 2.0f;
                    hunger -= DefaultHungerDecayRate * 1.5f;
                    hygiene -= DefaultHygieneDecayRate * 1.2f;
                    social -= DefaultSocialDecayRate * socialDecayMultiplier;
                    break;

                case ActivityKind.Leisure:
                    social += LeisureSocialRecoveryRate;
                    energy -= DefaultEnergyDecayRate * 1.0f;
                    hunger -= DefaultHungerDecayRate * 1.0f;
                    hygiene -= DefaultHygieneDecayRate * 1.0f;
                    purpose -= DefaultPurposeDecayRate * 0.5f;
                    break;

                case ActivityKind.Commuting:
                    energy -= DefaultEnergyDecayRate * 2.2f;
                    hunger -= DefaultHungerDecayRate * 1.5f;
                    hygiene -= DefaultHygieneDecayRate * 1.2f;
                    social -= DefaultSocialDecayRate * 0.5f * socialDecayMultiplier;
                    break;

                case ActivityKind.Idle:
                default:
                    // When idle in home apartment, personal hygiene and rest can recover if low
                    var isAtHome = person.CurrentRoomId.Equals(person.HomeRoomId);
                    if (isAtHome)
                    {
                        if (hygiene < 0.9f)
                        {
                            hygiene += HomeHygieneRecoveryRate;
                        }
                        energy += DefaultEnergyDecayRate * 0.5f;
                        hunger -= DefaultHungerDecayRate * 0.8f;
                        social -= DefaultSocialDecayRate * 0.8f * socialDecayMultiplier;
                        purpose -= DefaultPurposeDecayRate * 1.0f;
                    }
                    else
                    {
                        energy -= DefaultEnergyDecayRate * 1.0f;
                        hunger -= DefaultHungerDecayRate * 1.0f;
                        hygiene -= DefaultHygieneDecayRate * 1.0f;
                        social -= DefaultSocialDecayRate * socialDecayMultiplier;
                        purpose -= DefaultPurposeDecayRate * 1.0f;
                    }
                    break;
            }

            person.UpdateNeed(NeedKind.Hunger, Clamp01(hunger));
            person.UpdateNeed(NeedKind.Energy, Clamp01(energy));
            person.UpdateNeed(NeedKind.Social, Clamp01(social));
            person.UpdateNeed(NeedKind.Hygiene, Clamp01(hygiene));
            person.UpdateNeed(NeedKind.Purpose, Clamp01(purpose));
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
