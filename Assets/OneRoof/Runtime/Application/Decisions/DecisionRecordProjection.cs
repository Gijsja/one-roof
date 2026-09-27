using System;
using System.Collections.Generic;
using OneRoof.Domain;
using OneRoof.Domain.Decisions;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;

namespace OneRoof.Application.Decisions
{
    public sealed class DecisionRecordProjection
    {
        public DecisionRecordProjection(IReadOnlyList<DecisionEntryProjection> entries) { Entries = entries; }
        public IReadOnlyList<DecisionEntryProjection> Entries { get; }
    }

    public sealed class DecisionEntryProjection
    {
        public DecisionEntryProjection(DecisionEntrySaveData entry)
        {
            Id = entry.id;
            Day = entry.tick / DailySchedule.TicksPerDay + 1;
            Kind = entry.kind;
            Title = entry.title ?? entry.kind;
            Cause = entry.cause ?? "Cause unavailable";
            ImmediateEffect = entry.immediateEffect ?? string.Empty;
            OldSetting = entry.oldSetting ?? string.Empty;
            NewSetting = entry.newSetting ?? string.Empty;
            FactionId = entry.factionId ?? string.Empty;
            Phase = entry.phase ?? string.Empty;
            ResidentIds = Array.AsReadOnly((int[])(entry.residentIds ?? Array.Empty<int>()).Clone());
            HouseholdIds = Array.AsReadOnly((int[])(entry.householdIds ?? Array.Empty<int>()).Clone());
            BusinessIds = Array.AsReadOnly((int[])(entry.businessIds ?? Array.Empty<int>()).Clone());
            FloorIds = Array.AsReadOnly((int[])(entry.floorIds ?? Array.Empty<int>()).Clone());
            var observations = entry.observations ?? Array.Empty<DecisionObservationSaveData>();
            var projected = new DecisionObservationProjection[observations.Length];
            for (var i = 0; i < projected.Length; i++) projected[i] = new DecisionObservationProjection(observations[i]);
            Observations = Array.AsReadOnly(projected);
        }
        public long Id { get; }
        public long Day { get; }
        public string Kind { get; }
        public string Title { get; }
        public string Cause { get; }
        public string ImmediateEffect { get; }
        public string OldSetting { get; }
        public string NewSetting { get; }
        public string FactionId { get; }
        public string Phase { get; }
        public IReadOnlyList<int> ResidentIds { get; }
        public IReadOnlyList<int> HouseholdIds { get; }
        public IReadOnlyList<int> BusinessIds { get; }
        public IReadOnlyList<int> FloorIds { get; }
        public IReadOnlyList<DecisionObservationProjection> Observations { get; }
    }

    public sealed class DecisionObservationProjection
    {
        public DecisionObservationProjection(DecisionObservationSaveData observation)
        {
            Tick = observation?.tick ?? 0;
            Treasury = observation?.treasury ?? 0;
            HouseholdCash = observation?.householdCash ?? 0;
            BusinessCash = observation?.businessCash ?? 0;
            RentReceipts = observation?.rentReceipts ?? 0;
            TaxReceipts = observation?.taxReceipts ?? 0;
            SubsidyExpense = observation?.subsidyExpense ?? 0;
            Satisfaction = observation?.satisfaction ?? 0;
            Strain = observation?.strain ?? 0;
            Scrutiny = observation?.scrutiny ?? 0;
            RecentPolicyPressure = observation?.recentPolicyPressure ?? 0;
            FactionPressures = Array.AsReadOnly((float[])(observation?.factionPressures ?? Array.Empty<float>()).Clone());
            HouseholdBalances = Array.AsReadOnly((long[])(observation?.householdBalances ?? Array.Empty<long>()).Clone());
            BusinessBalances = Array.AsReadOnly((long[])(observation?.businessBalances ?? Array.Empty<long>()).Clone());
        }
        public long Tick { get; }
        public long Treasury { get; }
        public long HouseholdCash { get; }
        public long BusinessCash { get; }
        public long RentReceipts { get; }
        public long TaxReceipts { get; }
        public long SubsidyExpense { get; }
        public float Satisfaction { get; }
        public float Strain { get; }
        public float Scrutiny { get; }
        public float RecentPolicyPressure { get; }
        public IReadOnlyList<float> FactionPressures { get; }
        public IReadOnlyList<long> HouseholdBalances { get; }
        public IReadOnlyList<long> BusinessBalances { get; }
    }

    public static class DecisionProjectionService
    {
        public static DecisionRecordProjection Capture(TowerSimulation simulation)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            var source = simulation.Decisions.Entries;
            var entries = new DecisionEntryProjection[source.Count];
            for (var i = 0; i < source.Count; i++) entries[i] = new DecisionEntryProjection(source[i]);
            return new DecisionRecordProjection(Array.AsReadOnly(entries));
        }
    }
}
