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
        public const float ExpansionConstraintThreshold = .80f;

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
        public bool IsExpansionConstrained => Value >= ExpansionConstraintThreshold;
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

        public void Advance(BuildingTopologyState topology, PopulationState population)
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
                unresolvedPressure = population.Persons.Count == 0 ? 0f : Math.Min(1f, (grievances / (float)population.Persons.Count) + (strain / population.Persons.Count));
            }

            var serviceInvestment = CountServiceRooms(topology);
            var expansion = _recentExpansionPressure;
            var policy = _recentPolicyPressure;
            var pressure = expansion * .42f + inequality * .22f + unresolvedPressure * .30f + policy * .25f;
            var relief = wellbeing * .035f + Math.Min(.04f, serviceInvestment * .01f);
            Value = Clamp(Value + (pressure * .45f) - relief);
            _recentExpansionPressure = Clamp(_recentExpansionPressure - .02f);
            _recentPolicyPressure = Clamp(_recentPolicyPressure - .01f);

            if (expansion > .05f) _contributingFactors.Add("Recent construction is drawing external attention.");
            if (inequality > .25f) _contributingFactors.Add("Household resource inequality is visible across the tower.");
            if (unresolvedPressure > .15f) _contributingFactors.Add("Unresolved grievances and strain are sustaining pressure.");
            if (policy > .05f) _contributingFactors.Add("An aggressive tower policy is increasing external attention.");
            if (serviceInvestment > 0) _contributingFactors.Add($"{serviceInvestment} service space(s) are helping to reduce pressure.");
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
