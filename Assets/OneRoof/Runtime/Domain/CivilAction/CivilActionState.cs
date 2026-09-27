using System;
using System.Collections.Generic;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Social;

namespace OneRoof.Domain.CivilAction
{
    public enum CivilActionKind { RentStrike, LobbyProtest, WorkSlowdown }
    public enum CivilActionPhase { Clear, Warning, Active, Recovery, Cooldown }

    /// <summary>One dated faction signal sampled at daily settlement.</summary>
    public readonly struct CivilActionSignal
    {
        public CivilActionSignal(string factionId, float pressure, int memberCount, string grievance, int[] residentIds, int[] floors)
        {
            FactionId = factionId;
            Pressure = pressure;
            MemberCount = memberCount;
            Grievance = grievance ?? "none";
            ResidentIds = residentIds ?? Array.Empty<int>();
            Floors = floors ?? Array.Empty<int>();
        }
        public string FactionId { get; }
        public float Pressure { get; }
        public int MemberCount { get; }
        public string Grievance { get; }
        public int[] ResidentIds { get; }
        public int[] Floors { get; }
    }

    public sealed class CivilActionRecord
    {
        internal CivilActionRecord(CivilActionKind kind, string factionId)
        { Kind = kind; FactionId = factionId; }
        public CivilActionKind Kind { get; }
        public string FactionId { get; }
        public CivilActionPhase Phase { get; internal set; }
        public string OnsetCause { get; internal set; } = "none";
        public IReadOnlyList<int> AffectedResidentIds { get; internal set; } = Array.Empty<int>();
        public IReadOnlyList<int> AffectedFloors { get; internal set; } = Array.Empty<int>();
        public long PhaseStartedTick { get; internal set; }
        public int WarningDays { get; internal set; }
        public int ActiveDays { get; internal set; }
        public int CooldownDays { get; internal set; }
        public long LastEvaluationTick { get; internal set; } = -1;
    }

    /// <summary>Deterministic, once-per-day civil action lifecycle. Scrutiny changes severity only after faction eligibility.</summary>
    public sealed class CivilActionState
    {
        public const float WarningPressure = .30f;
        public const float ActivePressure = .40f;
        public const int MinimumMembers = 2;
        private readonly CivilActionRecord[] _actions =
        {
            new CivilActionRecord(CivilActionKind.RentStrike, FactionIds.TenantUnion),
            new CivilActionRecord(CivilActionKind.LobbyProtest, FactionIds.CivicEcoCouncil),
            new CivilActionRecord(CivilActionKind.WorkSlowdown, FactionIds.CorporateCoalition)
        };
        public IReadOnlyList<CivilActionRecord> Actions => _actions;
        public bool RentStrikeActive => _actions[0].Phase == CivilActionPhase.Active;
        public bool LobbyProtestActive => _actions[1].Phase == CivilActionPhase.Active;
        public bool WorkSlowdownActive => _actions[2].Phase == CivilActionPhase.Active;
        public float ResidentialRentCollectionMultiplier => RentStrikeActive ? .75f : 1f;
        public float LobbyCapacityMultiplier => LobbyProtestActive ? .75f : 1f;
        public float BusinessOutputMultiplier => WorkSlowdownActive ? .8f : 1f;

        public void Evaluate(long tick, IReadOnlyList<CivilActionSignal> signals, float scrutiny)
        {
            if (signals == null) throw new ArgumentNullException(nameof(signals));
            foreach (var action in _actions)
            {
                if (tick <= action.LastEvaluationTick) continue;
                action.LastEvaluationTick = tick;
                CivilActionSignal signal = default;
                foreach (var candidate in signals) if (candidate.FactionId == action.FactionId) { signal = candidate; break; }
                var eligible = signal.MemberCount >= MinimumMembers && IsRelevant(action.Kind, signal.Grievance);
                var warning = eligible && signal.Pressure >= WarningPressure;
                var severe = eligible && signal.Pressure >= ActivePressure;
                switch (action.Phase)
                {
                    case CivilActionPhase.Clear:
                        if (warning) { action.WarningDays = 1; Transition(action, CivilActionPhase.Warning, tick); }
                        break;
                    case CivilActionPhase.Warning:
                        if (!warning) { action.WarningDays = 0; Transition(action, CivilActionPhase.Clear, tick); }
                        else if (severe && ++action.WarningDays >= 2)
                        {
                            action.OnsetCause = signal.Grievance;
                            action.AffectedResidentIds = SortedCopy(signal.ResidentIds);
                            action.AffectedFloors = SortedCopy(signal.Floors);
                            action.ActiveDays = 0;
                            Transition(action, CivilActionPhase.Active, tick);
                        }
                        break;
                    case CivilActionPhase.Active:
                        action.ActiveDays++;
                        if (!warning || action.ActiveDays >= (scrutiny >= .7f ? 4 : 3)) Transition(action, CivilActionPhase.Recovery, tick);
                        break;
                    case CivilActionPhase.Recovery:
                        Transition(action, CivilActionPhase.Cooldown, tick);
                        action.CooldownDays = 0;
                        break;
                    case CivilActionPhase.Cooldown:
                        if (++action.CooldownDays >= 3) { action.WarningDays = 0; Transition(action, CivilActionPhase.Clear, tick); }
                        break;
                }
            }
        }

        private static bool IsRelevant(CivilActionKind kind, string grievance)
        {
            switch (kind)
            {
                case CivilActionKind.RentStrike: return grievance == "rent burden" || grievance == "arrears";
                case CivilActionKind.LobbyProtest: return grievance == "shared services" || grievance == "noise" || grievance == "living strain";
                case CivilActionKind.WorkSlowdown: return grievance == "service reliability" || grievance == "commute" || grievance == "business continuity";
                default: return false;
            }
        }
        private static int[] SortedCopy(int[] ids)
        {
            var copy = (int[])ids.Clone();
            Array.Sort(copy);
            return copy;
        }
        private static void Transition(CivilActionRecord record, CivilActionPhase phase, long tick)
        { record.Phase = phase; record.PhaseStartedTick = tick; }

        public CivilActionSaveData ToSaveData()
        {
            var data = new CivilActionSaveData { version = 1, actions = new CivilActionRecordSaveData[_actions.Length] };
            for (var i = 0; i < _actions.Length; i++)
            {
                var a = _actions[i];
                data.actions[i] = new CivilActionRecordSaveData { kind = (int)a.Kind, phase = (int)a.Phase, factionId = a.FactionId,
                    onsetCause = a.OnsetCause, affectedResidentIds = Copy(a.AffectedResidentIds), affectedFloors = Copy(a.AffectedFloors),
                    phaseStartedTick = a.PhaseStartedTick, warningDays = a.WarningDays, activeDays = a.ActiveDays,
                    cooldownDays = a.CooldownDays, lastEvaluationTick = a.LastEvaluationTick };
            }
            return data;
        }
        public static CivilActionState FromSaveData(CivilActionSaveData data)
        {
            var state = new CivilActionState();
            if (data == null || data.version != 1 || data.actions == null) return state;
            foreach (var saved in data.actions)
            {
                if (saved == null || saved.kind < 0 || saved.kind >= state._actions.Length) continue;
                var action = state._actions[saved.kind];
                if (saved.factionId != action.FactionId || saved.phase < 0 || saved.phase > (int)CivilActionPhase.Cooldown) continue;
                action.Phase = (CivilActionPhase)saved.phase;
                action.OnsetCause = saved.onsetCause ?? "none";
                action.AffectedResidentIds = SortedCopy(saved.affectedResidentIds ?? Array.Empty<int>());
                action.AffectedFloors = SortedCopy(saved.affectedFloors ?? Array.Empty<int>());
                action.PhaseStartedTick = Math.Max(0, saved.phaseStartedTick);
                action.WarningDays = Math.Max(0, saved.warningDays);
                action.ActiveDays = Math.Max(0, saved.activeDays);
                action.CooldownDays = Math.Max(0, saved.cooldownDays);
                action.LastEvaluationTick = saved.lastEvaluationTick;
            }
            return state;
        }
        private static int[] Copy(IReadOnlyList<int> source)
        { var copy = new int[source.Count]; for (var i = 0; i < copy.Length; i++) copy[i] = source[i]; return copy; }
    }
}
