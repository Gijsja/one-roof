using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Population;

namespace OneRoof.Application.Inspectors
{
    public readonly struct ThoughtMemorySnapshot
    {
        public ThoughtMemorySnapshot(string description, float moodDelta, long remainingTicks, int targetResidentId)
        {
            Description = description ?? string.Empty;
            MoodDelta = moodDelta;
            RemainingTicks = remainingTicks;
            TargetResidentId = targetResidentId;
        }

        public string Description { get; }
        public float MoodDelta { get; }
        public long RemainingTicks { get; }
        public int TargetResidentId { get; }
    }

    /// <summary>A resident's inspection facts captured independently of later simulation ticks.</summary>
    public sealed class ResidentInspectionSnapshot
    {
        internal ResidentInspectionSnapshot(PersonRecord person, long currentTick = 0)
        {
            Activity = person.CurrentActivity;
            HouseholdId = person.HouseholdId.Value;
            HomeRoomId = person.HomeRoomId.Value;
            WorkplaceRoomId = person.WorkplaceRoomId.Value;
            WorksOutside = person.WorkplaceLocation.IsOutside;
            IsOutside = person.CurrentLocation.IsOutside;
            Role = person.Specialization.Role;
            TrainingRole = person.Specialization.TrainingRole;
            TrainingProgress = person.Specialization.TrainingProgress;
            IsTraining = person.Specialization.IsTraining;
            Needs = new ReadOnlyCollection<NeedState>(new List<NeedState>(person.Needs));
            Traits = new ReadOnlyCollection<PersonTrait>(new List<PersonTrait>(person.Traits));
            PersonalityFacets = new ReadOnlyCollection<PersonalityFacet>(new List<PersonalityFacet>(person.PersonalityFacets));
            Satisfaction = person.Wellbeing.Satisfaction;
            Strain = person.Wellbeing.Strain;
            Commute = person.Wellbeing.Commute;
            RentBurden = person.Wellbeing.RentBurden;
            Grievances = new ReadOnlyCollection<string>(new List<string>(person.Wellbeing.Grievances));

            var traits = new List<SocialTraitKind>();
            if (person.SocialTraits != null)
            {
                for (var i = 0; i < person.SocialTraits.Count; i++)
                    traits.Add(person.SocialTraits[i].Kind);
            }
            SocialTraits = new ReadOnlyCollection<SocialTraitKind>(traits);

            var thoughts = new List<ThoughtMemorySnapshot>();
            if (person.Wellbeing.ActiveThoughts != null)
            {
                for (var i = 0; i < person.Wellbeing.ActiveThoughts.Count; i++)
                {
                    var t = person.Wellbeing.ActiveThoughts[i];
                    var remaining = Math.Max(0, t.ExpiresAtTick - (currentTick > 0 ? currentTick : t.CreatedAtTick));
                    thoughts.Add(new ThoughtMemorySnapshot(t.Description, t.MoodDelta, remaining, t.TargetResidentId.Value));
                }
            }
            ActiveThoughts = new ReadOnlyCollection<ThoughtMemorySnapshot>(thoughts);
            TotalThoughtMoodDelta = person.Wellbeing.TotalThoughtMoodDelta;
        }

        public ActivityKind Activity { get; }
        public int HouseholdId { get; }
        public int HomeRoomId { get; }
        public int WorkplaceRoomId { get; }
        public bool WorksOutside { get; }
        public bool IsOutside { get; }
        public SpecialistRole Role { get; }
        public SpecialistRole TrainingRole { get; }
        public float TrainingProgress { get; }
        public bool IsTraining { get; }
        public IReadOnlyList<NeedState> Needs { get; }
        public IReadOnlyList<PersonTrait> Traits { get; }
        public IReadOnlyList<PersonalityFacet> PersonalityFacets { get; }
        public float Satisfaction { get; }
        public float Strain { get; }
        public float Commute { get; }
        public float RentBurden { get; }
        public IReadOnlyList<string> Grievances { get; }
        public IReadOnlyList<SocialTraitKind> SocialTraits { get; }
        public IReadOnlyList<ThoughtMemorySnapshot> ActiveThoughts { get; }
        public float TotalThoughtMoodDelta { get; }
    }
}
