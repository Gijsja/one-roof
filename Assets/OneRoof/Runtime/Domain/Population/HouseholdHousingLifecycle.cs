using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Population
{
    /// <summary>Derived housing condition while rent obligations remain unpaid.</summary>
    public enum HousingConditionStage
    {
        Maintained = 0,
        Worn = 1,
        Degraded = 2
    }

    /// <summary>Lease response derived from rent arrears and a persisted notice date.</summary>
    public enum HouseholdLeasePhase
    {
        Current = 0,
        AtRisk = 1,
        Notice = 2,
        MoveOutEligible = 3
    }

    /// <summary>
    /// Serializable, household-keyed state for an eviction notice. Notice timing is
    /// persisted separately from topology and can be cancelled after financial recovery.
    /// </summary>
    public sealed class HouseholdLeaseLifecycleState
    {
        public const int MaxDepartureHistory = 64;
        private readonly Dictionary<EntityId, long> _noticeStartedDays = new Dictionary<EntityId, long>();
        private readonly List<HouseholdDepartureRecord> _departureHistory = new List<HouseholdDepartureRecord>(MaxDepartureHistory);
        private readonly ReadOnlyCollection<HouseholdDepartureRecord> _departureHistoryView;

        public HouseholdLeaseLifecycleState()
        {
            _departureHistoryView = _departureHistory.AsReadOnly();
        }

        public int PendingNoticeCount => _noticeStartedDays.Count;
        public IReadOnlyList<HouseholdDepartureRecord> DepartureHistory => _departureHistoryView;

        public bool HasNotice(EntityId householdId) => _noticeStartedDays.ContainsKey(householdId);

        public bool TryGetNoticeStartedDay(EntityId householdId, out long simulationDay) =>
            _noticeStartedDays.TryGetValue(householdId, out simulationDay);

        /// <summary>Starts a seven-day move-out notice unless one is already active.</summary>
        public bool MarkNotice(EntityId householdId, long simulationDay)
        {
            householdId.EnsureValid();
            if (simulationDay < 0) throw new ArgumentOutOfRangeException(nameof(simulationDay));
            if (_noticeStartedDays.ContainsKey(householdId)) return false;
            _noticeStartedDays.Add(householdId, simulationDay);
            return true;
        }

        /// <summary>Cancels a pending notice, normally after rent arrears are fully cleared.</summary>
        public bool CancelNotice(EntityId householdId) => _noticeStartedDays.Remove(householdId);

        public bool IsMoveOutEligible(EntityId householdId, long simulationDay)
        {
            if (!_noticeStartedDays.TryGetValue(householdId, out var noticeStartedDay)) return false;
            return simulationDay >= noticeStartedDay && simulationDay - noticeStartedDay >= HouseholdHousingLifecycleSystem.NoticeDurationDays;
        }

        public void RemoveHousehold(EntityId householdId) => _noticeStartedDays.Remove(householdId);

        /// <summary>Records a completed household departure once, retaining the newest bounded history.</summary>
        public bool RecordDeparture(EntityId householdId, long simulationDay, long finalCashBalance, long finalRentArrears)
        {
            return RecordDeparture(householdId, simulationDay, finalCashBalance, finalRentArrears, finalOutsideCreditBalance: 0);
        }

        /// <summary>Records final liquid cash and outstanding housing and outside-market obligations.</summary>
        public bool RecordDeparture(EntityId householdId, long simulationDay, long finalCashBalance,
            long finalRentArrears, long finalOutsideCreditBalance)
        {
            householdId.EnsureValid();
            if (simulationDay < 0) throw new ArgumentOutOfRangeException(nameof(simulationDay));
            if (finalRentArrears < 0) throw new ArgumentOutOfRangeException(nameof(finalRentArrears));
            if (finalOutsideCreditBalance < 0) throw new ArgumentOutOfRangeException(nameof(finalOutsideCreditBalance));
            for (var i = 0; i < _departureHistory.Count; i++)
                if (_departureHistory[i].HouseholdId == householdId) return false;

            var record = new HouseholdDepartureRecord(householdId, simulationDay, finalCashBalance, finalRentArrears, finalOutsideCreditBalance);
            var insertAt = _departureHistory.Count;
            for (var i = 0; i < _departureHistory.Count; i++)
            {
                var existing = _departureHistory[i];
                if (simulationDay < existing.SimulationDay ||
                    (simulationDay == existing.SimulationDay && householdId.CompareTo(existing.HouseholdId) < 0))
                {
                    insertAt = i;
                    break;
                }
            }
            _departureHistory.Insert(insertAt, record);
            if (_departureHistory.Count > MaxDepartureHistory) _departureHistory.RemoveAt(0);
            return true;
        }

        public HouseholdLeaseLifecycleSaveData ToSaveData()
        {
            var records = new List<HouseholdLeaseNoticeSaveData>(_noticeStartedDays.Count);
            foreach (var pair in _noticeStartedDays)
                records.Add(new HouseholdLeaseNoticeSaveData { householdId = pair.Key.Value, noticeStartedDay = pair.Value });
            records.Sort((left, right) => left.householdId.CompareTo(right.householdId));
            var departures = new HouseholdDepartureSaveData[_departureHistory.Count];
            for (var i = 0; i < _departureHistory.Count; i++)
            {
                var departure = _departureHistory[i];
                departures[i] = new HouseholdDepartureSaveData
                {
                    householdId = departure.HouseholdId.Value,
                    simulationDay = departure.SimulationDay,
                    finalCashBalance = departure.FinalCashBalance,
                    finalRentArrears = departure.FinalRentArrears,
                    finalOutsideCreditBalance = departure.FinalOutsideCreditBalance
                };
            }
            return new HouseholdLeaseLifecycleSaveData { notices = records.ToArray(), departures = departures };
        }

        public static HouseholdLeaseLifecycleState FromSaveData(HouseholdLeaseLifecycleSaveData data)
        {
            var state = new HouseholdLeaseLifecycleState();
            if (data == null) return state;
            if (data.notices != null)
            {
                foreach (var notice in data.notices)
                {
                    if (notice == null || notice.householdId <= 0 || notice.noticeStartedDay < 0) continue;
                    var id = new EntityId(notice.householdId);
                    if (!state._noticeStartedDays.ContainsKey(id)) state._noticeStartedDays.Add(id, notice.noticeStartedDay);
                }
            }
            if (data.departures != null)
            {
                foreach (var departure in data.departures)
                {
                    if (departure == null || departure.householdId <= 0 || departure.simulationDay < 0 || departure.finalRentArrears < 0) continue;
                    state.RecordDeparture(new EntityId(departure.householdId), departure.simulationDay,
                        departure.finalCashBalance, departure.finalRentArrears, Math.Max(0, departure.finalOutsideCreditBalance));
                }
            }
            return state;
        }
    }

    /// <summary>Immutable record of a household after all members complete departure.</summary>
    public readonly struct HouseholdDepartureRecord
    {
        public HouseholdDepartureRecord(EntityId householdId, long simulationDay, long finalCashBalance,
            long finalRentArrears, long finalOutsideCreditBalance = 0)
        {
            householdId.EnsureValid();
            if (simulationDay < 0) throw new ArgumentOutOfRangeException(nameof(simulationDay));
            if (finalRentArrears < 0) throw new ArgumentOutOfRangeException(nameof(finalRentArrears));
            if (finalOutsideCreditBalance < 0) throw new ArgumentOutOfRangeException(nameof(finalOutsideCreditBalance));
            HouseholdId = householdId;
            SimulationDay = simulationDay;
            FinalCashBalance = finalCashBalance;
            FinalRentArrears = finalRentArrears;
            FinalOutsideCreditBalance = finalOutsideCreditBalance;
        }

        public EntityId HouseholdId { get; }
        public long SimulationDay { get; }
        public long FinalCashBalance { get; }
        public long FinalRentArrears { get; }
        public long FinalOutsideCreditBalance { get; }
    }

    [Serializable]
    public sealed class HouseholdLeaseLifecycleSaveData
    {
        public HouseholdLeaseNoticeSaveData[] notices;
        public HouseholdDepartureSaveData[] departures;
    }

    [Serializable]
    public sealed class HouseholdLeaseNoticeSaveData
    {
        public int householdId;
        public long noticeStartedDay;
    }

    [Serializable]
    public sealed class HouseholdDepartureSaveData
    {
        public int householdId;
        public long simulationDay;
        public long finalCashBalance;
        public long finalRentArrears;
        public long finalOutsideCreditBalance;
    }

    /// <summary>Immutable projection for a household and its home room.</summary>
    public readonly struct HouseholdHousingLifecycleProjection
    {
        public HouseholdHousingLifecycleProjection(
            EntityId householdId,
            EntityId homeRoomId,
            int rentArrearsDays,
            HousingConditionStage roomCondition,
            HouseholdLeasePhase leasePhase,
            bool hasNotice,
            long noticeStartedDay)
        {
            householdId.EnsureValid();
            homeRoomId.EnsureValid();
            HouseholdId = householdId;
            HomeRoomId = homeRoomId;
            RentArrearsDays = Math.Max(0, rentArrearsDays);
            RoomCondition = roomCondition;
            LeasePhase = leasePhase;
            HasNotice = hasNotice;
            NoticeStartedDay = hasNotice ? noticeStartedDay : -1;
        }

        public EntityId HouseholdId { get; }
        public EntityId HomeRoomId { get; }
        public int RentArrearsDays { get; }
        public HousingConditionStage RoomCondition { get; }
        public HouseholdLeasePhase LeasePhase { get; }
        public bool HasNotice { get; }
        public long NoticeStartedDay { get; }
        public bool IsMoveOutEligible => LeasePhase == HouseholdLeasePhase.MoveOutEligible;
    }

    /// <summary>
    /// Advances lease lifecycle once per simulation day. Room condition is a reversible
    /// projection; only the notice date is stateful and must be included in saves.
    /// </summary>
    public sealed class HouseholdHousingLifecycleSystem
    {
        public const int WornAfterRentArrearsDays = 7;
        public const int DegradedAfterRentArrearsDays = 14;
        public const int AtRiskAfterRentArrearsDays = 31;
        public const int NoticeAfterRentArrearsDays = 45;
        public const int NoticeDurationDays = 7;
        public const int FinancialStressHistoryDays = HouseholdRecord.DailyBudgetHistoryCapacity;

        public IReadOnlyList<HouseholdHousingLifecycleProjection> AdvanceDaily(
            HouseholdLeaseLifecycleState lifecycle,
            PopulationState population,
            long simulationDay)
        {
            if (lifecycle == null) throw new ArgumentNullException(nameof(lifecycle));
            if (simulationDay < 0) throw new ArgumentOutOfRangeException(nameof(simulationDay));
            var projections = new List<HouseholdHousingLifecycleProjection>();
            if (population == null) return projections;

            foreach (var household in population.Households)
            {
                var financiallyInsolvent = IsFinanciallyInsolvent(household);
                if (household.RentArrearsDays == 0 && !financiallyInsolvent)
                    lifecycle.CancelNotice(household.Id);
                else if (household.RentArrearsDays >= NoticeAfterRentArrearsDays || financiallyInsolvent)
                    lifecycle.MarkNotice(household.Id, simulationDay);

                projections.Add(Evaluate(household, lifecycle, simulationDay));
            }
            projections.Sort((left, right) => left.HouseholdId.CompareTo(right.HouseholdId));
            return projections;
        }

        public HouseholdHousingLifecycleProjection Evaluate(
            HouseholdRecord household,
            HouseholdLeaseLifecycleState lifecycle,
            long simulationDay)
        {
            if (household == null) throw new ArgumentNullException(nameof(household));
            if (lifecycle == null) throw new ArgumentNullException(nameof(lifecycle));
            if (simulationDay < 0) throw new ArgumentOutOfRangeException(nameof(simulationDay));

            var arrearsDays = Math.Max(0, household.RentArrearsDays);
            var financiallyInsolvent = IsFinanciallyInsolvent(household);
            var negativeTrajectory = HasPersistentNegativeBudget(household);
            var roomCondition = arrearsDays >= DegradedAfterRentArrearsDays || financiallyInsolvent ||
                               household.UnderprovisionExposure >= 96
                ? HousingConditionStage.Degraded
                : arrearsDays >= WornAfterRentArrearsDays || negativeTrajectory ||
                  household.UnderprovisionExposure >= 32
                    ? HousingConditionStage.Worn
                    : HousingConditionStage.Maintained;

            var hasNotice = lifecycle.TryGetNoticeStartedDay(household.Id, out var noticeStartedDay);
            var leasePhase = lifecycle.IsMoveOutEligible(household.Id, simulationDay)
                ? HouseholdLeasePhase.MoveOutEligible
                : hasNotice
                    ? HouseholdLeasePhase.Notice
                    : arrearsDays >= AtRiskAfterRentArrearsDays || financiallyInsolvent
                        ? HouseholdLeasePhase.AtRisk
                        : HouseholdLeasePhase.Current;

            return new HouseholdHousingLifecycleProjection(
                household.Id, household.HomeRoomId, arrearsDays, roomCondition, leasePhase,
                hasNotice, noticeStartedDay);
        }

        public static bool HasPersistentNegativeBudget(HouseholdRecord household) =>
            household != null && household.BudgetHistoryDays >= FinancialStressHistoryDays &&
            household.Rolling30DayNetFlow < 0;

        public static bool IsFinanciallyInsolvent(HouseholdRecord household) =>
            HasPersistentNegativeBudget(household) && household.NetFinancialPosition <= 0;
    }
}
