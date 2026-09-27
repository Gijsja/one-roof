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
        { Factions = new ReadOnlyCollection<FactionSummaryProjection>(new List<FactionSummaryProjection>(factions)); ResidentSupports = new ReadOnlyCollection<ResidentFactionProjection>(new List<ResidentFactionProjection>(residents)); }
        public IReadOnlyList<FactionSummaryProjection> Factions { get; }
        public IReadOnlyList<ResidentFactionProjection> ResidentSupports { get; }
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
            return new FactionProjection(summaries, residents);
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
