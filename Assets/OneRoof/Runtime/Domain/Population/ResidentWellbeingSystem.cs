using System;
using System.Collections.Generic;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Population
{
    /// <summary>Deterministically derives satisfaction, grievances, and facet-filtered strain.</summary>
    public sealed class ResidentWellbeingSystem
    {
        public const float GrievanceThreshold = 0.48f;

        // Reused for each resident. ResidentWellbeingState copies the values during
        // Update, so this scratch buffer never escapes the system.
        private readonly List<string> _grievanceBuffer = new List<string>(3);

        public void Advance(PopulationState population, ElevatorBank elevatorBank, float serviceEfficiencyMultiplier = 1f, float rentMultiplier = 1f, bool transitSubsidyEnabled = false)
        {
            if (population == null) return;
            var averageWait = elevatorBank == null ? 0f : elevatorBank.AverageWaitTicks;
            foreach (var person in population.Persons)
            {
                var household = population.GetHousehold(person.HouseholdId);
                var commute = Clamp(1f - (person.CurrentActivity == ActivityKind.Commuting ? 0.22f : 0f) - averageWait * 0.01f + (transitSubsidyEnabled ? 0.1f : 0f));
                var crowding = 0.82f;
                var noise = person.CurrentActivity == ActivityKind.Commuting ? 0.70f : 0.88f;
                var rentDue = (long)Math.Round(household.MemberIds.Count * (double)TowerEconomyState.RentPerResidentPerDay * rentMultiplier, MidpointRounding.AwayFromZero);
                var rent = household.CalculateRentBurden(rentDue);
                var service = Clamp(0.80f * serviceEfficiencyMultiplier);
                var events = 1f;
                var satisfaction = (commute + crowding + noise + rent + service + events) / 6f;
                _grievanceBuffer.Clear();
                if (commute < GrievanceThreshold) _grievanceBuffer.Add("Long elevator waits are disrupting daily travel.");
                if (rent < GrievanceThreshold) _grievanceBuffer.Add("Household budget is under rent pressure.");
                if (noise < GrievanceThreshold) _grievanceBuffer.Add("Crowded travel is creating persistent noise stress.");
                var pressure = 1f - satisfaction;
                var multiplier = FacetMultiplier(person, commute, rent, service, noise);
                var strain = Clamp(person.Wellbeing.Strain + (pressure * multiplier * 0.015f) - (satisfaction > .85f ? .01f : 0f));
                person.Wellbeing.Update(satisfaction, strain, commute, crowding, noise, rent, service, events, _grievanceBuffer);
            }
        }

        public static float FacetMultiplier(PersonRecord person, float commute, float rent, float service, float noise)
        {
            var multiplier = 1f;
            foreach (var facet in person.PersonalityFacets)
            {
                switch (facet.Kind)
                {
                    case PersonalityFacetKind.CommuteSensitive: if (commute < .7f) multiplier += .45f; break;
                    case PersonalityFacetKind.FinanciallyCautious: if (rent < .7f) multiplier += .35f; break;
                    case PersonalityFacetKind.ServiceExpectant: if (service < .7f) multiplier += .30f; break;
                    case PersonalityFacetKind.PrivacySeeking: if (noise < .75f) multiplier += .25f; break;
                    case PersonalityFacetKind.Resilient: multiplier -= .35f; break;
                    case PersonalityFacetKind.CommunityRooted: multiplier -= .10f; break;
                }
            }
            return multiplier < .25f ? .25f : multiplier;
        }

        private static float Clamp(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);
    }
}
