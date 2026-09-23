using System;

namespace OneRoof.Domain.Economy
{
    /// <summary>
    /// Immutable, validated settings for the tower's economy policy decrees.
    /// Values are deliberately limited to the options exposed by the decree panel.
    /// </summary>
    public sealed class PolicyDecreeState : IEquatable<PolicyDecreeState>
    {
        public const float LowRentCapMultiplier = 0.7f;
        public const float NeutralRentCapMultiplier = 1.0f;
        public const float HighRentCapMultiplier = 1.3f;

        public const float NoCommercialTaxRate = 0.0f;
        public const float StandardCommercialTaxRate = 0.1f;
        public const float HighCommercialTaxRate = 0.2f;

        public const int TransitSubsidyPerDay = 40;

        public static PolicyDecreeState Default { get; } = new PolicyDecreeState(
            NeutralRentCapMultiplier,
            NoCommercialTaxRate,
            transitSubsidyEnabled: false,
            quietHoursEnabled: false);

        public PolicyDecreeState(
            float rentCapMultiplier,
            float commercialTaxRate,
            bool transitSubsidyEnabled,
            bool quietHoursEnabled)
        {
            if (!IsAllowedRentCapMultiplier(rentCapMultiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(rentCapMultiplier), rentCapMultiplier,
                    "Rent cap must be 0.7, 1.0, or 1.3.");
            }

            if (!IsAllowedCommercialTaxRate(commercialTaxRate))
            {
                throw new ArgumentOutOfRangeException(nameof(commercialTaxRate), commercialTaxRate,
                    "Commercial tax must be 0%, 10%, or 20%.");
            }

            RentCapMultiplier = rentCapMultiplier;
            CommercialTaxRate = commercialTaxRate;
            TransitSubsidyEnabled = transitSubsidyEnabled;
            QuietHoursEnabled = quietHoursEnabled;
        }

        public float RentCapMultiplier { get; }

        /// <summary>Fraction of gross commercial revenue; for example, 0.1 means 10%.</summary>
        public float CommercialTaxRate { get; }

        public bool TransitSubsidyEnabled { get; }

        public int DailyTransitSubsidy => TransitSubsidyEnabled ? TransitSubsidyPerDay : 0;

        public bool QuietHoursEnabled { get; }

        /// <summary>Whether this decree uses either setting identified as extreme by ECON-003.</summary>
        public bool IsAggressive => RentCapMultiplier == HighRentCapMultiplier ||
                                    CommercialTaxRate == HighCommercialTaxRate;

        public static bool IsAllowedRentCapMultiplier(float value) =>
            value == LowRentCapMultiplier ||
            value == NeutralRentCapMultiplier ||
            value == HighRentCapMultiplier;

        public static bool IsAllowedCommercialTaxRate(float value) =>
            value == NoCommercialTaxRate ||
            value == StandardCommercialTaxRate ||
            value == HighCommercialTaxRate;

        public bool Equals(PolicyDecreeState other)
        {
            return other != null &&
                   RentCapMultiplier == other.RentCapMultiplier &&
                   CommercialTaxRate == other.CommercialTaxRate &&
                   TransitSubsidyEnabled == other.TransitSubsidyEnabled &&
                   QuietHoursEnabled == other.QuietHoursEnabled;
        }

        public override bool Equals(object obj) => Equals(obj as PolicyDecreeState);

        public override int GetHashCode() => HashCode.Combine(
            RentCapMultiplier,
            CommercialTaxRate,
            TransitSubsidyEnabled,
            QuietHoursEnabled);

        public static bool operator ==(PolicyDecreeState left, PolicyDecreeState right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
            return left.Equals(right);
        }

        public static bool operator !=(PolicyDecreeState left, PolicyDecreeState right) => !(left == right);
    }
}
