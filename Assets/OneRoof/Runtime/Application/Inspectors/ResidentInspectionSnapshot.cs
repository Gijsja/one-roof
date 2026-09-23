using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Population;

namespace OneRoof.Application.Inspectors
{
    /// <summary>A resident's inspection facts captured independently of later simulation ticks.</summary>
    public sealed class ResidentInspectionSnapshot
    {
        internal ResidentInspectionSnapshot(PersonRecord person)
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
    }
}
