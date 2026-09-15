using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Events
{
    /// <summary>Immutable simulation fact emitted after a successful domain change.</summary>
    public sealed class DomainEvent : IComparable<DomainEvent>
    {
        public DomainEvent(EntityId eventId, ContentId type, Tick tick, IEnumerable<EntityId> affectedEntityIds = null)
        {
            eventId.EnsureValid();
            type.EnsureValid();
            EventId = eventId;
            Type = type;
            Tick = tick;
            AffectedEntityIds = CopyAffectedIds(affectedEntityIds);
        }

        public EntityId EventId { get; }

        public ContentId Type { get; }

        public Tick Tick { get; }

        public IReadOnlyList<EntityId> AffectedEntityIds { get; }

        public int CompareTo(DomainEvent other)
        {
            if (other == null)
            {
                return 1;
            }

            var tickComparison = Tick.CompareTo(other.Tick);
            return tickComparison != 0 ? tickComparison : EventId.CompareTo(other.EventId);
        }

        private static IReadOnlyList<EntityId> CopyAffectedIds(IEnumerable<EntityId> values)
        {
            var copy = new List<EntityId>();
            if (values != null)
            {
                foreach (var value in values)
                {
                    copy.Add(value);
                }
            }

            return new ReadOnlyCollection<EntityId>(copy);
        }
    }
}
