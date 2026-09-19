using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Domain.Population
{
    /// <summary>Mutable domain-only wellbeing state; values remain normalized to [0,1].</summary>
    public sealed class ResidentWellbeingState
    {
        private readonly List<string> _grievances = new List<string>();
        public float Satisfaction { get; private set; } = 1f;
        public float Strain { get; private set; }
        public float Commute { get; private set; } = 1f;
        public float Crowding { get; private set; } = 1f;
        public float Noise { get; private set; } = 1f;
        public float RentBurden { get; private set; } = 1f;
        public float ServiceAccess { get; private set; } = 1f;
        public float RecentEvents { get; private set; } = 1f;
        public IReadOnlyList<string> Grievances => new ReadOnlyCollection<string>(_grievances);

        public void Update(float satisfaction, float strain, float commute, float crowding, float noise, float rentBurden, float serviceAccess, float recentEvents, IEnumerable<string> grievances)
        {
            Satisfaction = Clamp(satisfaction); Strain = Clamp(strain); Commute = Clamp(commute);
            Crowding = Clamp(crowding); Noise = Clamp(noise); RentBurden = Clamp(rentBurden);
            ServiceAccess = Clamp(serviceAccess); RecentEvents = Clamp(recentEvents);
            _grievances.Clear();
            if (grievances == null) return;
            foreach (var grievance in grievances)
                if (!string.IsNullOrEmpty(grievance)) _grievances.Add(grievance);
        }

        private static float Clamp(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);
    }
}
