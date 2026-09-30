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
        internal RelationshipEdge(EntityId first, EntityId second, float affinity, long lastContactTick, string cause, long lastMeaningfulTick = 0, int sharedSupportDays = 0, string sharedIssue = null, RelationshipStage stage = RelationshipStage.Stranger)
        {
            First = first; Second = second; Affinity = affinity; LastContactTick = lastContactTick; Cause = cause;
            LastMeaningfulTick = lastMeaningfulTick; SharedSupportDays = sharedSupportDays; SharedIssue = sharedIssue;
            Stage = stage == RelationshipStage.Stranger ? RelationshipMilestones.DeriveStage(affinity) : stage;
        }
        public EntityId First { get; }
        public EntityId Second { get; }
        public float Affinity { get; set; }
        public float PreviousAffinity { get; set; }
        public RelationshipStage Stage { get; set; }
        public long LastContactTick { get; set; }
        public string Cause { get; set; }
        public long LastMeaningfulTick { get; set; }
        public int SharedSupportDays { get; set; }
        public string SharedIssue { get; set; }
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
            var byId = new Dictionary<int, PersonRecord>();
            foreach (var person in people) byId[person.Id.Value] = person;
            _edges.RemoveAll(e => !live.Contains(e.First.Value) || !live.Contains(e.Second.Value) || tick - e.LastContactTick > EdgeExpiryTicks);
            foreach (var edge in _edges)
            {
                edge.PreviousAffinity = edge.Affinity;
                if (edge.Affinity > 0f) edge.Affinity = Math.Max(0f, edge.Affinity - .01f);
                else if (edge.Affinity < 0f)
                {
                    var isGrudge = HasGrudge(byId, edge.First, edge.Second);
                    var decay = isGrudge ? .002f : .01f;
                    edge.Affinity = Math.Min(0f, edge.Affinity + decay);
                }
                edge.Stage = RelationshipMilestones.DeriveStage(edge.Affinity, edge.Stage);
            }
            _degree.Clear();
            foreach (var edge in _edges) { Increment(edge.First.Value); Increment(edge.Second.Value); }
            var home = new Dictionary<int, List<PersonRecord>>();
            var work = new Dictionary<int, List<PersonRecord>>();
            var transit = new Dictionary<int, List<PersonRecord>>();
            foreach (var person in people)
            {
                AddBucket(home, person.HomeRoomId.Value, person, tick);
                if (person.WorkplaceRoomId.IsValid) AddBucket(work, person.WorkplaceRoomId.Value, person, tick);
                if (person.CurrentActivity == ActivityKind.Commuting && person.CurrentRoomId.IsValid && !person.CurrentLocation.IsOutside) AddBucket(transit, person.CurrentRoomId.Value, person, tick);
            }
            ConnectBuckets(home, tick, "nearby home", byId);
            ConnectBuckets(work, tick, "shared workplace", byId);
            ConnectBuckets(transit, tick, "transit co-presence", byId);
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
        private static void AddBucket(Dictionary<int, List<PersonRecord>> buckets, int key, PersonRecord person, long tick)
        {
            if (!buckets.TryGetValue(key, out var list)) { list = new List<PersonRecord>(MaxEdgesPerResident + 1); buckets[key] = list; }
            if (list.Count < MaxEdgesPerResident + 1) { list.Add(person); return; }
            var worst = 0;
            for (var i = 1; i < list.Count; i++)
                if (CompareContact(list[i], list[worst], key, tick) > 0) worst = i;
            if (CompareContact(person, list[worst], key, tick) < 0) list[worst] = person;
        }
        private void ConnectBuckets(Dictionary<int, List<PersonRecord>> buckets, long tick, string cause, Dictionary<int, PersonRecord> byId)
        {
            var keys = new List<int>(buckets.Keys); keys.Sort();
            foreach (var key in keys)
            {
                var list = buckets[key];
                // A stable day/key hash samples crowded contexts without preferring low resident IDs.
                list.Sort((a, b) => CompareContact(a, b, key, tick));
                for (var i = 0; i < list.Count; i++)
                    for (var j = i + 1; j < list.Count; j++) Connect(list[i].Id, list[j].Id, tick, cause, byId);
            }
        }
        private static int CompareContact(PersonRecord a, PersonRecord b, int key, long tick)
        {
            var order = ContactOrder(a.Id.Value, key, tick).CompareTo(ContactOrder(b.Id.Value, key, tick));
            return order != 0 ? order : a.Id.Value.CompareTo(b.Id.Value);
        }
        private static uint ContactOrder(int id, int key, long tick)
        {
            unchecked
            {
                var value = (uint)id * 2654435761u ^ (uint)key * 2246822519u ^ (uint)(tick / DailySchedule.TicksPerDay) * 3266489917u;
                value ^= value >> 16; value *= 2246822519u; value ^= value >> 13;
                return value;
            }
        }
        private void Connect(EntityId a, EntityId b, long tick, string context, Dictionary<int, PersonRecord> byId)
        {
            if (a == b) return;
            if (a.Value > b.Value) { var tmp = a; a = b; b = tmp; }
            var shared = SharedGrievance(byId[a.Value], byId[b.Value]);
            foreach (var edge in _edges)
                if (edge.First == a && edge.Second == b)
                {
                    if (edge.LastContactTick == tick) return;
                    var consecutiveContact = edge.LastContactTick == LastEvaluationTick && LastEvaluationTick > 0;
                    edge.LastContactTick = tick;
                    if (shared != null)
                    {
                        edge.SharedSupportDays = consecutiveContact && string.Equals(edge.SharedIssue, shared, StringComparison.Ordinal)
                            ? Math.Min(10000, edge.SharedSupportDays + 1) : 1;
                        edge.SharedIssue = shared;
                        if (edge.SharedSupportDays >= 2)
                        {
                            edge.Affinity = Math.Min(1f, edge.Affinity + .04f);
                            edge.LastMeaningfulTick = tick;
                            edge.Cause = "shared grievance: " + shared;
                            edge.Stage = RelationshipMilestones.DeriveStage(edge.Affinity, edge.Stage);
                        }
                    }
                    else { edge.SharedSupportDays = 0; edge.SharedIssue = null; }
                    return;
                }
            _degree.TryGetValue(a.Value, out var da); _degree.TryGetValue(b.Value, out var db);
            if (da >= MaxEdgesPerResident || db >= MaxEdgesPerResident) return;
            _edges.Add(new RelationshipEdge(a, b, 0f, tick, "encounter: " + context, 0, shared == null ? 0 : 1, shared, RelationshipStage.Acquaintance)); Increment(a.Value); Increment(b.Value);
        }

        public RelationshipEdge GetEdge(EntityId a, EntityId b)
        {
            if (a.Value > b.Value) { var tmp = a; a = b; b = tmp; }
            for (var i = 0; i < _edges.Count; i++)
                if (_edges[i].First == a && _edges[i].Second == b) return _edges[i];
            return null;
        }

        public RelationshipEdge GetOrCreateEdge(EntityId a, EntityId b, long tick, string cause, RelationshipStage initialStage = RelationshipStage.Acquaintance)
        {
            if (a == b) return null;
            if (a.Value > b.Value) { var tmp = a; a = b; b = tmp; }
            for (var i = 0; i < _edges.Count; i++)
            {
                var e = _edges[i];
                if (e.First == a && e.Second == b)
                {
                    e.LastContactTick = tick;
                    return e;
                }
            }
            _degree.TryGetValue(a.Value, out var da);
            _degree.TryGetValue(b.Value, out var db);
            if (da >= MaxEdgesPerResident || db >= MaxEdgesPerResident) return null;

            var edge = new RelationshipEdge(a, b, 0f, tick, cause, 0, 0, null, initialStage);
            _edges.Add(edge);
            Increment(a.Value);
            Increment(b.Value);
            return edge;
        }

        private static bool HasGrudge(Dictionary<int, PersonRecord> byId, EntityId a, EntityId b)
        {
            if (byId.TryGetValue(a.Value, out var pa) && pa.SocialTraits != null)
                for (var i = 0; i < pa.SocialTraits.Count; i++)
                    if (pa.SocialTraits[i].Kind == SocialTraitKind.GrudgeHolder) return true;
            if (byId.TryGetValue(b.Value, out var pb) && pb.SocialTraits != null)
                for (var i = 0; i < pb.SocialTraits.Count; i++)
                    if (pb.SocialTraits[i].Kind == SocialTraitKind.GrudgeHolder) return true;
            return false;
        }

        private static string SharedGrievance(PersonRecord first, PersonRecord second)
        {
            foreach (var grievance in first.Wellbeing.Grievances)
                foreach (var other in second.Wellbeing.Grievances)
                    if (string.Equals(grievance, other, StringComparison.Ordinal)) return grievance;
            return null;
        }

        public FactionSaveData ToSaveData()
        {
            var data = new FactionSaveData { version = 3, lastEvaluationTick = LastEvaluationTick, edges = new RelationshipEdgeSaveData[_edges.Count], supports = new FactionSupportSaveData[_supports.Count], factions = new FactionRecordSaveData[_factions.Count] };
            for (var i = 0; i < _edges.Count; i++) { var e = _edges[i]; data.edges[i] = new RelationshipEdgeSaveData { first = e.First.Value, second = e.Second.Value, affinity = e.Affinity, previousAffinity = e.PreviousAffinity, lastContactTick = e.LastContactTick, cause = e.Cause, lastMeaningfulTick = e.LastMeaningfulTick, sharedSupportDays = e.SharedSupportDays, sharedIssue = e.SharedIssue, stage = (int)e.Stage }; }
            Array.Sort(data.edges, (a, b) => a.first != b.first ? a.first.CompareTo(b.first) : a.second.CompareTo(b.second));
            for (var i = 0; i < _supports.Count; i++) { var s = _supports[i]; data.supports[i] = new FactionSupportSaveData { residentId = s.ResidentId.Value, factionId = s.FactionId, support = s.Support, sustainedDays = s.SustainedDays, driver = s.Driver, homeFloor = s.HomeFloor }; }
            for (var i = 0; i < _factions.Count; i++) { var f = _factions[i]; data.factions[i] = new FactionRecordSaveData { id = f.Id, pressure = f.Pressure, previousPressure = f.PreviousPressure, topGrievance = f.TopGrievance }; }
            return data;
        }
        public static FactionState FromSaveData(FactionSaveData data, PopulationState population)
        {
            var state = new FactionState();
            if (data == null || data.version <= 0) return state;
            var live = new HashSet<int>(); foreach (var person in population.Persons) live.Add(person.Id.Value);
            var seenEdges = new HashSet<long>();
            if (data.edges != null) foreach (var e in data.edges)
                if (e != null && e.first > 0 && e.second > e.first && live.Contains(e.first) && live.Contains(e.second))
                {
                    var pair = ((long)e.first << 32) | (uint)e.second;
                    if (!seenEdges.Add(pair)) continue;
                    var contactTick = Math.Max(0, e.lastContactTick);
                    var edgeAffinity = ClampFinite(e.affinity, -1f, 1f);
                    var stage = data.version >= 3 && Enum.IsDefined(typeof(RelationshipStage), e.stage)
                        ? (RelationshipStage)e.stage
                        : RelationshipMilestones.DeriveStage(edgeAffinity);
                    var edge = new RelationshipEdge(new EntityId(e.first), new EntityId(e.second), edgeAffinity, contactTick, data.version >= 2 ? e.cause ?? "encounter" : "legacy tie (cause unavailable)", data.version >= 2 ? Math.Max(0, Math.Min(contactTick, e.lastMeaningfulTick)) : 0, data.version >= 2 && !string.IsNullOrEmpty(e.sharedIssue) ? Math.Max(0, Math.Min(10000, e.sharedSupportDays)) : 0, data.version >= 2 ? e.sharedIssue : null, stage);
                    edge.PreviousAffinity = data.version >= 2 ? ClampFinite(e.previousAffinity, -1f, 1f) : edge.Affinity;
                    state._edges.Add(edge);
                }
            state._edges.Sort((a, b) => a.First.Value != b.First.Value ? a.First.Value.CompareTo(b.First.Value) : a.Second.Value.CompareTo(b.Second.Value));
            state._degree.Clear();
            for (var i = 0; i < state._edges.Count; i++)
            {
                var e = state._edges[i]; state._degree.TryGetValue(e.First.Value, out var a); state._degree.TryGetValue(e.Second.Value, out var b);
                if (a >= MaxEdgesPerResident || b >= MaxEdgesPerResident) { state._edges.RemoveAt(i--); continue; }
                state.Increment(e.First.Value); state.Increment(e.Second.Value);
            }
            var seenSupports = new HashSet<string>();
            if (data.supports != null) foreach (var s in data.supports)
                if (s != null && live.Contains(s.residentId) && Array.IndexOf(FactionIds.All, s.factionId) >= 0)
                    if (seenSupports.Add(Key(s.residentId, s.factionId)))
                        state._supports.Add(new FactionSupport(new EntityId(s.residentId), s.factionId, ClampFinite(s.support, 0f, 1f), Math.Max(0, Math.Min(10000, s.sustainedDays)), s.driver ?? "unavailable", s.homeFloor));
            state._supports.Sort((a, b) => a.ResidentId.Value != b.ResidentId.Value ? a.ResidentId.Value.CompareTo(b.ResidentId.Value) : string.CompareOrdinal(a.FactionId, b.FactionId));
            if (data.factions != null) foreach (var saved in data.factions)
                if (saved != null) foreach (var faction in state._factions) if (faction.Id == saved.id)
                    { faction.Pressure = ClampFinite(saved.pressure, 0f, 1f); faction.PreviousPressure = ClampFinite(saved.previousPressure, 0f, 1f); faction.TopGrievance = saved.topGrievance ?? "none"; }
            foreach (var faction in state._factions) foreach (var support in state._supports) if (support.FactionId == faction.Id) { faction.SupporterCount++; if (support.IsMember) faction.MemberCount++; }
            state.LastEvaluationTick = Math.Max(0, data.lastEvaluationTick);
            return state;
        }
        private static float ClampFinite(float value, float min, float max)
            => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(min, Math.Min(max, value));
    }
}
