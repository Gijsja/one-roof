using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Topology;

namespace OneRoof.Application.Overlays
{
    /// <summary>Builds presentation-safe aggregate satisfaction data without mutating the domain.</summary>
    public sealed class SatisfactionOverlayService
    {
        public SatisfactionOverlayProjection CreateOverlay(TowerSimulationSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var accumulators = new SortedDictionary<int, float[]>();
            foreach (var person in session.Population.Persons)
            {
                if (!session.Topology.TryGetRoom(person.CurrentRoomId, out var room)) continue;
                if (!accumulators.TryGetValue(room.Floor, out var values)) accumulators.Add(room.Floor, values = new float[3]);
                values[0] += person.Wellbeing.Satisfaction; values[1]++; values[2] += person.Wellbeing.Grievances.Count;
            }
            var floors = new List<SatisfactionFloorProjection>(); var total = 0f; var count = 0;
            foreach (var entry in accumulators)
            {
                var values = entry.Value; var satisfaction = values[1] == 0 ? 1f : values[0] / values[1];
                floors.Add(new SatisfactionFloorProjection(entry.Key, satisfaction, (int)values[1], (int)values[2]));
                total += values[0]; count += (int)values[1];
            }
            return new SatisfactionOverlayProjection(count == 0 ? 1f : total / count, floors);
        }
    }
}
