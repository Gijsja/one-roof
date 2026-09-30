using System;

namespace OneRoof.Domain.Commands
{
    /// <summary>Raises the whole elevator bank by exactly one tier.</summary>
    public sealed class UpgradeElevatorTierCommand : ICommand
    {
        public UpgradeElevatorTierCommand(int targetTier)
        {
            if (targetTier < 2 || targetTier > 5)
                throw new ArgumentOutOfRangeException(nameof(targetTier), targetTier, "Target elevator tier must be between 2 and 5.");
            TargetTier = targetTier;
        }

        public int TargetTier { get; }

        public override string ToString() => $"Upgrade elevator bank to tier {TargetTier}";
    }
}
