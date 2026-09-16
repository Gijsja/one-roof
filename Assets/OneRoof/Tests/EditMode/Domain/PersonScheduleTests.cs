using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class PersonScheduleTests
    {
        // ── ScheduleBlock validation ──────────────────────────────────────────

        [Test]
        public void ScheduleBlockWithEndEqualToStartThrows()
        {
            Assert.That(
                () => new ScheduleBlock("Sleep", new Tick(100), new Tick(100)),
                Throws.ArgumentException);
        }

        [Test]
        public void ScheduleBlockWithEndBeforeStartThrows()
        {
            Assert.That(
                () => new ScheduleBlock("Sleep", new Tick(200), new Tick(100)),
                Throws.ArgumentException);
        }

        [Test]
        public void ScheduleBlockWithEmptyLabelThrows()
        {
            Assert.That(
                () => new ScheduleBlock("", new Tick(0), new Tick(100)),
                Throws.ArgumentException);
        }

        [Test]
        public void ScheduleBlockContainsReturnsTrueForTickInRange()
        {
            var block = new ScheduleBlock("Work", new Tick(480), new Tick(960));

            Assert.That(block.Contains(new Tick(480)), Is.True,  "StartTick should be included.");
            Assert.That(block.Contains(new Tick(720)), Is.True,  "Mid-range tick should be included.");
            Assert.That(block.Contains(new Tick(959)), Is.True,  "Last tick before EndTick should be included.");
            Assert.That(block.Contains(new Tick(960)), Is.False, "EndTick is exclusive.");
        }

        // ── DailySchedule ─────────────────────────────────────────────────────

        [Test]
        public void StandardScheduleCoversExactlyOneFullDay()
        {
            var rng   = new DeterministicRandomStream(42);
            var trait = new PersonTrait(PersonTraitKind.EarlyBird);
            var sched = DailySchedule.Standard(trait, rng);

            Assert.That(sched.Blocks.Count, Is.EqualTo(4));

            // First block starts at 0
            Assert.That(sched.Blocks[0].StartTick.Value, Is.EqualTo(0));

            // Each block's end equals the next block's start
            for (var i = 0; i < sched.Blocks.Count - 1; i++)
            {
                Assert.That(
                    sched.Blocks[i].EndTick.Value,
                    Is.EqualTo(sched.Blocks[i + 1].StartTick.Value),
                    $"Block {i} end should equal block {i + 1} start.");
            }

            // Last block ends at TicksPerDay
            Assert.That(sched.Blocks[sched.Blocks.Count - 1].EndTick.Value,
                Is.EqualTo(DailySchedule.TicksPerDay));
        }

        [Test]
        public void ActiveLabelAtReturnsCorrectBlockLabel()
        {
            // Build a simple 3-block schedule manually to get deterministic boundaries.
            var blocks = new List<ScheduleBlock>
            {
                new ScheduleBlock("Sleep",   new Tick(0),    new Tick(480)),
                new ScheduleBlock("Work",    new Tick(480),  new Tick(960)),
                new ScheduleBlock("Leisure", new Tick(960),  new Tick(DailySchedule.TicksPerDay)),
            };
            var sched = DailySchedule.FromBlocks(blocks);

            Assert.That(sched.ActiveLabelAt(new Tick(0)),    Is.EqualTo("Sleep"));
            Assert.That(sched.ActiveLabelAt(new Tick(479)),  Is.EqualTo("Sleep"));
            Assert.That(sched.ActiveLabelAt(new Tick(480)),  Is.EqualTo("Work"));
            Assert.That(sched.ActiveLabelAt(new Tick(959)),  Is.EqualTo("Work"));
            Assert.That(sched.ActiveLabelAt(new Tick(960)),  Is.EqualTo("Leisure"));
            Assert.That(sched.ActiveLabelAt(new Tick(1439)), Is.EqualTo("Leisure"));
        }

        [Test]
        public void ActiveLabelAtWrapsCorrectlyForTicksBeyondOneDay()
        {
            var blocks = new List<ScheduleBlock>
            {
                new ScheduleBlock("Sleep",   new Tick(0),   new Tick(480)),
                new ScheduleBlock("Work",    new Tick(480), new Tick(960)),
                new ScheduleBlock("Leisure", new Tick(960), new Tick(DailySchedule.TicksPerDay)),
            };
            var sched = DailySchedule.FromBlocks(blocks);

            // Tick 1440 wraps to tick 0 → Sleep
            Assert.That(sched.ActiveLabelAt(new Tick(DailySchedule.TicksPerDay)), Is.EqualTo("Sleep"));
            // Tick 1920 wraps to tick 480 → Work
            Assert.That(sched.ActiveLabelAt(new Tick(DailySchedule.TicksPerDay + 480)), Is.EqualTo("Work"));
        }

        // ── NeedState ─────────────────────────────────────────────────────────

        [Test]
        public void NeedStateSatisfactionBelowZeroThrows()
        {
            Assert.That(
                () => new NeedState(NeedKind.Hunger, -0.01f),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NeedStateSatisfactionAboveOneThrows()
        {
            Assert.That(
                () => new NeedState(NeedKind.Rest, 1.01f),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NeedStateWithSatisfactionReturnsNewStructWithSameKind()
        {
            var original = new NeedState(NeedKind.Social, 0.8f);
            var updated  = original.WithSatisfaction(0.3f);

            Assert.That(updated.Kind, Is.EqualTo(NeedKind.Social));
            Assert.That(updated.Satisfaction, Is.EqualTo(0.3f).Within(0.001f));
            // Original is unchanged (struct value semantics).
            Assert.That(original.Satisfaction, Is.EqualTo(0.8f).Within(0.001f));
        }
    }
}
