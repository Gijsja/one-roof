using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Mutable domain record for a household — the economic and social unit that shares a home room.
    /// Cash is authoritative; Budget remains a normalized compatibility projection.
    /// </summary>
    public sealed class HouseholdRecord
    {
        public const long DefaultStartingCash = 200;

        private float _satisfaction;

        public HouseholdRecord(
            EntityId id,
            IEnumerable<EntityId> memberIds,
            EntityId homeRoomId,
            float budget,
            float satisfaction,
            long? cashBalance = null,
            int arrearsDays = 0,
            long dailyIncome = 0,
            long dailyServiceSpend = 0)
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

            MemberIds = memberIds != null
                ? new ReadOnlyCollection<EntityId>(new List<EntityId>(memberIds))
                : new ReadOnlyCollection<EntityId>(new List<EntityId>());

            _satisfaction = satisfaction;
            CashBalance = cashBalance ?? CashFromLegacyBudget(budget, MemberIds.Count);
            ArrearsDays = Math.Max(0, arrearsDays);
            DailyIncome = Math.Max(0, dailyIncome);
            DailyServiceSpend = Math.Max(0, dailyServiceSpend);
        }

        // ── Identity ──────────────────────────────────────────────────────────

        public EntityId Id { get; }

        public EntityId HomeRoomId { get; }

        public IReadOnlyList<EntityId> MemberIds { get; }

        // ── Economy & wellbeing ───────────────────────────────────────────────

        /// <summary>Normalized wealth projection against a 30-day rent reserve.</summary>
        public float Budget
        {
            get
            {
                var target = RentReserveTarget;
                return target <= 0 ? 0f : Clamp((float)CashBalance / target, 0f, 1f);
            }
        }

        /// <summary>Cash reserve equal to 30 days of this household's daily rent.</summary>
        public long RentReserveTarget => MemberIds.Count * 30L * 12L;

        /// <summary>Aggregate household satisfaction: 0 = miserable, 1 = thriving.</summary>
        public float Satisfaction => _satisfaction;

        /// <summary>Household cash ledger. Negative balances represent unpaid obligations.</summary>
        public long CashBalance { get; private set; }

        /// <summary>Consecutive daily settlements with a negative cash balance.</summary>
        public int ArrearsDays { get; private set; }

        /// <summary>Role wages transferred into this household during the current settlement day.</summary>
        public long DailyIncome { get; private set; }

        /// <summary>Walk-in food and service spending already transferred by this household today.</summary>
        public long DailyServiceSpend { get; private set; }

        // ── Mutation methods ──────────────────────────────────────────────────

        /// <summary>Compatibility adapter that converts a normalized budget delta into cash.</summary>
        public void AdjustBudget(float delta)
        {
            var target = RentReserveTarget;
            if (target <= 0) return;
            AdjustCashBalance((long)Math.Round(delta * target, MidpointRounding.AwayFromZero));
        }

        public static long CashFromLegacyBudget(float budget, int memberCount)
        {
            var normalizedBudget = Clamp(budget, 0f, 1f);
            var target = Math.Max(0, memberCount) * 30L * 12L;
            return (long)Math.Round(normalizedBudget * target, MidpointRounding.AwayFromZero);
        }

        /// <summary>Applies a signed cash flow to the household ledger.</summary>
        public void AdjustCashBalance(long delta)
        {
            CashBalance += delta;
        }

        public void RecordDailyIncome(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            AdjustCashBalance(amount);
            DailyIncome += amount;
        }

        public void BeginDailySettlement()
        {
            DailyIncome = 0;
            DailyServiceSpend = 0;
        }

        public long SpendOnService(long requested)
        {
            if (requested <= 0) return 0;
            var dailyCap = MemberIds.Count * 5L;
            var available = Math.Max(0, dailyCap - DailyServiceSpend);
            var paid = Math.Min(requested, available);
            if (paid <= 0) return 0;
            DailyServiceSpend += paid;
            AdjustCashBalance(-paid);
            return paid;
        }

        public float CalculateRentBurden(long dailyRentDue)
        {
            if (dailyRentDue <= 0) return 0f;
            if (DailyIncome <= 0) return 1f;
            return Clamp((float)dailyRentDue / DailyIncome, 0f, 1f);
        }

        /// <summary>Updates delinquency after a daily settlement.</summary>
        public void UpdateArrearsDays()
        {
            ArrearsDays = CashBalance < 0
                ? (ArrearsDays == int.MaxValue ? int.MaxValue : ArrearsDays + 1)
                : 0;
        }

        /// <summary>Sets the aggregate satisfaction to <paramref name="value"/>, clamped to [0, 1].</summary>
        public void SetSatisfaction(float value)
        {
            _satisfaction = Clamp(value, 0f, 1f);
        }

        public override string ToString() =>
            $"Household {Id} (Home {HomeRoomId}, Members {MemberIds.Count}, Budget {Budget:P0}, Satisfaction {_satisfaction:P0})";

        private static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
