using System;
using System.Collections.Generic;
using OneRoof.Domain.Social;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Population
{
    public readonly struct SocialGrievanceInfluence
    {
        public SocialGrievanceInfluence(string issue, float pressure) { Issue = issue; Pressure = pressure; }
        public string Issue { get; }
        public float Pressure { get; }
    }

    /// <summary>Daily, one-hop grievance sharing. Only direct grievances from the prior settlement can spread.</summary>
    public sealed class ResidentSocialInfluenceSystem
    {
        public const float StrongAffinity = .4f;
        public const long RecentContactTicks = 2 * DailySchedule.TicksPerDay;
        public const float MaxPressure = .12f;
        public const float MaxIssuePressure = .04f;
        public const float ReliefPerSettlement = .02f;

        public void AdvanceSettlement(PopulationState population, FactionState factions, long tick)
        {
            if (population == null || factions == null) return;
            foreach (var recipient in population.Persons)
            {
                var issues = new SortedDictionary<string, float>(StringComparer.Ordinal);
                foreach (var previous in recipient.Wellbeing.SocialInfluences)
                    if (previous.Pressure > ReliefPerSettlement) issues[previous.Issue] = previous.Pressure - ReliefPerSettlement;
                foreach (var edge in factions.Edges)
                {
                    if (edge.Affinity < StrongAffinity || edge.LastContactTick > tick || tick - edge.LastContactTick > RecentContactTicks) continue;
                    var sourceId = edge.First == recipient.Id ? edge.Second : edge.Second == recipient.Id ? edge.First : default;
                    if (!sourceId.IsValid || !population.TryGetPerson(sourceId, out var source)) continue;
                    foreach (var issue in source.Wellbeing.PriorSettlementGrievances)
                    {
                        // Exposure stays inspectable through direct grievances; social concern remains separate.
                        if (!issues.ContainsKey(issue)) issues[issue] = 0f;
                        issues[issue] = Math.Min(MaxIssuePressure, issues[issue] + .02f);
                    }
                }
                var next = new List<SocialGrievanceInfluence>();
                var remaining = MaxPressure;
                foreach (var issue in issues)
                {
                    var pressure = Math.Min(remaining, issue.Value);
                    if (pressure > 0f) next.Add(new SocialGrievanceInfluence(issue.Key, pressure));
                    remaining -= pressure;
                }
                recipient.Wellbeing.ReplaceSocialInfluences(next);
            }
            // Capture only after everyone reads the previous boundary: population order cannot cause a cascade.
            foreach (var person in population.Persons) person.Wellbeing.CaptureSettlementGrievances();
        }
    }
}
