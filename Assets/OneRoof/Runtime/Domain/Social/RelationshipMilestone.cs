using System;

namespace OneRoof.Domain.Social
{
    /// <summary>
    /// Explicit qualitative stage of an interpersonal relationship edge between two residents.
    /// Spans platonic, romantic, and adversarial branches. Pure C# domain type.
    /// </summary>
    public enum RelationshipStage
    {
        Stranger,
        Acquaintance,
        Friend,
        CloseFriend,
        Crush,
        Dating,
        Partnered,
        Married,
        Disliked,
        Rival,
        Enemy,
        Nemesis
    }

    /// <summary>
    /// Classification and milestone transition logic for relationship progression.
    /// </summary>
    public static class RelationshipMilestones
    {
        public static bool IsRomantic(RelationshipStage stage) =>
            stage == RelationshipStage.Crush ||
            stage == RelationshipStage.Dating ||
            stage == RelationshipStage.Partnered ||
            stage == RelationshipStage.Married;

        public static bool IsAdversarial(RelationshipStage stage) =>
            stage == RelationshipStage.Disliked ||
            stage == RelationshipStage.Rival ||
            stage == RelationshipStage.Enemy ||
            stage == RelationshipStage.Nemesis;

        public static bool IsPlatonic(RelationshipStage stage) =>
            stage == RelationshipStage.Stranger ||
            stage == RelationshipStage.Acquaintance ||
            stage == RelationshipStage.Friend ||
            stage == RelationshipStage.CloseFriend;

        public static bool IsCommittedRomance(RelationshipStage stage) =>
            stage == RelationshipStage.Partnered ||
            stage == RelationshipStage.Married;

        /// <summary>
        /// Deterministically advances, sustains, or regresses a relationship stage given current affinity
        /// and prior stage context.
        /// </summary>
        public static RelationshipStage DeriveStage(float affinity, RelationshipStage currentStage = RelationshipStage.Stranger)
        {
            // Romantic branch preservation and transition rules
            if (currentStage == RelationshipStage.Married)
            {
                if (affinity < -0.25f) return RelationshipStage.Disliked; // Bitter breakup / divorce
                if (affinity < 0.20f) return RelationshipStage.Dating;   // Severe marital strain / separation
                return RelationshipStage.Married;
            }

            if (currentStage == RelationshipStage.Partnered)
            {
                if (affinity < -0.20f) return RelationshipStage.Disliked; // Breakup
                if (affinity < 0.35f) return RelationshipStage.Dating;   // Lover's quarrel
                if (affinity >= 0.85f) return RelationshipStage.Married;  // Marriage milestone
                return RelationshipStage.Partnered;
            }

            if (currentStage == RelationshipStage.Dating)
            {
                if (affinity < -0.10f) return RelationshipStage.Disliked; // Messy breakup
                if (affinity >= 0.75f) return RelationshipStage.Partnered; // Cohabitation
                if (affinity < 0.15f) return RelationshipStage.Acquaintance; // Faded fling
                return RelationshipStage.Dating;
            }

            if (currentStage == RelationshipStage.Crush)
            {
                if (affinity < 0.05f) return RelationshipStage.Acquaintance;
                if (affinity >= 0.50f) return RelationshipStage.Dating;
                return RelationshipStage.Crush;
            }

            // Adversarial branch
            if (affinity <= -0.85f) return RelationshipStage.Nemesis;
            if (affinity <= -0.65f) return RelationshipStage.Enemy;
            if (affinity <= -0.40f) return RelationshipStage.Rival;
            if (affinity <= -0.15f) return RelationshipStage.Disliked;

            // Platonic branch
            if (affinity >= 0.65f) return RelationshipStage.CloseFriend;
            if (affinity >= 0.30f) return RelationshipStage.Friend;
            if (affinity > -0.15f) return RelationshipStage.Acquaintance;

            return RelationshipStage.Stranger;
        }

        public static string GetStageDescription(RelationshipStage stage)
        {
            switch (stage)
            {
                case RelationshipStage.Stranger: return "Stranger";
                case RelationshipStage.Acquaintance: return "Acquaintance";
                case RelationshipStage.Friend: return "Friend";
                case RelationshipStage.CloseFriend: return "Close Friend";
                case RelationshipStage.Crush: return "Crush";
                case RelationshipStage.Dating: return "Dating";
                case RelationshipStage.Partnered: return "Partnered";
                case RelationshipStage.Married: return "Married";
                case RelationshipStage.Disliked: return "Disliked";
                case RelationshipStage.Rival: return "Rival";
                case RelationshipStage.Enemy: return "Enemy";
                case RelationshipStage.Nemesis: return "Nemesis";
                default: return stage.ToString();
            }
        }
    }
}
