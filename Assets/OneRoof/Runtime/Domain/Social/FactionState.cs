using System;
using System.Collections.Generic;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Social
{
    public static class FactionIds
    {
        public const string TenantUnion = "tenant_union";
        public const string CorporateCoalition = "corporate_coalition";
        public const string MerchantGuild = "merchant_guild";
        public const string CivicEcoCouncil = "civic_eco_council";
        public static readonly string[] All = { TenantUnion, CorporateCoalition, MerchantGuild, CivicEcoCouncil };
    }

    /// <summary>One resident's attributable, daily-updated support. Membership requires sustained support.</summary>
    public sealed class FactionSupport
    {
        internal FactionSupport(EntityId residentId, string factionId, float support, int sustainedDays, string driver, int homeFloor)
        { ResidentId = residentId; FactionId = factionId; Support = support; SustainedDays = sustainedDays; Driver = driver; HomeFloor = homeFloor; }
        public EntityId ResidentId { get; }
        public string FactionId { get; }
        public float Support { get; internal set; }
        public int SustainedDays { get; internal set; }
        public string Driver { get; internal set; }
        public int HomeFloor { get; internal set; }
        public bool IsMember => SustainedDays >= 2 && Support >= .55f;
    }

    /// <summary>Bounded, attributable resident connection. Affinity is in [-1, 1].</summary>
    public sealed class RelationshipEdge
    {
        internal RelationshipEdge(EntityId first, EntityId second, float affinity, long lastContactTick, string cause)
        { First = first; Second = second; Affinity = affinity; LastContactTick = lastContactTick; Cause = cause; }
        public EntityId First { get; }
        public EntityId Second { get; }
        public float Affinity { get; internal set; }
        public long LastContactTick { get; internal set; }
        public string Cause { get; internal set; }
    }

    public sealed class FactionRecord
    {
        internal FactionRecord(string id) { Id = id; }
        public string Id { get; }
        public float Pressure { get; internal set; }
        public float PreviousPressure { get; internal set; }
        public string TopGrievance { get; internal set; } = "none";
        public int MemberCount { get; internal set; }
        public int SupporterCount { get; internal set; }
    }

    /// <summary>Authoritative daily social state. All iteration and tie breaks use resident ID order.</summary>
    public sealed class FactionState
    {
        public const int MaxEdgesPerResident = 6;
        public const long EdgeExpiryTicks = 3 * DailySchedule.TicksPerDay;
        private readonly List<RelationshipEdge> _edges = new List<RelationshipEdge>();
        private readonly List<FactionSupport> _supports = new List<FactionSupport>();
        private readonly List<FactionRecord> _factions = new List<FactionRecord>();
        private readonly Dictionary<int, int> _degree = new Dictionary<int, int>();
        public FactionState() { foreach (var id in FactionIds.All) _factions.Add(new FactionRecord(id)); }
        public IReadOnlyList<RelationshipEdge> Edges => _edges;
        public IReadOnlyList<FactionSupport> Supports => _supports;
        public IReadOnlyList<FactionRecord> Factions => _factions;
        public long LastEvaluationTick { get; private set; }

        public void Evaluate(PopulationState population, BuildingTopologyState topology, BusinessState businesses, PolicyDecreeState policy, long tick)
        {
            if (population == null || topology == null) throw new ArgumentNullException(population == null ? nameof(population) : nameof(topology));
            var people = new List<PersonRecord>(population.Persons);
            people.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            var live = new HashSet<int>();
            foreach (var person in people) live.Add(person.Id.Value);
            var affiliations = new Dictionary<int, FactionSupport>();
            foreach (var support in _supports)
                if (support.IsMember && (!affiliations.TryGetValue(support.ResidentId.Value, out var strongest) ||
                    support.Support > strongest.Support || support.Support == strongest.Support && string.CompareOrdinal(support.FactionId, strongest.FactionId) < 0))
                    affiliations[support.ResidentId.Value] = support;
            _edges.RemoveAll(e => !live.Contains(e.First.Value) || !live.Contains(e.Second.Value) || tick - e.LastContactTick > EdgeExpiryTicks);
            _degree.Clear();
            foreach (var edge in _edges) { Increment(edge.First.Value); Increment(edge.Second.Value); }
            // Each bucket remembers at most the six lowest IDs. This is linear in population size.
            var home = new Dictionary<int, List<PersonRecord>>();
            var work = new Dictionary<int, List<PersonRecord>>();
            var transit = new Dictionary<int, List<PersonRecord>>();
            foreach (var person in people)
            {
                AddBucket(home, person.HomeRoomId.Value, person);
                if (person.WorkplaceRoomId.IsValid) AddBucket(work, person.WorkplaceRoomId.Value, person);
                if (person.CurrentActivity == ActivityKind.Commuting && person.CurrentRoomId.IsValid && !person.CurrentLocation.IsOutside) AddBucket(transit, person.CurrentRoomId.Value, person);
            }
            ConnectBuckets(home, tick, "nearby home", affiliations);
            ConnectBuckets(work, tick, "shared workplace", affiliations);
            ConnectBuckets(transit, tick, "transit co-presence", affiliations);
            _edges.Sort((a, b) => a.First.Value != b.First.Value ? a.First.Value.CompareTo(b.First.Value) : a.Second.Value.CompareTo(b.Second.Value));

            var prior = new Dictionary<string, FactionSupport>();
            foreach (var support in _supports) prior[Key(support.ResidentId.Value, support.FactionId)] = support;
            _supports.Clear();
            var pressureSum = new float[4];
            var counts = new int[4];
            var driverCounts = new Dictionary<string, int>[4];
            for (var i = 0; i < 4; i++) driverCounts[i] = new Dictionary<string, int>();
            foreach (var person in people)
            {
                population.TryGetHousehold(person.HouseholdId, out var household);
                topology.TryGetRoom(person.HomeRoomId, out var homeRoom);
                topology.TryGetRoom(person.WorkplaceRoomId, out var workRoom);
                var floor = homeRoom == null ? 0 : homeRoom.Floor;
                var isOffice = workRoom != null && workRoom.ContentType.Value.StartsWith("commercial:office", StringComparison.Ordinal);
                var isMerchant = workRoom != null && (workRoom.ContentType.Value.StartsWith("commercial:", StringComparison.Ordinal) || workRoom.ContentType.Value.StartsWith("service:", StringComparison.Ordinal)) && !isOffice;
                var rent = Math.Max(0f, 1f - person.Wellbeing.RentBurden);
                var commute = Math.Max(0f, 1f - person.Wellbeing.Commute);
                var noise = Math.Max(0f, 1f - person.Wellbeing.Noise);
                var service = Math.Max(0f, 1f - person.Wellbeing.ServiceAccess);
                var arrears = household != null && household.ArrearsDays > 0 ? Math.Min(1f, household.ArrearsDays / 3f) : 0f;
                var strain = person.Wellbeing.Strain;
                var businessStress = 0f;
                if (businesses != null && person.WorkplaceRoomId.IsValid)
                    foreach (var business in businesses.Businesses)
                        if (business.RoomId == person.WorkplaceRoomId) { businessStress = business.IsInsolvent ? 1f : business.CashBalance < 0 ? .7f : 0f; break; }
                var scores = new[] {
                    Math.Min(1f, .55f * rent + .35f * arrears + .1f * commute),
                    isOffice ? Math.Min(1f, .5f * service + .3f * commute + .2f * businessStress) : 0f,
                    isMerchant ? Math.Min(1f, .45f * businessStress + .3f * (policy.CommercialTaxRate / PolicyDecreeState.HighCommercialTaxRate) + .25f * noise) : 0f,
                    Math.Min(1f, .4f * service + .35f * noise + .25f * strain)
                };
                var drivers = new[] {
                    MaxDriver(rent, "rent burden", arrears, "arrears", commute, "commute"),
                    MaxDriver(service, "service reliability", commute, "commute", businessStress, "business continuity"),
                    MaxDriver(businessStress, "business margin", policy.CommercialTaxRate / PolicyDecreeState.HighCommercialTaxRate, "commercial tax", noise, "noise"),
                    MaxDriver(service, "shared services", noise, "noise", strain, "living strain")
                };
                for (var i = 0; i < 4; i++)
                {
                    var id = FactionIds.All[i];
                    prior.TryGetValue(Key(person.Id.Value, id), out var old);
                    var score = scores[i];
                    var next = old == null ? score : .6f * old.Support + .4f * score;
                    var days = next >= .55f ? Math.Min(10000, (old?.SustainedDays ?? 0) + 1) : 0;
                    if (next < .15f) continue;
                    var support = new FactionSupport(person.Id, id, next, days, drivers[i], floor);
                    _supports.Add(support);
                    pressureSum[i] += next;
                    counts[i]++;
                    if (!driverCounts[i].ContainsKey(drivers[i])) driverCounts[i][drivers[i]] = 0;
                    driverCounts[i][drivers[i]]++;
                }
            }
            for (var i = 0; i < 4; i++)
            {
                var record = _factions[i];
                record.PreviousPressure = record.Pressure;
                record.Pressure = people.Count == 0 ? 0f : pressureSum[i] / people.Count;
                record.SupporterCount = counts[i];
                record.MemberCount = 0;
                foreach (var support in _supports) if (support.FactionId == record.Id && support.IsMember) record.MemberCount++;
                record.TopGrievance = "none";
                var best = 0;
                foreach (var pair in driverCounts[i]) if (pair.Value > best || pair.Value == best && string.CompareOrdinal(pair.Key, record.TopGrievance) < 0) { best = pair.Value; record.TopGrievance = pair.Key; }
            }
            LastEvaluationTick = tick;
        }

        private static string MaxDriver(float a, string aa, float b, string bb, float c, string cc)
        { if (a >= b && a >= c) return aa; return b >= c ? bb : cc; }
        private static string Key(int person, string faction) => person + ":" + faction;
        private void Increment(int id) { _degree.TryGetValue(id, out var n); _degree[id] = n + 1; }
        private static void AddBucket(Dictionary<int, List<PersonRecord>> buckets, int key, PersonRecord person)
        {
            if (!buckets.TryGetValue(key, out var list)) { list = new List<PersonRecord>(MaxEdgesPerResident + 1); buckets[key] = list; }
            if (list.Count <= MaxEdgesPerResident) list.Add(person);
        }
        private void ConnectBuckets(Dictionary<int, List<PersonRecord>> buckets, long tick, string cause, Dictionary<int, FactionSupport> affiliations)
        {
            var keys = new List<int>(buckets.Keys); keys.Sort();
            foreach (var key in keys)
            {
                var list = buckets[key];
                for (var i = 0; i < list.Count; i++)
                    for (var j = i + 1; j < list.Count; j++) Connect(list[i].Id, list[j].Id, tick, cause, affiliations);
            }
        }
        private void Connect(EntityId a, EntityId b, long tick, string cause, Dictionary<int, FactionSupport> affiliations)
        {
            if (a == b) return;
            if (a.Value > b.Value) { var tmp = a; a = b; b = tmp; }
            affiliations.TryGetValue(a.Value, out var first);
            affiliations.TryGetValue(b.Value, out var second);
            var opposed = first != null && second != null &&
                (first.FactionId == FactionIds.TenantUnion && second.FactionId == FactionIds.CorporateCoalition ||
                 second.FactionId == FactionIds.TenantUnion && first.FactionId == FactionIds.CorporateCoalition ||
                 first.FactionId == FactionIds.MerchantGuild && second.FactionId == FactionIds.CivicEcoCouncil ||
                 second.FactionId == FactionIds.MerchantGuild && first.FactionId == FactionIds.CivicEcoCouncil);
            var contactCause = opposed ? "conflicting faction priorities during " + cause : cause;
            foreach (var edge in _edges)
                if (edge.First == a && edge.Second == b)
                {
                    edge.Affinity = Math.Max(-1f, Math.Min(1f, edge.Affinity + (opposed ? -.1f : .05f)));
                    edge.LastContactTick = tick;
                    edge.Cause = contactCause;
                    return;
                }
            _degree.TryGetValue(a.Value, out var da); _degree.TryGetValue(b.Value, out var db);
            if (da >= MaxEdgesPerResident || db >= MaxEdgesPerResident) return;
            _edges.Add(new RelationshipEdge(a, b, opposed ? -.1f : .1f, tick, contactCause)); Increment(a.Value); Increment(b.Value);
        }

        public FactionSaveData ToSaveData()
        {
            var data = new FactionSaveData { version = 1, lastEvaluationTick = LastEvaluationTick, edges = new RelationshipEdgeSaveData[_edges.Count], supports = new FactionSupportSaveData[_supports.Count], factions = new FactionRecordSaveData[_factions.Count] };
            for (var i = 0; i < _edges.Count; i++) { var e = _edges[i]; data.edges[i] = new RelationshipEdgeSaveData { first = e.First.Value, second = e.Second.Value, affinity = e.Affinity, lastContactTick = e.LastContactTick, cause = e.Cause }; }
            for (var i = 0; i < _supports.Count; i++) { var s = _supports[i]; data.supports[i] = new FactionSupportSaveData { residentId = s.ResidentId.Value, factionId = s.FactionId, support = s.Support, sustainedDays = s.SustainedDays, driver = s.Driver, homeFloor = s.HomeFloor }; }
            for (var i = 0; i < _factions.Count; i++) { var f = _factions[i]; data.factions[i] = new FactionRecordSaveData { id = f.Id, pressure = f.Pressure, previousPressure = f.PreviousPressure, topGrievance = f.TopGrievance }; }
            return data;
        }
        public static FactionState FromSaveData(FactionSaveData data, PopulationState population)
        {
            var state = new FactionState();
            if (data == null || data.version <= 0) return state;
            var live = new HashSet<int>(); foreach (var person in population.Persons) live.Add(person.Id.Value);
            if (data.edges != null) foreach (var e in data.edges)
                if (e != null && e.first > 0 && e.second > e.first && live.Contains(e.first) && live.Contains(e.second))
                    state._edges.Add(new RelationshipEdge(new EntityId(e.first), new EntityId(e.second), Math.Max(-1f, Math.Min(1f, e.affinity)), Math.Max(0, e.lastContactTick), e.cause ?? "contact"));
            state._edges.Sort((a, b) => a.First.Value != b.First.Value ? a.First.Value.CompareTo(b.First.Value) : a.Second.Value.CompareTo(b.Second.Value));
            state._degree.Clear();
            for (var i = state._edges.Count - 1; i >= 0; i--)
            {
                var e = state._edges[i]; state._degree.TryGetValue(e.First.Value, out var a); state._degree.TryGetValue(e.Second.Value, out var b);
                if (a >= MaxEdgesPerResident || b >= MaxEdgesPerResident) { state._edges.RemoveAt(i); continue; }
                state.Increment(e.First.Value); state.Increment(e.Second.Value);
            }
            if (data.supports != null) foreach (var s in data.supports)
                if (s != null && live.Contains(s.residentId) && Array.IndexOf(FactionIds.All, s.factionId) >= 0)
                    state._supports.Add(new FactionSupport(new EntityId(s.residentId), s.factionId, Math.Max(0f, Math.Min(1f, s.support)), Math.Max(0, s.sustainedDays), s.driver ?? "unavailable", s.homeFloor));
            state._supports.Sort((a, b) => a.ResidentId.Value != b.ResidentId.Value ? a.ResidentId.Value.CompareTo(b.ResidentId.Value) : string.CompareOrdinal(a.FactionId, b.FactionId));
            if (data.factions != null) foreach (var saved in data.factions)
                if (saved != null) foreach (var faction in state._factions) if (faction.Id == saved.id)
                    { faction.Pressure = Math.Max(0f, Math.Min(1f, saved.pressure)); faction.PreviousPressure = Math.Max(0f, Math.Min(1f, saved.previousPressure)); faction.TopGrievance = saved.topGrievance ?? "none"; }
            foreach (var faction in state._factions) foreach (var support in state._supports) if (support.FactionId == faction.Id) { faction.SupporterCount++; if (support.IsMember) faction.MemberCount++; }
            state.LastEvaluationTick = Math.Max(0, data.lastEvaluationTick);
            return state;
        }
    }
}
