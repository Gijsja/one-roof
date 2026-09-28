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
        public const int DailyBudgetHistoryCapacity = 30;

        private float _satisfaction;
        private readonly long[] _dailyBudgetNetHistory = new long[DailyBudgetHistoryCapacity];
        private int _dailyBudgetHistoryCount;
        private int _dailyBudgetHistoryNext;

        public HouseholdRecord(
            EntityId id,
            IEnumerable<EntityId> memberIds,
            EntityId homeRoomId,
            float budget,
            float satisfaction,
            long? cashBalance = null,
            int arrearsDays = 0,
            long dailyIncome = 0,
            long dailyServiceSpend = 0,
            long rentArrearsBalance = 0,
            int rentArrearsDays = 0,
            long outsideCreditBalance = 0,
            long dailyOutsideEssentialSpend = 0,
            long dailyOutsideQualitySpend = 0,
            long dailyCareSpend = 0,
            long dailyRentDue = 0,
            long dailyRentPaid = 0,
            long dailyOutsideWages = 0,
            long dailyCreditRepayment = 0,
            long[] recentDailyBudgetNetFlows = null,
            long dailyEssentialShortfall = 0,
            long underprovisionExposure = 0)
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
            RentArrearsBalance = Math.Max(0, rentArrearsBalance);
            RentArrearsDays = Math.Max(0, rentArrearsDays);
            OutsideCreditBalance = Math.Max(0, outsideCreditBalance);
            DailyOutsideEssentialSpend = Math.Max(0, dailyOutsideEssentialSpend);
            DailyOutsideQualitySpend = Math.Max(0, dailyOutsideQualitySpend);
            DailyCareSpend = Math.Max(0, dailyCareSpend);
            DailyRentDue = Math.Max(0, dailyRentDue);
            DailyRentPaid = Math.Max(0, dailyRentPaid);
            DailyOutsideWages = Math.Max(0, dailyOutsideWages);
            DailyCreditRepayment = Math.Max(0, dailyCreditRepayment);
            DailyEssentialShortfall = Math.Max(0, dailyEssentialShortfall);
            UnderprovisionExposure = Math.Max(0, underprovisionExposure);
            RestoreDailyBudgetHistory(recentDailyBudgetNetFlows);
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
                return target <= 0 ? 0f : Clamp((float)NetFinancialPosition / target, 0f, 1f);
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

        /// <summary>Unpaid rent only. Consumer credit and other negative cash do not count as rent default.</summary>
        public long RentArrearsBalance { get; private set; }

        /// <summary>Consecutive daily settlements with unpaid rent remaining.</summary>
        public int RentArrearsDays { get; private set; }

        /// <summary>Outstanding, explicitly issued outside-market credit; separate from cash and rent default.</summary>
        public long OutsideCreditBalance { get; private set; }

        /// <summary>Cash minus outstanding outside credit, saturated to the long range.</summary>
        public long NetCashPosition => SaturatingAdd(CashBalance, OutsideCreditBalance == long.MinValue
            ? long.MaxValue
            : -OutsideCreditBalance);

        /// <summary>Liquid cash less outside-service credit and unpaid residential rent.</summary>
        public long NetFinancialPosition => SaturatingAdd(NetCashPosition,
            RentArrearsBalance == long.MinValue ? long.MaxValue : -RentArrearsBalance);

        /// <summary>Last closed day's income less rent due and priced household purchases.</summary>
        public long DailyBudgetNetFlow { get; private set; }
        public int BudgetHistoryDays => _dailyBudgetHistoryCount;
        public long Rolling7DayNetFlow => SumRecentBudgetFlows(Math.Min(7, _dailyBudgetHistoryCount));
        public long Rolling30DayNetFlow => SumRecentBudgetFlows(_dailyBudgetHistoryCount);
        public bool HasPersistentBudgetStress => _dailyBudgetHistoryCount >= 7 && Rolling7DayNetFlow < 0;
        public long DailyEssentialShortfall { get; private set; }
        /// <summary>Slowly changing exposure score in money units for unmet essential services.</summary>
        public long UnderprovisionExposure { get; private set; }

        /// <summary>Role wages transferred into this household during the current settlement day.</summary>
        public long DailyIncome { get; private set; }

        /// <summary>Walk-in food and service spending already transferred by this household today.</summary>
        public long DailyServiceSpend { get; private set; }

        public long DailyOutsideEssentialSpend { get; private set; }
        public long DailyOutsideQualitySpend { get; private set; }
        public long DailyCareSpend { get; private set; }
        public long DailyRentDue { get; private set; }
        public long DailyRentPaid { get; private set; }
        public long DailyOutsideWages { get; private set; }
        public long DailyCreditRepayment { get; private set; }

        /// <summary>Cash this household can still spend on tower walk-in services today. Outside essentials have their own ledger and reduce available cash, not this allowance.</summary>
        public long AvailableServiceSpend => Math.Min(Math.Max(0L, CashBalance),
            Math.Max(0L, MemberIds.Count * 5L - DailyServiceSpend));

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
            CashBalance = SaturatingAdd(CashBalance, delta);
        }

        public void RecordDailyIncome(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var priorCash = CashBalance;
            AdjustCashBalance(amount);
            DailyIncome = SaturatingAdd(DailyIncome, amount);
            if (amount <= 0 || OutsideCreditBalance <= 0 || CashBalance <= 0) return;
            var priorCashDeficit = priorCash < 0
                ? (priorCash == long.MinValue ? long.MaxValue : -priorCash)
                : 0;
            var incomeAvailable = Math.Max(0L, amount - Math.Min(amount, priorCashDeficit));
            var repayment = Math.Min(OutsideCreditBalance, Math.Min(CashBalance, incomeAvailable));
            if (repayment <= 0) return;
            CashBalance -= repayment;
            OutsideCreditBalance -= repayment;
            DailyCreditRepayment = SaturatingAdd(DailyCreditRepayment, repayment);
        }

        public void RecordOutsideWage(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            RecordDailyIncome(amount);
            DailyOutsideWages = SaturatingAdd(DailyOutsideWages, amount);
        }

        public void BeginDailySettlement()
        {
            DailyIncome = 0;
            DailyServiceSpend = 0;
            DailyOutsideEssentialSpend = 0;
            DailyOutsideQualitySpend = 0;
            DailyCareSpend = 0;
            DailyRentDue = 0;
            DailyRentPaid = 0;
            DailyOutsideWages = 0;
            DailyCreditRepayment = 0;
            DailyEssentialShortfall = 0;
        }

        /// <summary>Closes one deterministic daily budget period and retains up to 30 net-flow values.</summary>
        public void CompleteDailyBudgetSettlement()
        {
            var flow = DailyIncome;
            flow = SaturatingAdd(flow, DailyRentDue == long.MinValue ? long.MaxValue : -DailyRentDue);
            flow = SaturatingAdd(flow, DailyServiceSpend == long.MinValue ? long.MaxValue : -DailyServiceSpend);
            flow = SaturatingAdd(flow, DailyOutsideEssentialSpend == long.MinValue ? long.MaxValue : -DailyOutsideEssentialSpend);
            flow = SaturatingAdd(flow, DailyOutsideQualitySpend == long.MinValue ? long.MaxValue : -DailyOutsideQualitySpend);
            flow = SaturatingAdd(flow, DailyCareSpend == long.MinValue ? long.MaxValue : -DailyCareSpend);
            DailyBudgetNetFlow = flow;

            _dailyBudgetNetHistory[_dailyBudgetHistoryNext] = flow;
            _dailyBudgetHistoryNext = (_dailyBudgetHistoryNext + 1) % DailyBudgetHistoryCapacity;
            if (_dailyBudgetHistoryCount < DailyBudgetHistoryCapacity) _dailyBudgetHistoryCount++;

            if (DailyEssentialShortfall > 0)
                UnderprovisionExposure = SaturatingAdd(UnderprovisionExposure, DailyEssentialShortfall);
            else if (UnderprovisionExposure > 0)
                UnderprovisionExposure = Math.Max(0, UnderprovisionExposure - Math.Max(1, MemberIds.Count));
        }

        public void RecordEssentialShortfall(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            DailyEssentialShortfall = SaturatingAdd(DailyEssentialShortfall, amount);
        }

        public long[] CopyRecentDailyBudgetNetFlows()
        {
            var copy = new long[_dailyBudgetHistoryCount];
            var first = (_dailyBudgetHistoryNext - _dailyBudgetHistoryCount + DailyBudgetHistoryCapacity) % DailyBudgetHistoryCapacity;
            for (var i = 0; i < copy.Length; i++) copy[i] = _dailyBudgetNetHistory[(first + i) % DailyBudgetHistoryCapacity];
            return copy;
        }

        private void RestoreDailyBudgetHistory(long[] history)
        {
            if (history == null) return;
            var start = Math.Max(0, history.Length - DailyBudgetHistoryCapacity);
            for (var i = start; i < history.Length; i++)
            {
                _dailyBudgetNetHistory[_dailyBudgetHistoryNext] = history[i];
                _dailyBudgetHistoryNext = (_dailyBudgetHistoryNext + 1) % DailyBudgetHistoryCapacity;
                if (_dailyBudgetHistoryCount < DailyBudgetHistoryCapacity) _dailyBudgetHistoryCount++;
            }
            if (_dailyBudgetHistoryCount > 0) DailyBudgetNetFlow = _dailyBudgetNetHistory[(_dailyBudgetHistoryNext - 1 + DailyBudgetHistoryCapacity) % DailyBudgetHistoryCapacity];
        }

        private long SumRecentBudgetFlows(int count)
        {
            long sum = 0;
            for (var offset = 1; offset <= count; offset++)
            {
                var index = (_dailyBudgetHistoryNext - offset + DailyBudgetHistoryCapacity) % DailyBudgetHistoryCapacity;
                sum = SaturatingAdd(sum, _dailyBudgetNetHistory[index]);
            }
            return sum;
        }

        public long SpendOnService(long requested)
        {
            if (requested <= 0) return 0;
            var paid = Math.Min(requested, AvailableServiceSpend);
            if (paid <= 0) return 0;
            DailyServiceSpend = SaturatingAdd(DailyServiceSpend, paid);
            AdjustCashBalance(-paid);
            return paid;
        }

        /// <summary>Accrues rent as a housing liability, independent of the household's total cash balance.</summary>
        public void RecordRentDue(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            DailyRentDue = SaturatingAdd(DailyRentDue, amount);
            RentArrearsBalance = SaturatingAdd(RentArrearsBalance, amount);
        }

        /// <summary>Pays as much outstanding rent as cash permits and returns the actual transfer amount.</summary>
        public long RecordRentPayment(long requestedAmount)
        {
            if (requestedAmount <= 0 || CashBalance <= 0 || RentArrearsBalance <= 0) return 0;
            var paid = Math.Min(requestedAmount, Math.Min(CashBalance, RentArrearsBalance));
            AdjustCashBalance(-paid);
            RentArrearsBalance -= paid;
            DailyRentPaid = SaturatingAdd(DailyRentPaid, paid);
            return paid;
        }

        /// <summary>Settles a whole outside purchase, using cash first and then bounded market credit.</summary>
        public bool TryPurchaseOutsideService(long price, long creditLimit, OutsideServiceCategory category,
            out long cashPaid, out long creditIssued)
        {
            cashPaid = 0;
            creditIssued = 0;
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
            if (price == 0) return true;
            creditLimit = Math.Max(0, creditLimit);
            var availableCash = Math.Max(0, CashBalance);
            var cash = Math.Min(price, availableCash);
            var creditNeeded = price - cash;
            var creditRoom = Math.Max(0, creditLimit - Math.Min(creditLimit, OutsideCreditBalance));
            if (creditNeeded > creditRoom) return false;

            cashPaid = cash;
            creditIssued = creditNeeded;
            AdjustCashBalance(-cash);
            OutsideCreditBalance = SaturatingAdd(OutsideCreditBalance, creditNeeded);
            RecordOutsideCategorySpend(price, category);
            return true;
        }

        public long RepayOutsideCredit(long requestedAmount)
        {
            if (requestedAmount <= 0 || CashBalance <= 0 || OutsideCreditBalance <= 0) return 0;
            var paid = Math.Min(requestedAmount, Math.Min(CashBalance, OutsideCreditBalance));
            AdjustCashBalance(-paid);
            OutsideCreditBalance -= paid;
            DailyCreditRepayment = SaturatingAdd(DailyCreditRepayment, paid);
            return paid;
        }

        private void RecordOutsideCategorySpend(long amount, OutsideServiceCategory category)
        {
            switch (category)
            {
                case OutsideServiceCategory.EssentialFood: DailyOutsideEssentialSpend = SaturatingAdd(DailyOutsideEssentialSpend, amount); break;
                case OutsideServiceCategory.QualityService: DailyOutsideQualitySpend = SaturatingAdd(DailyOutsideQualitySpend, amount); break;
                case OutsideServiceCategory.Care: DailyCareSpend = SaturatingAdd(DailyCareSpend, amount); break;
                default: DailyOutsideQualitySpend = SaturatingAdd(DailyOutsideQualitySpend, amount); break;
            }
        }

        public void UpdateRentArrearsDays()
        {
            RentArrearsDays = RentArrearsBalance > 0
                ? (RentArrearsDays == int.MaxValue ? int.MaxValue : RentArrearsDays + 1)
                : 0;
        }

        public float CalculateRentBurden(long dailyRentDue)
        {
            if (dailyRentDue <= 0) return 0f;
            if (DailyIncome > 0)
                return Clamp(Math.Max((float)dailyRentDue / DailyIncome, 1f - Budget), 0f, 1f);
            return Clamp(1f - Budget, 0f, 1f);
        }

        /// <summary>Updates delinquency after a daily settlement.</summary>
        public void UpdateArrearsDays()
        {
            UpdateRentArrearsDays();
            ArrearsDays = RentArrearsDays;
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

        private static long SaturatingAdd(long left, long right)
        {
            if (right > 0 && left > long.MaxValue - right) return long.MaxValue;
            if (right < 0 && left < long.MinValue - right) return long.MinValue;
            return left + right;
        }
    }
}
