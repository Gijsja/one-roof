using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Application.Social;

namespace OneRoof.Application.Overlays
{
    public sealed class FactionTensionOverlayProjection
    {
        public FactionTensionOverlayProjection(long tick, IReadOnlyList<FactionTensionFloorProjection> floors)
        {
            Tick = tick;
            Floors = new ReadOnlyCollection<FactionTensionFloorProjection>(new List<FactionTensionFloorProjection>(floors ?? Array.Empty<FactionTensionFloorProjection>()));
        }

        public long Tick { get; }
        public IReadOnlyList<FactionTensionFloorProjection> Floors { get; }
    }

    public readonly struct FactionTensionFloorProjection
    {
        public FactionTensionFloorProjection(int floor, int supporters, float averagePressure, string leadingFactionId, string leadingFactionName, string topGrievance, string trend)
        {
            Floor = floor;
            SupporterCount = supporters;
            AveragePressure = averagePressure;
            LeadingFactionId = leadingFactionId ?? string.Empty;
            LeadingFactionName = leadingFactionName ?? string.Empty;
            TopGrievance = topGrievance ?? "none";
            Trend = trend ?? "steady";
        }

        public int Floor { get; }
        public int SupporterCount { get; }
        public float AveragePressure { get; }
        public string LeadingFactionId { get; }
        public string LeadingFactionName { get; }
        public string TopGrievance { get; }
        public string Trend { get; }
        public string Tier => SupporterCount == 0 ? "NO DATA" : AveragePressure >= .7f ? "SEVERE" : AveragePressure >= .4f ? "ELEVATED" : "LOW";
        public string Glyph => SupporterCount == 0 ? "-" : AveragePressure >= .7f ? "!!!" : AveragePressure >= .4f ? "!!" : ".";
        public string AccessibilityLabel => SupporterCount == 0
            ? $"Floor {Floor}: no faction supporters here."
            : $"{Glyph} Floor {Floor}: {Tier} faction tension ({AveragePressure:P0}), {SupporterCount} support signal(s); {LeadingFactionName}, {TopGrievance}, {Trend}.";
    }

    public static class FactionTensionOverlayProjector
    {
        private sealed class FloorAccumulator
        {
            public int Count;
            public float Sum;
            public readonly Dictionary<string, int> ByFaction = new Dictionary<string, int>();
        }

        public static FactionTensionOverlayProjection Project(long tick, FactionProjection social, int floorCount)
        {
            if (social == null) throw new ArgumentNullException(nameof(social));
            var floors = new SortedDictionary<int, FloorAccumulator>();
            for (var floor = 0; floor < floorCount; floor++) floors[floor] = new FloorAccumulator();
            foreach (var faction in social.Factions)
                foreach (var pair in faction.InfluenceByFloor)
                    if (!floors.ContainsKey(pair.Key)) floors[pair.Key] = new FloorAccumulator();
            foreach (var support in social.ResidentSupports)
            {
                if (!floors.TryGetValue(support.HomeFloor, out var entry))
                    floors[support.HomeFloor] = entry = new FloorAccumulator();
                entry.Count++;
                entry.Sum += support.Support;
                entry.ByFaction.TryGetValue(support.FactionId, out var count);
                entry.ByFaction[support.FactionId] = count + 1;
            }
            var result = new List<FactionTensionFloorProjection>(floors.Count);
            foreach (var pair in floors)
            {
                var entry = pair.Value;
                FactionSummaryProjection leader = null;
                var strongest = -1;
                foreach (var faction in social.Factions)
                    if (entry.ByFaction.TryGetValue(faction.Id, out var count) && count > strongest)
                    { strongest = count; leader = faction; }
                result.Add(new FactionTensionFloorProjection(pair.Key, entry.Count,
                    entry.Count == 0 ? 0f : entry.Sum / entry.Count,
                    leader?.Id, leader?.Name, leader?.TopGrievance, leader?.Trend));
            }
            return new FactionTensionOverlayProjection(tick, result);
        }
    }
}
