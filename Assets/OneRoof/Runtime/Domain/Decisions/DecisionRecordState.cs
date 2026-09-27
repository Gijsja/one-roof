using System;
using System.Collections.Generic;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Persistence;

namespace OneRoof.Domain.Decisions
{
    /// <summary>Bounded, saved history of effective decrees and civil actions, with observed daily outcomes.</summary>
    public sealed class DecisionRecordState
    {
        public const int MaxEntries = 64;
        public const int MaxObservationsPerEntry = 4; // enactment baseline plus three later settlements
        private readonly List<DecisionEntrySaveData> _entries = new List<DecisionEntrySaveData>();
        private long _nextId = 1;
        public IReadOnlyList<DecisionEntrySaveData> Entries => _entries;

        public void RecordDecree(TowerSimulation sim, PolicyDecreeState oldPolicy, PolicyDecreeState newPolicy)
        {
            if (sim == null || oldPolicy == null || newPolicy == null) throw new ArgumentNullException(nameof(sim));
            if (oldPolicy == newPolicy) return;
            var entry = New(sim, "decree", "Steward decree", "Effective policy settings changed");
            if (entry == null) return;
            entry.oldSetting = Describe(oldPolicy);
            entry.newSetting = Describe(newPolicy);
            entry.householdIds = new int[sim.Population.Households.Count];
            for (var i = 0; i < entry.householdIds.Length; i++) entry.householdIds[i] = sim.Population.Households[i].Id.Value;
            Array.Sort(entry.householdIds);
            entry.businessIds = new int[sim.Businesses.Businesses.Count];
            for (var i = 0; i < entry.businessIds.Length; i++) entry.businessIds[i] = sim.Businesses.Businesses[i].Id.Value;
            Array.Sort(entry.businessIds);
            entry.residentIds = new int[Math.Min(8, sim.Population.Persons.Count)];
            for (var i = 0; i < entry.residentIds.Length; i++) entry.residentIds[i] = sim.Population.Persons[i].Id.Value;
            Array.Sort(entry.residentIds);
            entry.immediateEffect = $"No immediate treasury transfer; policy scrutiny pressure {sim.Scrutiny.RecentPolicyPressure:0.000}.";
            entry.observations = new[] { Capture(sim, entry) };
        }

        /// <summary>Call once after civil action evaluation. Active onset creates one entry; later phases update it.</summary>
        public void RecordCivilPhases(TowerSimulation sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            foreach (var action in sim.CivilActions.Actions)
            {
                DecisionEntrySaveData existing = null;
                for (var i = _entries.Count - 1; i >= 0; i--)
                    if (_entries[i].kind == "civil_action" && _entries[i].factionId == action.FactionId && _entries[i].title == action.Kind.ToString() && _entries[i].active)
                    { existing = _entries[i]; break; }
                if (action.Phase == CivilActionPhase.Active && existing == null)
                {
                    existing = New(sim, "civil_action", action.Kind.ToString(), action.OnsetCause);
                    if (existing == null) continue;
                    existing.factionId = action.FactionId;
                    existing.phase = action.Phase.ToString();
                    existing.active = true;
                    existing.residentIds = Copy(action.AffectedResidentIds);
                    existing.floorIds = Copy(action.AffectedFloors);
                    existing.immediateEffect = action.Kind == CivilActionKind.RentStrike ? "Residential rent collection at 75% while active." :
                        action.Kind == CivilActionKind.WorkSlowdown ? "Business output at 80% while active." : "Lobby transit capacity at 75% while active.";
                    existing.observations = new[] { Capture(sim, existing) };
                }
                else if (existing != null)
                {
                    existing.phase = action.Phase.ToString();
                    existing.active = action.Phase == CivilActionPhase.Active || action.Phase == CivilActionPhase.Recovery;
                }
            }
        }

        public void ObserveSettlement(TowerSimulation sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            foreach (var entry in _entries)
            {
                var old = entry.observations ?? Array.Empty<DecisionObservationSaveData>();
                if (old.Length == 0 || old.Length >= MaxObservationsPerEntry || old[old.Length - 1].tick >= sim.CurrentTick) continue;
                var updated = new DecisionObservationSaveData[old.Length + 1];
                Array.Copy(old, updated, old.Length);
                updated[old.Length] = Capture(sim, entry);
                entry.observations = updated;
            }
        }

        private DecisionEntrySaveData New(TowerSimulation sim, string kind, string title, string cause)
        {
            if (_entries.Count >= MaxEntries)
            {
                var evict = _entries.FindIndex(e => !e.active);
                if (evict < 0) return null; // Preserve active incidents if retention is saturated.
                _entries.RemoveAt(evict);
            }
            var entry = new DecisionEntrySaveData { id = _nextId++, tick = sim.CurrentTick, kind = kind, title = title,
                cause = cause, phase = string.Empty, residentIds = Array.Empty<int>(), householdIds = Array.Empty<int>(), businessIds = Array.Empty<int>(), floorIds = Array.Empty<int>(),
                observations = Array.Empty<DecisionObservationSaveData>() };
            _entries.Add(entry);
            return entry;
        }

        private static string Describe(PolicyDecreeState p) =>
            $"rent {p.RentCapMultiplier:0.0}x, tax {p.CommercialTaxRate:P0}, transit subsidy {(p.TransitSubsidyEnabled ? "on" : "off")}, quiet hours {(p.QuietHoursEnabled ? "on" : "off")}";
        private static int[] Copy(IReadOnlyList<int> ids)
        { var result = new int[ids.Count]; for (var i = 0; i < ids.Count; i++) result[i] = ids[i]; return result; }

        private static DecisionObservationSaveData Capture(TowerSimulation sim, DecisionEntrySaveData entry)
        {
            long householdCash = 0, businessCash = 0;
            float satisfaction = 0, strain = 0;
            foreach (var household in sim.Population.Households) householdCash += household.CashBalance;
            foreach (var business in sim.Businesses.Businesses) businessCash += business.CashBalance;
            foreach (var resident in sim.Population.Persons)
            { satisfaction += resident.Wellbeing.Satisfaction; strain += resident.Wellbeing.Strain; }
            var count = sim.Population.Persons.Count;
            var pressures = new float[sim.Factions.Factions.Count];
            for (var i = 0; i < pressures.Length; i++) pressures[i] = sim.Factions.Factions[i].Pressure;
            var householdBalances = new long[Math.Min(8, entry.householdIds?.Length ?? 0)];
            var businessBalances = new long[Math.Min(8, entry.businessIds?.Length ?? 0)];
            for (var i = 0; i < householdBalances.Length; i++)
            {
                householdBalances[i] = long.MinValue;
                foreach (var household in sim.Population.Households)
                    if (household.Id.Value == entry.householdIds[i]) { householdBalances[i] = household.CashBalance; break; }
            }
            for (var i = 0; i < businessBalances.Length; i++)
            {
                businessBalances[i] = long.MinValue;
                foreach (var business in sim.Businesses.Businesses)
                    if (business.Id.Value == entry.businessIds[i]) { businessBalances[i] = business.CashBalance; break; }
            }
            return new DecisionObservationSaveData { tick = sim.CurrentTick, treasury = sim.Economy.CashBalance,
                householdCash = householdCash, businessCash = businessCash, rentReceipts = sim.Economy.LastDailyRent,
                taxReceipts = sim.Economy.LastDailyTax, subsidyExpense = sim.Economy.LastDailySubsidy,
                satisfaction = count == 0 ? 0 : satisfaction / count, strain = count == 0 ? 0 : strain / count,
                scrutiny = sim.Scrutiny.Value, recentPolicyPressure = sim.Scrutiny.RecentPolicyPressure, factionPressures = pressures,
                householdBalances = householdBalances, businessBalances = businessBalances };
        }

        public DecisionRecordSaveData ToSaveData()
        {
            var copy = new DecisionEntrySaveData[_entries.Count];
            for (var i = 0; i < copy.Length; i++) copy[i] = Clone(_entries[i]);
            return new DecisionRecordSaveData { version = 1, nextId = _nextId, entries = copy };
        }

        public static DecisionRecordState FromSaveData(DecisionRecordSaveData data)
        {
            var state = new DecisionRecordState();
            if (data == null || data.version != 1 || data.entries == null) return state;
            foreach (var entry in data.entries)
            {
                if (entry == null || entry.id <= 0 || entry.tick < 0 || string.IsNullOrEmpty(entry.kind)) continue;
                if (state._entries.Count >= MaxEntries) break;
                state._entries.Add(Clone(entry));
                state._nextId = Math.Max(state._nextId, entry.id + 1);
            }
            state._nextId = Math.Max(state._nextId, data.nextId);
            return state;
        }

        private static DecisionEntrySaveData Clone(DecisionEntrySaveData e)
        {
            var observations = e.observations ?? Array.Empty<DecisionObservationSaveData>();
            var clipped = new DecisionObservationSaveData[Math.Min(MaxObservationsPerEntry, observations.Length)];
            for (var i = 0; i < clipped.Length; i++)
            {
                var o = observations[i];
                if (o == null) continue;
                clipped[i] = new DecisionObservationSaveData { tick = o.tick, treasury = o.treasury, householdCash = o.householdCash,
                    businessCash = o.businessCash, rentReceipts = o.rentReceipts, taxReceipts = o.taxReceipts,
                    subsidyExpense = o.subsidyExpense, satisfaction = o.satisfaction, strain = o.strain,
                    scrutiny = o.scrutiny, recentPolicyPressure = o.recentPolicyPressure, factionPressures = o.factionPressures == null ? Array.Empty<float>() : (float[])o.factionPressures.Clone(),
                    householdBalances = o.householdBalances == null ? Array.Empty<long>() : (long[])o.householdBalances.Clone(),
                    businessBalances = o.businessBalances == null ? Array.Empty<long>() : (long[])o.businessBalances.Clone() };
            }
            return new DecisionEntrySaveData { id = e.id, tick = e.tick, kind = e.kind, title = e.title, cause = e.cause, immediateEffect = e.immediateEffect,
                oldSetting = e.oldSetting, newSetting = e.newSetting, factionId = e.factionId, phase = e.phase,
                active = e.active, residentIds = e.residentIds == null ? Array.Empty<int>() : (int[])e.residentIds.Clone(),
                householdIds = e.householdIds == null ? Array.Empty<int>() : (int[])e.householdIds.Clone(),
                businessIds = e.businessIds == null ? Array.Empty<int>() : (int[])e.businessIds.Clone(),
                floorIds = e.floorIds == null ? Array.Empty<int>() : (int[])e.floorIds.Clone(), observations = clipped };
        }
    }
}
