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
            if (FloatEquals(rentCapMultiplier, LowRentCapMultiplier)) RentCapMultiplier = LowRentCapMultiplier;
            else if (FloatEquals(rentCapMultiplier, NeutralRentCapMultiplier)) RentCapMultiplier = NeutralRentCapMultiplier;
            else if (FloatEquals(rentCapMultiplier, HighRentCapMultiplier)) RentCapMultiplier = HighRentCapMultiplier;
            else throw new ArgumentOutOfRangeException(nameof(rentCapMultiplier), rentCapMultiplier, "Rent cap must be 0.7, 1.0, or 1.3.");

            if (FloatEquals(commercialTaxRate, NoCommercialTaxRate)) CommercialTaxRate = NoCommercialTaxRate;
            else if (FloatEquals(commercialTaxRate, StandardCommercialTaxRate)) CommercialTaxRate = StandardCommercialTaxRate;
            else if (FloatEquals(commercialTaxRate, HighCommercialTaxRate)) CommercialTaxRate = HighCommercialTaxRate;
            else throw new ArgumentOutOfRangeException(nameof(commercialTaxRate), commercialTaxRate, "Commercial tax must be 0%, 10%, or 20%.");

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
        public bool IsAggressive => FloatEquals(RentCapMultiplier, HighRentCapMultiplier) ||
                                    FloatEquals(CommercialTaxRate, HighCommercialTaxRate);

        public static bool FloatEquals(float a, float b) => Math.Abs(a - b) < 0.001f;

        public static bool IsAllowedRentCapMultiplier(float value) =>
            FloatEquals(value, LowRentCapMultiplier) ||
            FloatEquals(value, NeutralRentCapMultiplier) ||
            FloatEquals(value, HighRentCapMultiplier);

        public static bool IsAllowedCommercialTaxRate(float value) =>
            FloatEquals(value, NoCommercialTaxRate) ||
            FloatEquals(value, StandardCommercialTaxRate) ||
            FloatEquals(value, HighCommercialTaxRate);

        public bool Equals(PolicyDecreeState other)
        {
            return other != null &&
                   FloatEquals(RentCapMultiplier, other.RentCapMultiplier) &&
                   FloatEquals(CommercialTaxRate, other.CommercialTaxRate) &&
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
