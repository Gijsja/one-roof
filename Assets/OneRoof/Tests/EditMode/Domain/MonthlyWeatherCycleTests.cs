using System;
using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Weather;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class MonthlyWeatherCycleTests
    {
        [Test]
        public void MonthNumber_And_DayOfMonth_CalculateCorrectly()
        {
            Assert.That(MonthlyWeatherCycle.MonthNumberFromTick(0), Is.EqualTo(1));
            Assert.That(MonthlyWeatherCycle.DayOfMonthFromTick(0), Is.EqualTo(1));

            // End of day 1
            Assert.That(MonthlyWeatherCycle.MonthNumberFromTick(1439), Is.EqualTo(1));
            Assert.That(MonthlyWeatherCycle.DayOfMonthFromTick(1439), Is.EqualTo(1));

            // Start of day 2
            Assert.That(MonthlyWeatherCycle.MonthNumberFromTick(1440), Is.EqualTo(1));
            Assert.That(MonthlyWeatherCycle.DayOfMonthFromTick(1440), Is.EqualTo(2));

            // Day 30 of month 1
            var day30Tick = 29 * MonthlyWeatherCycle.TicksPerDay + 500;
            Assert.That(MonthlyWeatherCycle.MonthNumberFromTick(day30Tick), Is.EqualTo(1));
            Assert.That(MonthlyWeatherCycle.DayOfMonthFromTick(day30Tick), Is.EqualTo(30));

            // Day 1 of month 2 (tick 43200)
            var month2Tick = MonthlyWeatherCycle.TicksPerMonth;
            Assert.That(MonthlyWeatherCycle.MonthNumberFromTick(month2Tick), Is.EqualTo(2));
            Assert.That(MonthlyWeatherCycle.DayOfMonthFromTick(month2Tick), Is.EqualTo(1));
        }

        [Test]
        public void MonthlyCycle_IsDeterministic_AcrossIdenticalSeeds()
        {
            var cycle1 = new MonthlyWeatherCycle(1234567UL);
            var cycle2 = new MonthlyWeatherCycle(1234567UL);

            var events1 = cycle1.GetMonthlyEvents(1);
            var events2 = cycle2.GetMonthlyEvents(1);

            Assert.That(events1.Count, Is.EqualTo(events2.Count));
            for (var i = 0; i < events1.Count; i++)
            {
                Assert.That(events1[i].Condition, Is.EqualTo(events2[i].Condition));
                Assert.That(events1[i].StartTick, Is.EqualTo(events2[i].StartTick));
                Assert.That(events1[i].EndTick, Is.EqualTo(events2[i].EndTick));
                Assert.That(events1[i].PeakIntensity, Is.EqualTo(events2[i].PeakIntensity).Within(0.0001f));
            }

            // Verify sampling is identical at random points
            Assert.That(cycle1.Sample(500), Is.EqualTo(cycle2.Sample(500)));
            Assert.That(cycle1.Sample(15000), Is.EqualTo(cycle2.Sample(15000)));
            Assert.That(cycle1.Sample(40000), Is.EqualTo(cycle2.Sample(40000)));
        }

        [Test]
        public void MonthlyCycle_ProducesDiverseWeather_AcrossFullMonth()
        {
            var cycle = new MonthlyWeatherCycle(0xCAFE_BABE_9999UL);
            var events = cycle.GetMonthlyEvents(1);

            // Month must be fully covered
            Assert.That(events.Count, Is.GreaterThan(10), "Month should have multiple distinct weather fronts.");
            Assert.That(events[0].StartTick, Is.EqualTo(0));
            Assert.That(events[^1].EndTick, Is.EqualTo(MonthlyWeatherCycle.TicksPerMonth));

            // Events must be contiguous with zero gaps
            for (var i = 0; i < events.Count - 1; i++)
            {
                Assert.That(events[i].EndTick, Is.EqualTo(events[i + 1].StartTick),
                    $"Gap or overlap between event {i} and {i + 1}");
            }

            // Verify diverse weather conditions appear throughout the month
            var conditions = events.Select(e => e.Condition).Distinct().ToList();
            Assert.That(conditions, Does.Contain(WeatherCondition.Clear), "Month should have clear periods.");
            Assert.That(events.Any(e => e.Condition != WeatherCondition.Clear), Is.True,
                "Month must have precipitation events.");

            // Every event should have a positive duration
            foreach (var evt in events)
            {
                Assert.That(evt.DurationTicks, Is.GreaterThan(0));
            }
        }

        [Test]
        public void Sample_EasesIntensity_AcrossFrontBoundaries()
        {
            var cycle = new MonthlyWeatherCycle(42UL);
            var events = cycle.GetMonthlyEvents(1);

            // Find a transition between clear and rain/drizzle
            int transitionIdx = -1;
            for (var i = 0; i < events.Count - 1; i++)
            {
                if (events[i].Condition == WeatherCondition.Clear && events[i + 1].Condition != WeatherCondition.Clear)
                {
                    transitionIdx = i;
                    break;
                }
            }

            if (transitionIdx >= 0)
            {
                var boundaryTick = events[transitionIdx].EndTick;
                var sampleBefore = cycle.Sample(boundaryTick - 30);
                var sampleAtBoundary = cycle.Sample(boundaryTick);
                var sampleAfter = cycle.Sample(boundaryTick + 30);

                Assert.That(sampleBefore.Intensity, Is.LessThanOrEqualTo(sampleAtBoundary.Intensity));
                Assert.That(sampleAfter.Intensity, Is.GreaterThan(sampleAtBoundary.Intensity));
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDistinctWeatherPatterns()
        {
            var cycleA = new MonthlyWeatherCycle(1111UL);
            var cycleB = new MonthlyWeatherCycle(9999UL);

            var eventsA = cycleA.GetMonthlyEvents(1);
            var eventsB = cycleB.GetMonthlyEvents(1);

            var identical = eventsA.Count == eventsB.Count &&
                eventsA.Zip(eventsB, (a, b) => a.Condition == b.Condition && a.DurationTicks == b.DurationTicks).All(x => x);

            Assert.That(identical, Is.False, "Different world seeds must generate different weather cycles.");
        }

        [Test]
        public void NegativeTicks_SampleSafely()
        {
            var cycle = new MonthlyWeatherCycle(12345UL);
            Assert.DoesNotThrow(() =>
            {
                var sample = cycle.Sample(-100);
                Assert.That(sample.Intensity, Is.InRange(0f, 1f));
            });
        }
    }
}
