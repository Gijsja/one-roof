using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Trips
{
    public enum DemandGateStatus { Unavailable, Pending, Failed, Passed }

    [Serializable]
    public sealed class DemandEpisodeData
    {
        public int residentId;
        public TripPurpose purpose;
        public long episodeStartTick, sequence, submissionTick, day, tripId;
        public int queueWait;
        public bool terminal, unreachable;
    }

    [Serializable]
    public sealed class DemandDayData
    {
        public long day, attempted, succeeded, failed, unreachable;
        public int liveMaximum;
        public bool complete, invalid;
        public long[] waitHistogram = new long[362];
        public int P95Wait
        {
            get
            {
                long count = 0;
                foreach (long n in waitHistogram) count = checked(count + n);
                if (count == 0) return 0;
                long rank = count - count / 20;
                long seen = 0;
                for (int i = 0; i < waitHistogram.Length; i++)
                { seen = checked(seen + waitHistogram[i]); if (seen >= rank) return i; }
                return 361;
            }
        }
        public DemandGateStatus Status => invalid || !complete ? DemandGateStatus.Unavailable :
            attempted != succeeded + failed ? DemandGateStatus.Pending :
            unreachable == 0 && (attempted == 0 || (decimal)succeeded / attempted >= .99m) && P95Wait <= 60 && liveMaximum <= 240
                ? DemandGateStatus.Passed : DemandGateStatus.Failed;
    }

    [Serializable]
    public sealed class TransitDemandSaveData
    {
        public int version = 1, capacity;
        public long nextSequence = 1, lastFinalizedDay = -1;
        public bool hasPending;
        public DemandDayData current, pending;
        public DemandEpisodeData[] episodes;
    }

    /// <summary>Complete pre-planning demand cohort. Call terminal outcomes before Advance at each tick.</summary>
    public sealed class TransitDemandObserver
    {
        private readonly Dictionary<(int, TripPurpose), DemandEpisodeData> latest = new Dictionary<(int, TripPurpose), DemandEpisodeData>();
        private readonly Dictionary<long, DemandEpisodeData> unresolved = new Dictionary<long, DemandEpisodeData>();
        private readonly Dictionary<long, long> tripSequences = new Dictionary<long, long>();
        private readonly List<long> expiryBuffer = new List<long>();
        private readonly int capacity;
        private long nextSequence = 1;
        public long LastFinalizedDay { get; private set; } = -1;
        public DemandDayData Current { get; private set; }
        public DemandDayData Pending { get; private set; }
        public TransitDemandObserver(int capacity, long initialDay = 0)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            Current = new DemandDayData { day = initialDay };
        }
        private static (int, TripPurpose) Key(int resident, TripPurpose purpose) => (resident, purpose);
        private DemandDayData Day(long day) => Current.day == day ? Current : Pending != null && Pending.day == day ? Pending : null;
        public long ObserveAttempt(EntityId resident, TripPurpose purpose, long episodeStartTick, long tick)
        {
            resident.EnsureValid();
            if (purpose == TripPurpose.MoveOut) return 0;
            if (purpose < TripPurpose.Home || purpose > TripPurpose.Hygiene)
            { Current.invalid = true; return 0; }
            if (purpose < TripPurpose.Home || purpose > TripPurpose.Hygiene) { Current.invalid = true; return 0; }
            var key = Key(resident.Value, purpose);
            if (latest.TryGetValue(key, out var prior) && (prior.episodeStartTick >= episodeStartTick || !prior.terminal)) return prior.sequence;
            if (tick < 0 || tick / 1440 != Current.day || episodeStartTick > tick || episodeStartTick < 0 || latest.Count >= capacity && prior == null || unresolved.Count >= capacity)
            { Current.invalid = true; return 0; }
            try
            {
                var item = new DemandEpisodeData { residentId = resident.Value, purpose = purpose,
                    episodeStartTick = episodeStartTick, submissionTick = tick, day = Current.day, sequence = nextSequence,
                    unreachable = prior != null && prior.unreachable };
                nextSequence = checked(nextSequence + 1);
                Current.attempted = checked(Current.attempted + 1);
                latest[key] = item; unresolved.Add(item.sequence, item); return item.sequence;
            }
            catch (OverflowException) { Current.invalid = true; return 0; }
        }
        public bool HasOutstandingTrip(EntityId resident, TripPurpose purpose)
        {
            return latest.TryGetValue(Key(resident.Value, purpose), out var item) &&
                !item.terminal && item.tripId > 0;
        }
        public long FindSequenceForTrip(long tripId)
        {
            if (tripId <= 0) return 0;
            return tripSequences.TryGetValue(tripId, out long sequence) ? sequence : 0;
        }
        public void MarkReachable(long sequence)
        {
            foreach (var item in latest.Values)
                if (item.sequence == sequence && item.unreachable)
                {
                    item.unreachable = false;
                    Current.unreachable = Math.Max(0, Current.unreachable - 1);
                    if (Pending != null && item.day == Pending.day) Pending.unreachable = Math.Max(0, Pending.unreachable - 1);
                    return;
                }
        }
        public void RejectUnreachable(long sequence, long tick)
        {
            foreach (var item in latest.Values)
                if (item.sequence == sequence && !item.unreachable)
                {
                    item.unreachable = true;
                    Current.unreachable++;
                    break;
                }
            Reject(sequence, tick);
        }
        public void AcceptTrip(long sequence, long tripId)
        {
            MarkReachable(sequence);
            if (!unresolved.TryGetValue(sequence, out var item)) return;
            if (tripId <= 0 || (tripSequences.TryGetValue(tripId, out long existing) && existing != sequence))
            { Day(item.day).invalid=true; return; }
            if (item.tripId > 0) tripSequences.Remove(item.tripId);
            item.tripId=tripId; tripSequences[tripId]=sequence;
        }
        public void ObserveLiveWait(int wait)
        {
            if (wait < 0) Current.invalid = true;
            else Current.liveMaximum = Math.Max(Current.liveMaximum, wait);
        }
        public void ObserveQueueWait(long sequence, int totalWait)
        {
            if (!unresolved.TryGetValue(sequence, out var item)) return;
            var day = Day(item.day);
            if (totalWait < 0) { day.invalid = true; return; }
            item.queueWait = Math.Max(item.queueWait, totalWait);
            day.liveMaximum = Math.Max(day.liveMaximum, totalWait);
        }
        public void Complete(long sequence, long tick)
        {
            MarkReachable(sequence);
            Finish(sequence, tick, true);
        }
        public void Reject(long sequence, long tick) => Finish(sequence, tick, false);
        private void Finish(long sequence, long tick, bool success)
        {
            if (!unresolved.TryGetValue(sequence, out var item)) return;
            var day = Day(item.day);
            if (tick < item.submissionTick) { day.invalid = true; return; }
            try
            {
                if (success && tick - item.submissionTick <= 360) day.succeeded = checked(day.succeeded + 1);
                else day.failed = checked(day.failed + 1);
                if (success || item.tripId > 0)
                { int bin = Math.Min(361, item.queueWait); day.waitHistogram[bin] = checked(day.waitHistogram[bin] + 1); }
            }
            catch (OverflowException) { day.invalid = true; }
            item.terminal = true; unresolved.Remove(sequence);
            if (item.tripId > 0) tripSequences.Remove(item.tripId);
        }
        public void MarkObservationComplete() => Current.complete = true;
        /// <summary>Rotate before new-day attempts; never expire here (terminal events get inclusive deadline first).</summary>
        public void BeginTick(long tick)
        {
            if (tick < 0 || tick / 1440 < Current.day) { Current.invalid=true; return; }
            if (tick / 1440 == Current.day) return;
            if (Pending != null || tick / 1440 != Current.day + 1) { Current.invalid=true; return; }
            Pending=Current;
            Current=new DemandDayData { day=tick/1440 };
            foreach (var item in latest.Values) if (item.unreachable) Current.unreachable++;
        }
        public DemandDayData Advance(long tick)
        {
            if (tick < 0 || tick / 1440 < Current.day) { Current.invalid = true; return null; }
            expiryBuffer.Clear();
            foreach (var item in unresolved.Values) if (tick - item.submissionTick >= 360) expiryBuffer.Add(item.sequence);
            foreach (long sequence in expiryBuffer) Reject(sequence, tick);
            DemandDayData finalized = null;
            if (Pending != null && Pending.attempted == Pending.succeeded + Pending.failed)
            { finalized = Pending; LastFinalizedDay = Pending.day; Pending = null; }
            if (tick / 1440 > Current.day)
            {
                if (Pending != null || tick / 1440 != Current.day + 1) { Current.invalid = true; return finalized; }
                Pending = Current; Current = new DemandDayData { day = tick / 1440 };
                foreach (var item in latest.Values) if (item.unreachable) Current.unreachable++;
                if (Pending.attempted == Pending.succeeded + Pending.failed && finalized == null)
                { finalized = Pending; LastFinalizedDay = Pending.day; Pending = null; }
            }
            return finalized;
        }
        public TransitDemandSaveData ToSaveData()
        {
            var items = new Dictionary<long, DemandEpisodeData>();
            foreach (var item in latest.Values) items[item.sequence] = item;
            foreach (var item in unresolved.Values) items[item.sequence] = item;
            var array = new DemandEpisodeData[items.Count]; items.Values.CopyTo(array, 0);
            Array.Sort(array, (a, b) => a.sequence.CompareTo(b.sequence));
            return new TransitDemandSaveData { capacity = capacity, nextSequence = nextSequence,
                lastFinalizedDay = LastFinalizedDay, hasPending = Pending != null, current = Clone(Current), pending = Clone(Pending), episodes = Array.ConvertAll(array, Clone) };
        }
        private static DemandEpisodeData Clone(DemandEpisodeData x) => new DemandEpisodeData { residentId=x.residentId, purpose=x.purpose,
            episodeStartTick=x.episodeStartTick, sequence=x.sequence, submissionTick=x.submissionTick, day=x.day, tripId=x.tripId, queueWait=x.queueWait, terminal=x.terminal, unreachable=x.unreachable };
        private static DemandDayData Clone(DemandDayData x) => x == null ? null : new DemandDayData { day=x.day, attempted=x.attempted,
            succeeded=x.succeeded, failed=x.failed, unreachable=x.unreachable, liveMaximum=x.liveMaximum, complete=x.complete, invalid=x.invalid, waitHistogram=(long[])x.waitHistogram.Clone() };
        private static bool ValidDay(DemandDayData day)
        {
            if (day.day < 0 || day.attempted < 0 || day.succeeded < 0 || day.unreachable < 0 || day.failed < 0 || day.liveMaximum < 0 || day.succeeded > day.attempted || day.failed > day.attempted - day.succeeded) return false;
            long count = 0;
            foreach (long n in day.waitHistogram)
            { if (n < 0 || n > day.attempted - count) return false; count += n; }
            return true;
        }
        public static TransitDemandObserver FromSaveData(TransitDemandSaveData data)
        {
            // Unity inline serialization materializes null records; presence is explicit.
            var pending = data != null && data.hasPending ? data.pending : null;
            if (data == null || data.hasPending && pending == null || data.version != 1 || data.current == null || data.episodes == null || data.current.waitHistogram == null || data.current.waitHistogram.Length != 362 || (pending != null && (pending.waitHistogram == null || pending.waitHistogram.Length != 362))) throw new ArgumentException("Invalid demand observation save.");
            if (data.capacity <= 0 || data.nextSequence <= 0 || data.lastFinalizedDay < -1 ||
                !ValidDay(data.current) || data.lastFinalizedDay >= data.current.day ||
                (pending != null && (!ValidDay(pending) || pending.day != data.current.day - 1 || pending.day <= data.lastFinalizedDay)))
                throw new ArgumentException("Invalid demand observation counters.");
            var result = new TransitDemandObserver(data.capacity, data.current.day) { Current=Clone(data.current), Pending=Clone(pending),
                nextSequence=data.nextSequence, LastFinalizedDay=data.lastFinalizedDay };
            var seenSequences = new HashSet<long>();
            var seenTrips = new HashSet<long>();
            foreach (var saved in data.episodes)
            {
                if (saved == null || saved.sequence <= 0 || saved.sequence >= data.nextSequence || !seenSequences.Add(saved.sequence) ||
                    saved.residentId <= 0 || saved.submissionTick < 0 || saved.episodeStartTick < 0 || saved.episodeStartTick > saved.submissionTick ||
                    saved.queueWait < 0 || saved.day != saved.submissionTick / 1440 || saved.tripId < 0 ||
                    (saved.tripId > 0 && !seenTrips.Add(saved.tripId)) || saved.purpose < TripPurpose.Home || saved.purpose > TripPurpose.Hygiene ||
                    (!saved.terminal && result.Day(saved.day) == null))
                    throw new ArgumentException("Invalid demand episode identity.");
                var item = Clone(saved); var key = Key(item.residentId,item.purpose);
                if (!result.latest.TryGetValue(key,out var prior) || prior.sequence < item.sequence) result.latest[key]=item;
                if (!item.terminal)
                {
                    result.unresolved.Add(item.sequence,item);
                    if (item.tripId > 0) result.tripSequences.Add(item.tripId,item.sequence);
                }
            }
            if (result.latest.Count > result.capacity || result.unresolved.Count > result.capacity)
                throw new ArgumentException("Demand observation capacity exceeded.");
            long currentUnresolved=0, pendingUnresolved=0;
            foreach (var item in result.unresolved.Values)
            { if (item.day == result.Current.day) currentUnresolved++; else pendingUnresolved++; }
            if (currentUnresolved != result.Current.attempted - result.Current.succeeded - result.Current.failed ||
                (result.Pending != null && pendingUnresolved != result.Pending.attempted - result.Pending.succeeded - result.Pending.failed))
                throw new ArgumentException("Demand observation pending denominator mismatch.");
            long blocked = 0;
            foreach (var item in result.latest.Values) if (item.unreachable) blocked++;
            if (blocked != result.Current.unreachable)
                throw new ArgumentException("Demand observation unreachable denominator mismatch.");
            return result;
        }
    }
}
