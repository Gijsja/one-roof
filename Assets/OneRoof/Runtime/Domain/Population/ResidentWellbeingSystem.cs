using System;
using System.Collections.Generic;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Time;
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

        public void Advance(PopulationState population, ElevatorBank elevatorBank, float serviceEfficiencyMultiplier = 1f, float rentMultiplier = 1f, bool transitSubsidyEnabled = false, bool applyDailyArrearsStrain = false, float dayFraction = 1f / DailySchedule.TicksPerDay, bool quietHoursEnabled = false, float commonsMoraleBoost = 0f)
        {
            if (population == null) return;
            var averageWait = elevatorBank == null ? 0f : elevatorBank.AverageWaitTicks;
            foreach (var person in population.Persons)
            {
                if (!population.TryGetHousehold(person.HouseholdId, out var household) || household == null) continue;
                var commute = Clamp(1f - (person.CurrentActivity == ActivityKind.Commuting ? 0.22f : 0f) - averageWait * 0.01f + (transitSubsidyEnabled ? 0.1f : 0f));
                var crowding = 0.82f;
                var noise = Clamp((person.CurrentActivity == ActivityKind.Commuting ? 0.70f : quietHoursEnabled ? 0.94f : 0.88f) + commonsMoraleBoost);
                var rentDue = (long)Math.Round(household.MemberIds.Count * (double)TowerEconomyState.RentPerResidentPerDay * rentMultiplier, MidpointRounding.AwayFromZero);
                var rentBurden = household.CalculateRentBurden(rentDue);
                var rentAffordability = Clamp(1f - rentBurden);
                var prolongedArrears = household.ArrearsDays > 30;
                var persistentBudgetStress = household.HasPersistentBudgetStress;
                var underprovisioned = household.UnderprovisionExposure >= 32;
                if (prolongedArrears) rentAffordability = Math.Min(rentAffordability, 0.2f);
                else if (persistentBudgetStress) rentAffordability = Math.Min(rentAffordability, 0.55f);
                var service = Clamp(0.80f * serviceEfficiencyMultiplier);
                if (underprovisioned) service = Math.Min(service, household.UnderprovisionExposure >= 96 ? 0.35f : 0.60f);
                var events = Clamp(1f + person.Wellbeing.TotalThoughtMoodDelta * 0.015f);
                var satisfaction = (commute + crowding + noise + rentAffordability + service + events) / 6f;
                _grievanceBuffer.Clear();
                if (commute < GrievanceThreshold) _grievanceBuffer.Add("Long elevator waits are disrupting daily travel.");
                if (rentAffordability < GrievanceThreshold) _grievanceBuffer.Add("Household budget is under rent pressure.");
                if (persistentBudgetStress) _grievanceBuffer.Add("Recent household spending is above recent income.");
                if (underprovisioned) _grievanceBuffer.Add("Unmet essentials are wearing down household wellbeing.");
                if (prolongedArrears) _grievanceBuffer.Add("Unpaid rent puts our lease at risk.");
                if (noise < GrievanceThreshold) _grievanceBuffer.Add("Crowded travel is creating persistent noise stress.");
                var pressure = 1f - satisfaction;
                var multiplier = FacetMultiplier(person, commute, rentAffordability, service, noise);
                var dailyStrainChange = pressure * multiplier * 0.015f
                    + (persistentBudgetStress ? 0.006f : 0f)
                    + (underprovisioned ? 0.004f : 0f)
                    - (satisfaction > .85f ? .01f : 0f);
                var strain = Clamp(person.Wellbeing.Strain + dailyStrainChange * dayFraction + (prolongedArrears && applyDailyArrearsStrain ? .01f : 0f));
                person.Wellbeing.Update(satisfaction, strain, commute, crowding, noise, rentAffordability, service, events, _grievanceBuffer);
            }
        }

        public static float FacetMultiplier(PersonRecord person, float commute, float rentAffordability, float service, float noise)
        {
            var multiplier = 1f;
            foreach (var facet in person.PersonalityFacets)
            {
                switch (facet.Kind)
                {
                    case PersonalityFacetKind.CommuteSensitive: if (commute < .7f) multiplier += .45f; break;
                    case PersonalityFacetKind.FinanciallyCautious: if (rentAffordability < .7f) multiplier += .35f; break;
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
