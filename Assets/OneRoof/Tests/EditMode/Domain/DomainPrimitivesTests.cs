using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Events;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class DomainPrimitivesTests
    {
        [Test]
        public void IdentityValuesCompareAndHashByTheirStableValues()
        {
            var first = new EntityId(42);
            var same = new EntityId(42);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(new ContentId("room:apartment"), Is.EqualTo(new ContentId("room:apartment")));
            Assert.That(new SchemaVersion(2), Is.EqualTo(new SchemaVersion(2)));
        }

        [Test]
        public void InvalidIdentityValuesAreRejectedAtConstruction()
        {
            Assert.That(() => new EntityId(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new Tick(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SchemaVersion(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ContentId("apartment"), Throws.ArgumentException);
            Assert.That(() => new ContentId("room:two words"), Throws.ArgumentException);
            Assert.That(() => new DomainEvent(default, new ContentId("event:valid"), new Tick(0)), Throws.InvalidOperationException);
        }

        [Test]
        public void ClockAdvancesOnlyByExplicitFixedSteps()
        {
            var clock = new SimulationClock(new Tick(10));

            Assert.That(clock.Advance(), Is.EqualTo(new Tick(11)));
            Assert.That(clock.Advance(4), Is.EqualTo(new Tick(15)));
            Assert.That(() => clock.Advance(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void IdenticallySeededStreamsProduceIdenticalSequences()
        {
            var first = new DeterministicRandomStream(12345);
            var second = new DeterministicRandomStream(12345);

            for (var index = 0; index < 20; index++)
            {
                Assert.That(first.NextInt(-20, 20), Is.EqualTo(second.NextInt(-20, 20)));
            }
        }

        [Test]
        public void RestoredRandomStateResumesAtTheExactNextValue()
        {
            var original = new DeterministicRandomStream(9876);
            original.NextUInt32();
            original.NextUInt32();
            var restored = DeterministicRandomStream.Restore(original.State);

            Assert.That(restored.NextUInt32(), Is.EqualTo(original.NextUInt32()));
            Assert.That(restored.State.Position, Is.EqualTo(original.State.Position));
        }

        [Test]
        public void CommandResultsExposeStableRejectionDataAndImmutableEvents()
        {
            var eventData = new DomainEvent(new EntityId(8), new ContentId("command:accepted"), new Tick(4), new[] { new EntityId(2) });
            var accepted = CommandResult.Accept(new[] { eventData });
            var rejected = CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("command:capacity-reached"), "Capacity is full.") });

            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.Events, Is.EqualTo(new[] { eventData }));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Rejections[0].Code, Is.EqualTo(new ContentId("command:capacity-reached")));
            Assert.That(rejected.Events, Is.Empty);
        }

        [Test]
        public void EventsSortDeterministicallyByTickThenEventId()
        {
            var events = new List<DomainEvent>
            {
                new DomainEvent(new EntityId(3), new ContentId("event:third"), new Tick(9)),
                new DomainEvent(new EntityId(2), new ContentId("event:second"), new Tick(4)),
                new DomainEvent(new EntityId(1), new ContentId("event:first"), new Tick(4))
            };

            events.Sort();

            Assert.That(events.ConvertAll(domainEvent => domainEvent.EventId), Is.EqualTo(new[] { new EntityId(1), new EntityId(2), new EntityId(3) }));
        }
    }
}
