using System;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// An attributable, time-decayed mental thought/mood event attached to a resident's inner life.
    /// Pure C# domain record, fully deterministic.
    /// </summary>
    [Serializable]
    public readonly struct ThoughtMemory : IEquatable<ThoughtMemory>
    {
        public ThoughtMemory(string description, float moodDelta, long createdAtTick, long expiresAtTick, EntityId targetResidentId = default)
        {
            Description = description ?? string.Empty;
            MoodDelta = moodDelta;
            CreatedAtTick = createdAtTick;
            ExpiresAtTick = expiresAtTick;
            TargetResidentId = targetResidentId;
        }

        public string Description { get; }

        public float MoodDelta { get; }

        public long CreatedAtTick { get; }

        public long ExpiresAtTick { get; }

        public EntityId TargetResidentId { get; }

        public bool IsActiveAt(long tick) => tick <= ExpiresAtTick;

        public bool IsExpiredAt(long tick) => tick > ExpiresAtTick;

        public bool Equals(ThoughtMemory other) =>
            Description == other.Description &&
            MoodDelta.Equals(other.MoodDelta) &&
            CreatedAtTick == other.CreatedAtTick &&
            ExpiresAtTick == other.ExpiresAtTick &&
            TargetResidentId.Equals(other.TargetResidentId);

        public override bool Equals(object obj) => obj is ThoughtMemory other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (Description != null ? Description.GetHashCode() : 0);
                hash = (hash * 397) ^ MoodDelta.GetHashCode();
                hash = (hash * 397) ^ CreatedAtTick.GetHashCode();
                hash = (hash * 397) ^ ExpiresAtTick.GetHashCode();
                hash = (hash * 397) ^ TargetResidentId.GetHashCode();
                return hash;
            }
        }

        public override string ToString() =>
            $"Thought: \"{Description}\" ({MoodDelta:+0.0;-0.0;0}, created {CreatedAtTick}, expires {ExpiresAtTick}, target {TargetResidentId})";

        public static bool operator ==(ThoughtMemory left, ThoughtMemory right) => left.Equals(right);

        public static bool operator !=(ThoughtMemory left, ThoughtMemory right) => !left.Equals(right);
    }
}
