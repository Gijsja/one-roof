using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Application.Overlays
{
    /// <summary>Room-scale acoustic estimate. Values are relative activity units, not decibels.</summary>
    public sealed class NoiseOverlayProjection
    {
        public NoiseOverlayProjection(long tick, bool quietHoursEnabled, IReadOnlyList<NoiseRoomProjection> rooms)
        {
            Tick = tick;
            QuietHoursEnabled = quietHoursEnabled;
            Rooms = new ReadOnlyCollection<NoiseRoomProjection>(new List<NoiseRoomProjection>(rooms ?? Array.Empty<NoiseRoomProjection>()));
        }

        public long Tick { get; }
        public bool QuietHoursEnabled { get; }
        public IReadOnlyList<NoiseRoomProjection> Rooms { get; }
        public string ModelLabel => "Relative room-activity estimate; attenuates by cells and floors, not measured sound propagation.";
        public string QuietHoursLabel => QuietHoursEnabled
            ? "Quiet hours: discretionary activity is estimated quieter; merchant revenue may fall."
            : "Quiet hours inactive now: discretionary activity keeps its normal estimate.";
    }

    public readonly struct NoiseRoomProjection
    {
        public NoiseRoomProjection(EntityId roomId, int floor, int minX, int maxX, float intensity, int sourceCount, int affectedOccupants, EntityId strongestSourceRoomId, string strongestSourceLabel)
        {
            RoomId = roomId;
            Floor = floor;
            MinX = minX;
            MaxX = maxX;
            Intensity = intensity;
            SourceCount = sourceCount;
            AffectedOccupants = affectedOccupants;
            StrongestSourceRoomId = strongestSourceRoomId;
            StrongestSourceLabel = strongestSourceLabel ?? string.Empty;
        }

        public EntityId RoomId { get; }
        public int Floor { get; }
        public int MinX { get; }
        public int MaxX { get; }
        public float Intensity { get; }
        public int SourceCount { get; }
        public int AffectedOccupants { get; }
        public EntityId StrongestSourceRoomId { get; }
        public string StrongestSourceLabel { get; }
        public string Tier => Intensity >= 2.5f ? "HIGH" : Intensity >= 1f ? "ELEVATED" : Intensity > 0f ? "LOW" : "NO ACTIVITY";
        public string Glyph => Intensity >= 2.5f ? "!!!" : Intensity >= 1f ? "!!" : Intensity > 0f ? "." : "-";
        public string AccessibilityLabel => $"{Glyph} Floor {Floor} room {RoomId}: {Tier} ({Intensity:0.0} relative units), {AffectedOccupants} occupant(s) affected; {SourceCount} nearby source(s). Strongest: {StrongestSourceLabel}";
    }
}
