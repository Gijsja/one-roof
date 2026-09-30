using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain;
using OneRoof.Domain.Social;

namespace OneRoof.Application.Social
{
    public sealed class FactionSummaryProjection
    {
        public FactionSummaryProjection(string id, string name, float pressure, float previousPressure, string topGrievance, int members, int supporters, IDictionary<int, int> influenceByFloor, IList<int> representativeResidentIds)
        {
            Id = id; Name = name; Pressure = pressure; PreviousPressure = previousPressure; TopGrievance = topGrievance;
            MemberCount = members; SupporterCount = supporters;
            InfluenceByFloor = new ReadOnlyDictionary<int, int>(new Dictionary<int, int>(influenceByFloor));
            RepresentativeResidentIds = new ReadOnlyCollection<int>(new List<int>(representativeResidentIds));
        }
        public string Id { get; }
        public string Name { get; }
        public float Pressure { get; }
        public float PreviousPressure { get; }
        public string Trend => Pressure > PreviousPressure + .001f ? "rising" : Pressure < PreviousPressure - .001f ? "falling" : "steady";
        public string TopGrievance { get; }
        public int MemberCount { get; }
        public int SupporterCount { get; }
        public IReadOnlyDictionary<int, int> InfluenceByFloor { get; }
        public IReadOnlyList<int> RepresentativeResidentIds { get; }
    }

    public sealed class ResidentFactionProjection
    {
        public ResidentFactionProjection(int residentId, string factionId, float support, bool member, string driver, int homeFloor)
        { ResidentId = residentId; FactionId = factionId; Support = support; IsMember = member; Driver = driver; HomeFloor = homeFloor; }
        public int ResidentId { get; }
        public string FactionId { get; }
        public float Support { get; }
        public bool IsMember { get; }
        public string Driver { get; }
        public int HomeFloor { get; }
    }

    public sealed class FactionProjection
    {
        public FactionProjection(IList<FactionSummaryProjection> factions, IList<ResidentFactionProjection> residents)
            : this(factions, residents, new List<ResidentTieProjection>()) { }
        public FactionProjection(IList<FactionSummaryProjection> factions, IList<ResidentFactionProjection> residents, IList<ResidentTieProjection> ties)
        { Factions = new ReadOnlyCollection<FactionSummaryProjection>(new List<FactionSummaryProjection>(factions)); ResidentSupports = new ReadOnlyCollection<ResidentFactionProjection>(new List<ResidentFactionProjection>(residents)); ResidentTies = new ReadOnlyCollection<ResidentTieProjection>(new List<ResidentTieProjection>(ties)); }
        public IReadOnlyList<FactionSummaryProjection> Factions { get; }
        public IReadOnlyList<ResidentFactionProjection> ResidentSupports { get; }
        public IReadOnlyList<ResidentTieProjection> ResidentTies { get; }
    }

    public sealed class ResidentTieProjection
    {
        public ResidentTieProjection(int firstId, int secondId, float affinity, float previousAffinity, long lastContactTick, long lastMeaningfulTick, string cause, RelationshipStage stage = RelationshipStage.Stranger)
        { FirstId = firstId; SecondId = secondId; Affinity = affinity; PreviousAffinity = previousAffinity; LastContactTick = lastContactTick; LastMeaningfulTick = lastMeaningfulTick; Cause = cause; Stage = stage; }
        public int FirstId { get; }
        public int SecondId { get; }
        public float Affinity { get; }
        public float PreviousAffinity { get; }
        public RelationshipStage Stage { get; }
        public string Trend
        {
            get
            {
                if (Math.Abs(Affinity - PreviousAffinity) <= .001f) return "steady";
                if (Affinity < 0f && Affinity < PreviousAffinity) return "straining";
                if (Affinity > 0f && Affinity > PreviousAffinity) return "strengthening";
                return "easing toward neutral";
            }
        }
        public long LastContactTick { get; }
        public long LastMeaningfulTick { get; }
        public string Cause { get; }
    }

    public static class FactionProjectionService
    {
        public static FactionProjection Capture(TowerSimulation simulation)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            var social = simulation.Factions;
            var summaries = new List<FactionSummaryProjection>();
            var residents = new List<ResidentFactionProjection>();
            foreach (var faction in social.Factions)
            {
                var floorCounts = new Dictionary<int, int>();
                var contributors = new List<FactionSupport>();
                foreach (var support in social.Supports)
                {
                    if (support.FactionId != faction.Id) continue;
                    residents.Add(new ResidentFactionProjection(support.ResidentId.Value, faction.Id, support.Support, support.IsMember, support.Driver, support.HomeFloor));
                    if (!floorCounts.ContainsKey(support.HomeFloor)) floorCounts[support.HomeFloor] = 0;
                    floorCounts[support.HomeFloor]++;
                    contributors.Add(support);
                }
                contributors.Sort((a, b) => { var bySupport = b.Support.CompareTo(a.Support); return bySupport != 0 ? bySupport : a.ResidentId.Value.CompareTo(b.ResidentId.Value); });
                var representatives = new List<int>();
                for (var i = 0; i < contributors.Count && i < 3; i++) representatives.Add(contributors[i].ResidentId.Value);
                summaries.Add(new FactionSummaryProjection(faction.Id, Name(faction.Id), faction.Pressure, faction.PreviousPressure, faction.TopGrievance, faction.MemberCount, faction.SupporterCount, floorCounts, representatives));
            }
            var ties = new List<ResidentTieProjection>();
            foreach (var edge in social.Edges)
                ties.Add(new ResidentTieProjection(edge.First.Value, edge.Second.Value, edge.Affinity, edge.PreviousAffinity, edge.LastContactTick, edge.LastMeaningfulTick, edge.Cause, edge.Stage));
            return new FactionProjection(summaries, residents, ties);
        }
        private static string Name(string id)
        {
            switch (id)
            {
                case FactionIds.TenantUnion: return "Tenant Union";
                case FactionIds.CorporateCoalition: return "Corporate Coalition";
                case FactionIds.MerchantGuild: return "Merchant Guild";
                default: return "Civic & Eco Council";
            }
        }
    }
}
