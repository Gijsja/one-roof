using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Application.Overlays
{
    /// <summary>Projects current resident activity into a bounded room-scale acoustic field.</summary>
    public static class NoiseOverlayProjector
    {
        public static NoiseOverlayProjection Project(long tick, IReadOnlyDictionary<EntityId, Room> rooms, IReadOnlyList<PersonRecord> residents, bool quietHoursEnabled)
        {
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (residents == null) throw new ArgumentNullException(nameof(residents));
            var ordered = new List<Room>(rooms.Values);
            ordered.Sort((a, b) => a.Id.CompareTo(b.Id));
            var source = new Dictionary<EntityId, float>();
            var occupants = new Dictionary<EntityId, int>();
            foreach (var resident in residents)
            {
                if (resident.CurrentLocation.IsOutside || !rooms.ContainsKey(resident.CurrentRoomId)) continue;
                var id = resident.CurrentRoomId;
                occupants[id] = occupants.TryGetValue(id, out var count) ? count + 1 : 1;
                var strength = ActivityStrength(resident.CurrentActivity, quietHoursEnabled);
                if (strength <= 0f) continue;
                source[id] = source.TryGetValue(id, out var prior) ? prior + strength : strength;
            }
            var sources = new List<Room>();
            foreach (var room in ordered) if (source.ContainsKey(room.Id)) sources.Add(room);
            var result = new List<NoiseRoomProjection>(ordered.Count);
            foreach (var target in ordered)
            {
                var total = 0f;
                var strongest = 0f;
                var strongestId = default(EntityId);
                var nearbyCount = 0;
                foreach (var origin in sources)
                {
                    var floorDistance = Math.Abs(origin.Floor - target.Floor);
                    if (floorDistance > 1) continue;
                    var gap = Math.Max(0, Math.Max(origin.Bounds.MinX - target.Bounds.MaxX, target.Bounds.MinX - origin.Bounds.MaxX));
                    if (gap > 4) continue;
                    var contribution = source[origin.Id] / (1f + gap + floorDistance * 3f);
                    total += contribution;
                    nearbyCount++;
                    if (contribution > strongest)
                    {
                        strongest = contribution;
                        strongestId = origin.Id;
                    }
                }
                var affected = occupants.TryGetValue(target.Id, out var occupied) ? occupied : 0;
                result.Add(new NoiseRoomProjection(target.Id, target.Floor, target.Bounds.MinX, target.Bounds.MaxX,
                    total, nearbyCount, affected, strongestId,
                    strongestId.IsValid ? $"room {strongestId} activity" : "none"));
            }
            return new NoiseOverlayProjection(tick, quietHoursEnabled, result);
        }

        private static float ActivityStrength(ActivityKind activity, bool quietHoursEnabled)
        {
            switch (activity)
            {
                case ActivityKind.Working: return 0.8f;
                case ActivityKind.Eating: return quietHoursEnabled ? 0.45f : 0.9f;
                case ActivityKind.Leisure: return quietHoursEnabled ? 0.6f : 1.2f;
                case ActivityKind.Idle: return 0.2f;
                default: return 0f;
            }
        }
    }
}
