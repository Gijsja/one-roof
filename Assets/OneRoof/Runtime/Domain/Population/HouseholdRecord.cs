using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Mutable domain record for a household — the economic and social unit that shares a home room.
    /// Satisfaction and budget are normalised to [0, 1] for first-playable scope.
    /// </summary>
    public sealed class HouseholdRecord
    {
        private float _budget;
        private float _satisfaction;

        public HouseholdRecord(
            EntityId id,
            IEnumerable<EntityId> memberIds,
            EntityId homeRoomId,
            float budget,
            float satisfaction)
        {
            id.EnsureValid();
            homeRoomId.EnsureValid();

            Id = id;
            HomeRoomId = homeRoomId;

            if (budget < 0f || budget > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(budget), budget, "Budget must be in [0, 1].");
            }

            if (satisfaction < 0f || satisfaction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(satisfaction), satisfaction, "Satisfaction must be in [0, 1].");
            }

            _budget = budget;
            _satisfaction = satisfaction;

            MemberIds = memberIds != null
                ? new ReadOnlyCollection<EntityId>(new List<EntityId>(memberIds))
                : new ReadOnlyCollection<EntityId>(new List<EntityId>());
        }

        // ── Identity ──────────────────────────────────────────────────────────

        public EntityId Id { get; }

        public EntityId HomeRoomId { get; }

        public IReadOnlyList<EntityId> MemberIds { get; }

        // ── Economy & wellbeing ───────────────────────────────────────────────

        /// <summary>Normalised wealth tier: 0 = destitute, 1 = affluent.</summary>
        public float Budget => _budget;

        /// <summary>Aggregate household satisfaction: 0 = miserable, 1 = thriving.</summary>
        public float Satisfaction => _satisfaction;

        // ── Mutation methods ──────────────────────────────────────────────────

        /// <summary>Adjusts the household's budget by <paramref name="delta"/>, clamped to [0, 1].</summary>
        public void AdjustBudget(float delta)
        {
            _budget = Clamp(_budget + delta, 0f, 1f);
        }

        /// <summary>Sets the aggregate satisfaction to <paramref name="value"/>, clamped to [0, 1].</summary>
        public void SetSatisfaction(float value)
        {
            _satisfaction = Clamp(value, 0f, 1f);
        }

        public override string ToString() =>
            $"Household {Id} (Home {HomeRoomId}, Members {MemberIds.Count}, Budget {_budget:P0}, Satisfaction {_satisfaction:P0})";

        private static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
