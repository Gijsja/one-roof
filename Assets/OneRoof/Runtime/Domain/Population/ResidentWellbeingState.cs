using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Domain.Population
{
    /// <summary>Mutable domain-only wellbeing state; values remain normalized to [0,1].</summary>
    public sealed class ResidentWellbeingState
    {
        public const int MaxThoughts = 16;
        private readonly List<string> _priorSettlementGrievances = new List<string>();
        private readonly List<SocialGrievanceInfluence> _socialInfluences = new List<SocialGrievanceInfluence>();
        private readonly ReadOnlyCollection<string> _priorSettlementView;
        private readonly ReadOnlyCollection<SocialGrievanceInfluence> _socialInfluenceView;
        public ResidentWellbeingState() { _priorSettlementView = _priorSettlementGrievances.AsReadOnly(); _socialInfluenceView = _socialInfluences.AsReadOnly(); }
        public IReadOnlyList<string> PriorSettlementGrievances => _priorSettlementView;
        public IReadOnlyList<SocialGrievanceInfluence> SocialInfluences => _socialInfluenceView;
        public float SocialPressure { get { var total = 0f; foreach (var influence in _socialInfluences) total += influence.Pressure; return total; } }
        public void CaptureSettlementGrievances() { _priorSettlementGrievances.Clear(); _priorSettlementGrievances.AddRange(_grievances); }
        public void RestoreSocialInfluence(IEnumerable<string> prior, IEnumerable<SocialGrievanceInfluence> influences)
        {
            _priorSettlementGrievances.Clear();
            if (prior != null) foreach (var issue in prior) if (!string.IsNullOrEmpty(issue) && !_priorSettlementGrievances.Contains(issue)) _priorSettlementGrievances.Add(issue);
            ReplaceSocialInfluences(influences);
        }
        internal void ReplaceSocialInfluences(IEnumerable<SocialGrievanceInfluence> influences)
        {
            _socialInfluences.Clear();
            var remaining = ResidentSocialInfluenceSystem.MaxPressure;
            if (influences != null) foreach (var influence in influences)
            {
                if (string.IsNullOrEmpty(influence.Issue) || remaining <= 0f || float.IsNaN(influence.Pressure)) continue;
                var duplicate = false;
                foreach (var existing in _socialInfluences) if (string.Equals(existing.Issue, influence.Issue, StringComparison.Ordinal)) { duplicate = true; break; }
                if (duplicate) continue;
                var pressure = Math.Min(remaining, Math.Max(0f, Math.Min(ResidentSocialInfluenceSystem.MaxIssuePressure, influence.Pressure)));
                if (pressure > 0f) { _socialInfluences.Add(new SocialGrievanceInfluence(influence.Issue, pressure)); remaining -= pressure; }
            }
        }
        private readonly List<string> _grievances = new List<string>();
        private readonly List<ThoughtMemory> _thoughts = new List<ThoughtMemory>();
        public float Satisfaction { get; private set; } = 1f;
        public float Strain { get; private set; }
        public float Commute { get; private set; } = 1f;
        public float Crowding { get; private set; } = 1f;
        public float Noise { get; private set; } = 1f;
        public float RentBurden { get; private set; } = 1f;
        public float ServiceAccess { get; private set; } = 1f;
        public float RecentEvents { get; private set; } = 1f;
        public IReadOnlyList<string> Grievances => new ReadOnlyCollection<string>(_grievances);
        public IReadOnlyList<ThoughtMemory> ActiveThoughts => _thoughts;
        public IReadOnlyList<ThoughtMemory> Thoughts => _thoughts;

        public float TotalThoughtMoodDelta
        {
            get
            {
                var sum = 0f;
                for (var i = 0; i < _thoughts.Count; i++) sum += _thoughts[i].MoodDelta;
                return sum;
            }
        }

        public void AddThought(ThoughtMemory thought)
        {
            if (_thoughts.Count >= MaxThoughts)
            {
                PruneExpiredThoughts(thought.CreatedAtTick);
            }
            if (_thoughts.Count >= MaxThoughts)
            {
                _thoughts.RemoveAt(0);
            }
            _thoughts.Add(thought);
        }

        public void PruneExpiredThoughts(long currentTick)
        {
            for (var i = _thoughts.Count - 1; i >= 0; i--)
            {
                if (_thoughts[i].IsExpiredAt(currentTick))
                {
                    _thoughts.RemoveAt(i);
                }
            }
        }

        public void ClearThoughts()
        {
            _thoughts.Clear();
        }

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
