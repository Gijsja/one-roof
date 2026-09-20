using System;

namespace OneRoof.Domain.Population
{
    /// <summary>Emergent role a resident can gain from available training capacity.</summary>
    public enum SpecialistRole
    {
        None = 0,
        Maintenance = 1,
        Security = 2,
        Service = 3,
        Knowledge = 4
    }

    /// <summary>
    /// Mutable, person-owned training record. Systems may advance it; no player command can
    /// select a particular resident for a role.
    /// </summary>
    public sealed class SpecialistRoleState
    {
        public SpecialistRoleState(SpecialistRole role = SpecialistRole.None, SpecialistRole trainingRole = SpecialistRole.None, float trainingProgress = 0f)
        {
            Role = IsDefined(role) ? role : SpecialistRole.None;
            TrainingRole = Role == SpecialistRole.None && IsDefined(trainingRole) ? trainingRole : SpecialistRole.None;
            TrainingProgress = Clamp(trainingProgress);
        }

        public SpecialistRole Role { get; private set; }
        public SpecialistRole TrainingRole { get; private set; }
        public float TrainingProgress { get; private set; }
        public bool IsTraining => Role == SpecialistRole.None && TrainingRole != SpecialistRole.None;

        public bool AdvanceTowards(SpecialistRole role, float progress)
        {
            if (role == SpecialistRole.None || Role != SpecialistRole.None || progress <= 0f) return false;
            if (TrainingRole != role)
            {
                TrainingRole = role;
                TrainingProgress = 0f;
            }
            TrainingProgress = Clamp(TrainingProgress + progress);
            if (TrainingProgress < 1f) return false;
            Role = role;
            TrainingRole = SpecialistRole.None;
            TrainingProgress = 0f;
            return true;
        }

        public void Restore(SpecialistRole role, SpecialistRole trainingRole, float trainingProgress)
        {
            Role = IsDefined(role) ? role : SpecialistRole.None;
            TrainingRole = Role == SpecialistRole.None && IsDefined(trainingRole) ? trainingRole : SpecialistRole.None;
            TrainingProgress = Clamp(trainingProgress);
        }

        private static bool IsDefined(SpecialistRole role) => Enum.IsDefined(typeof(SpecialistRole), role);
        private static float Clamp(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
