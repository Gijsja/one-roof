using System;
using System.Collections.Generic;
using OneRoof.Domain.Population;

namespace OneRoof.Application.Population
{
    /// <summary>
    /// Deterministic visibility and prioritization policy that enforces the 40-view cap
    /// across persistent simulation entities.
    /// <para>
    /// When the population exceeds the visible view budget (e.g. 50 persistent residents),
    /// this policy selects the most relevant residents to render based on viewport floor range,
    /// transit state, elevator wait congestion, and activity kind.
    /// </para>
    /// </summary>
    public sealed class NpcVisibilityPolicy
    {
        public const int DefaultMaxVisibleViews = 40;

        private readonly int _maxVisibleViews;

        public NpcVisibilityPolicy(int maxVisibleViews = DefaultMaxVisibleViews)
        {
            if (maxVisibleViews <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxVisibleViews), maxVisibleViews, "Max visible views must be positive.");
            }

            _maxVisibleViews = maxVisibleViews;
        }

        public int MaxVisibleViews => _maxVisibleViews;

        /// <summary>
        /// Selects at most <see cref="MaxVisibleViews"/> projections from <paramref name="candidates"/>,
        /// prioritized deterministically by visible floor range and transit relevance.
        /// </summary>
        public IReadOnlyList<NpcProjection> SelectVisibleNpcs(
            IReadOnlyList<NpcProjection> candidates,
            VisibleFloorRange visibleRange)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (candidates.Count <= _maxVisibleViews)
            {
                // All candidates fit within budget — filter to visible floors if any, or return all.
                // Note: Even if all fit, we still prioritize by floor visibility and activity for determinism.
                var sorted = new List<NpcProjection>(candidates);
                sorted.Sort((a, b) => CompareRelevance(a, b, visibleRange));
                return sorted;
            }

            // More candidates than budget: sort by relevance descending and take top MaxVisibleViews.
            var prioritized = new List<NpcProjection>(candidates);
            prioritized.Sort((a, b) => CompareRelevance(a, b, visibleRange));

            var result = new List<NpcProjection>(_maxVisibleViews);
            for (var i = 0; i < _maxVisibleViews; i++)
            {
                result.Add(prioritized[i]);
            }

            return result;
        }

        /// <summary>
        /// Compares two projections for presentation priority (higher priority first).
        /// </summary>
        public static int CompareRelevance(NpcProjection a, NpcProjection b, VisibleFloorRange visibleRange)
        {
            var scoreA = ComputeScore(a, visibleRange);
            var scoreB = ComputeScore(b, visibleRange);

            if (scoreA != scoreB)
            {
                return scoreB.CompareTo(scoreA); // descending score
            }

            // Deterministic tie-breaker: lower person ID first
            return a.PersonId.CompareTo(b.PersonId);
        }

        private static int ComputeScore(in NpcProjection npc, VisibleFloorRange visibleRange)
        {
            var score = 0;

            // 1. Visible floor bonus (10,000 pts)
            if (visibleRange.Contains(npc.Floor))
            {
                score += 10000;
            }

            // 2. Commute & transit bonus (in transit = 2,000 pts, plus wait congestion up to 1,000 pts)
            if (npc.IsInTransit)
            {
                score += 2000;
                score += Math.Min(npc.WaitTicks * 10, 1000);
            }

            // 3. Activity relevance bonus
            switch (npc.CurrentActivity)
            {
                case ActivityKind.Working:
                case ActivityKind.Eating:
                case ActivityKind.Leisure:
                    score += 500;
                    break;
                case ActivityKind.Idle:
                    score += 100;
                    break;
                case ActivityKind.Sleeping:
                    score += 0;
                    break;
            }

            return score;
        }
    }
}
