using System;
using System.Collections.Generic;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Scrutiny
{
    /// <summary>
    /// Pure-domain external pressure state. It records only tower-level conditions and never
    /// assigns behaviour to a particular resident.
    /// </summary>
    public sealed class ScrutinyState
    {
        // Scrutiny never blocks construction directly. High values raise
        // ExternalEventPressure, which the crisis-event system consumes to
        // trigger inspections and incidents; constraint, if any, arrives via
        // an explicit event with its own cause and expiry.
        private readonly List<string> _contributingFactors = new List<string>();
        private float _recentExpansionPressure;
        private float _recentPolicyPressure;

        public ScrutinyState(float value = .10f, float previousValue = .10f, float recentExpansionPressure = 0f, float recentPolicyPressure = 0f)
        {
            Value = Clamp(value);
            PreviousValue = Clamp(previousValue);
            _recentExpansionPressure = Clamp(recentExpansionPressure);
            _recentPolicyPressure = Clamp(recentPolicyPressure);
        }

        public float Value { get; private set; }
        public float PreviousValue { get; private set; }
        public float RecentExpansionPressure => _recentExpansionPressure;
        public float RecentPolicyPressure => _recentPolicyPressure;
        public IReadOnlyList<string> ContributingFactors => _contributingFactors;
        public float ExternalEventPressure => .10f + (.90f * Value);
        public ScrutinyTrend Trend => Value > PreviousValue + .005f ? ScrutinyTrend.Rising : Value < PreviousValue - .005f ? ScrutinyTrend.Falling : ScrutinyTrend.Stable;

        public void RecordExpansion(int structuralUnits)
        {
            if (structuralUnits <= 0) return;
            _recentExpansionPressure = Clamp(_recentExpansionPressure + Math.Min(.30f, structuralUnits * .015f));
        }

        public void RecordCapacityOrServiceInvestment()
        {
            _recentExpansionPressure = Clamp(_recentExpansionPressure - .08f);
            Value = Clamp(Value - .025f);
        }

        /// <summary>Called by future decree management when a policy makes tower conditions less balanced.</summary>
        public void RecordAggressivePolicy(float severity)
        {
            if (severity <= 0f) return;
            _recentPolicyPressure = Clamp(_recentPolicyPressure + severity);
        }

        public void Advance(BuildingTopologyState topology, PopulationState population, float crisisResponseMultiplier = 1f)
        {
            PreviousValue = Value;
            _contributingFactors.Clear();
            var inequality = 0f;
            var unresolvedPressure = 0f;
            var wellbeing = 1f;

            if (population != null && population.Households.Count > 0)
            {
                var minBudget = 1f;
                var maxBudget = 0f;
                var satisfactionTotal = 0f;
                var grievances = 0;
                var strain = 0f;
                foreach (var household in population.Households)
                {
                    minBudget = Math.Min(minBudget, household.Budget);
                    maxBudget = Math.Max(maxBudget, household.Budget);
                }
                foreach (var person in population.Persons)
                {
                    satisfactionTotal += person.Wellbeing.Satisfaction;
                    grievances += person.Wellbeing.Grievances.Count;
                    strain += person.Wellbeing.Strain;
                }
                inequality = maxBudget - minBudget;
                wellbeing = population.Persons.Count == 0 ? 1f : satisfactionTotal / population.Persons.Count;
                // Softened curve: one grievance per resident plus strained
                // residents is a busy tower, not a crisis. Sustained extreme
                // conditions can still push scrutiny high, but ordinary
                // morning-rush pressure must not saturate this driver.
                var grievancesPerPerson = population.Persons.Count == 0 ? 0f : grievances / (float)population.Persons.Count;
                var averageStrain = population.Persons.Count == 0 ? 0f : strain / population.Persons.Count;
                unresolvedPressure = Math.Min(1f, grievancesPerPerson / 3f + averageStrain / 2f);
            }

            var serviceInvestment = CountServiceRooms(topology);
            var expansion = _recentExpansionPressure;
            var policy = _recentPolicyPressure;
            var pressure = expansion * .42f + inequality * .22f + unresolvedPressure * .30f + policy * .25f;
            var responseReadiness = Math.Max(0f, crisisResponseMultiplier - 1f);
            // Relief is deliberately smaller than the pressure gain so single
            // expansions produce a visible bump; mean-reversion (Value term)
            // is the main downward force, settling calm towers low and busy
            // towers moderate without ever pinning at saturation.
            var relief = wellbeing * .02f + Math.Min(.03f, serviceInvestment * .008f) + Math.Min(.02f, responseReadiness * .10f);
            // Scrutiny must signal sustained harmful expansion, not lock the first
            // playable tower after a few ordinary simulation ticks. Fixture-level
            // household budget variation is intentionally broad, so a gentler
            // accumulation rate preserves the player’s core build-response loop
            // while still allowing repeated rapid expansion to spike pressure.
            // Mean-reversion (Value term in relief) guarantees even chronically
            // bad conditions settle below saturation instead of ratcheting to a
            // permanent 100%: scrutiny stays a live signal for the event system.
            Value = Clamp(Value + (pressure * .20f) - relief - (Value * .06f));
            _recentExpansionPressure = Clamp(_recentExpansionPressure - .02f);
            _recentPolicyPressure = Clamp(_recentPolicyPressure - .01f);

            if (expansion > .05f) _contributingFactors.Add("Recent construction is drawing external attention.");
            if (inequality > .25f) _contributingFactors.Add("Household resource inequality is visible across the tower.");
            if (unresolvedPressure > .15f) _contributingFactors.Add("Unresolved grievances and strain are sustaining pressure.");
            if (policy > .05f) _contributingFactors.Add("An aggressive tower policy is increasing external attention.");
            if (serviceInvestment > 0) _contributingFactors.Add($"{serviceInvestment} service space(s) are helping to reduce pressure.");
            if (responseReadiness > .001f) _contributingFactors.Add("Trained specialists are strengthening crisis-response readiness.");
            if (_contributingFactors.Count == 0) _contributingFactors.Add("Conditions are balanced; external attention remains low.");
        }

        private static int CountServiceRooms(BuildingTopologyState topology)
        {
            if (topology == null) return 0;
            var count = 0;
            foreach (var room in topology.Rooms.Values)
            {
                var content = room.ContentType.Value ?? string.Empty;
                if (content.Contains("diner") || content.Contains("amenity") || content.Contains("service")) count++;
            }
            return count;
        }

        private static float Clamp(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }

    public enum ScrutinyTrend { Falling, Stable, Rising }
}
