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

        [TestCase(100, 100, TestName = "ScheduleBlockWithEndEqualToStartThrows")]
        [TestCase(200, 100, TestName = "ScheduleBlockWithEndBeforeStartThrows")]
        public void ScheduleBlockWithNonIncreasingEndpointsThrows(int start, int end)
        {
            Assert.That(() => new ScheduleBlock("Sleep", new Tick(start), new Tick(end)),
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

        [TestCase(0, "Sleep")]
        [TestCase(479, "Sleep")]
        [TestCase(480, "Work")]
        [TestCase(959, "Work")]
        [TestCase(960, "Leisure")]
        [TestCase(1439, "Leisure")]
        [TestCase(1440, "Sleep")]
        [TestCase(1920, "Work")]
        public void ActiveLabelAtSelectsBlockAcrossBoundariesAndDayWrapping(int tick, string expectedLabel)
        {
            var blocks = new List<ScheduleBlock>
            {
                new ScheduleBlock("Sleep", new Tick(0), new Tick(480)),
                new ScheduleBlock("Work", new Tick(480), new Tick(960)),
                new ScheduleBlock("Leisure", new Tick(960), new Tick(DailySchedule.TicksPerDay)),
            };
            var schedule = DailySchedule.FromBlocks(blocks);
            Assert.That(schedule.ActiveLabelAt(new Tick(tick)), Is.EqualTo(expectedLabel));
        }

        // ── NeedState ─────────────────────────────────────────────────────────

        [TestCase(NeedKind.Hunger, -0.01f, TestName = "NeedStateSatisfactionBelowZeroThrows")]
        [TestCase(NeedKind.Rest, 1.01f, TestName = "NeedStateSatisfactionAboveOneThrows")]
        public void NeedStateWithOutOfRangeSatisfactionThrows(NeedKind kind, float satisfaction)
        {
            Assert.That(() => new NeedState(kind, satisfaction),
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
