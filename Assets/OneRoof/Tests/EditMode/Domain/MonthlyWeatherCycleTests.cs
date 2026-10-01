using System;
using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Weather;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class MonthlyWeatherCycleTests
    {
        [TestCase(0L, 1, 1)]
        [TestCase(1439L, 1, 1)]
        [TestCase(1440L, 1, 2)]
        [TestCase(42260L, 1, 30)]
        [TestCase(43200L, 2, 1)]
        public void CalendarPosition_FromTick(long tick, int expectedMonth, int expectedDay)
        {
            Assert.That(MonthlyWeatherCycle.MonthNumberFromTick(tick), Is.EqualTo(expectedMonth));
            Assert.That(MonthlyWeatherCycle.DayOfMonthFromTick(tick), Is.EqualTo(expectedDay));
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

            Assert.That(transitionIdx, Is.GreaterThanOrEqualTo(0),
                "The fixed weather seed must provide a clear-to-front boundary.");
            var boundaryTick = events[transitionIdx].EndTick;
            var sampleBefore = cycle.Sample(boundaryTick - 30);
            var sampleAtBoundary = cycle.Sample(boundaryTick);
            var sampleAfter = cycle.Sample(boundaryTick + 30);

            Assert.That(sampleBefore.Intensity, Is.LessThanOrEqualTo(sampleAtBoundary.Intensity));
            Assert.That(sampleAfter.Intensity, Is.GreaterThan(sampleAtBoundary.Intensity));
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

        [Test]
        public void Sample_DoesNotFlickerClear_DuringRainTransition()
        {
            // Scan across all months until we find a Clear-to-Rain boundary
            var cycle = new MonthlyWeatherCycle(0xABCDEF123456UL);
            var foundTransition = false;

            for (var month = 1; month <= 12; month++)
            {
                var events = cycle.GetMonthlyEvents(month);
                for (var i = 0; i < events.Count - 1; i++)
                {
                    var cur = events[i];
                    var next = events[i + 1];
                    if (cur.Condition == WeatherCondition.Clear &&
                        (next.Condition == WeatherCondition.Rain || next.Condition == WeatherCondition.Storm))
                    {
                        // A clear-to-rain transition must keep the incoming condition,
                        // including its initially near-zero intensity.
                        var boundary = next.StartTick;
                        for (var t = 1; t <= 44; t++)
                        {
                            var sample = cycle.Sample(boundary + t);
                            Assert.That(sample.Condition, Is.EqualTo(next.Condition),
                                $"Flicker at tick {boundary + t} (month {month}, event {i}→{i+1})");
                            if (t == 44)
                                Assert.That(sample.Intensity, Is.GreaterThan(0.05f), "The front must actually intensify.");
                        }
                        foundTransition = true;
                        break;
                    }
                }
                if (foundTransition) break;
            }

            Assert.That(foundTransition, Is.True,
                "The fixed weather seed must provide a clear-to-rain/storm transition.");
        }

        [Test]
        public void GenerateMonthEvents_ProducesValidConditionsDurationsAndIntensities()
        {
            // Scan across 12 months; all produced WeatherConditions must be valid enum values
            var cycle = new MonthlyWeatherCycle(0xFAB15EED_C0FEUL);
            var validConditions = new System.Collections.Generic.HashSet<WeatherCondition>
            {
                WeatherCondition.Clear, WeatherCondition.Drizzle, WeatherCondition.Rain,
                WeatherCondition.Storm, WeatherCondition.Fog, WeatherCondition.Snow
            };

            for (var month = 1; month <= 12; month++)
            {
                var events = cycle.GetMonthlyEvents(month);
                foreach (var evt in events)
                {
                    Assert.That(validConditions, Does.Contain(evt.Condition),
                        $"Unexpected condition {evt.Condition} in month {month}");
                    Assert.That(evt.DurationTicks, Is.GreaterThan(0), $"Zero-duration event in month {month}");
                    Assert.That(evt.PeakIntensity, Is.InRange(0f, 1f), $"Intensity out of range in month {month}");
                }
            }
        }

        [TestCase(WeatherCondition.Fog)]
        [TestCase(WeatherCondition.Snow)]
        public void GeneratedYear_ContainsAndSamplesVisibilityReducingFronts(WeatherCondition condition)
        {
            var cycle = new MonthlyWeatherCycle(0xFAB15EED_C0FEUL);
            var fronts = Enumerable.Range(1, 12)
                .SelectMany(month => cycle.GetMonthlyEvents(month).ToArray())
                .Where(front => front.Condition == condition).ToArray();
            Assert.That(fronts, Is.Not.Empty, $"The fixed yearly fixture must produce {condition}.");
            foreach (var front in fronts)
            {
                var sample = cycle.Sample(front.StartTick + front.DurationTicks / 2);
                Assert.That(sample.Condition, Is.EqualTo(condition), $"Front at {front.StartTick}");
                Assert.That(sample.IsReducedVisibility, Is.True);
            }
        }

        [Test]
        public void MonthlyEvents_AreContiguous_AcrossAllTwelveMonths()
        {
            var cycle = new MonthlyWeatherCycle(0x1234_5678_90ABUL);
            for (var month = 1; month <= 12; month++)
            {
                var events = cycle.GetMonthlyEvents(month);
                Assert.That(events.Count, Is.GreaterThan(0), $"Month {month} produced no events");
                var monthStart = (long)(month - 1) * MonthlyWeatherCycle.TicksPerMonth;
                var monthEnd   = monthStart + MonthlyWeatherCycle.TicksPerMonth;
                Assert.That(events[0].StartTick, Is.EqualTo(monthStart),
                    $"Month {month} first event does not start at month boundary");
                Assert.That(events[^1].EndTick, Is.EqualTo(monthEnd),
                    $"Month {month} last event does not end at month boundary");
                for (var i = 0; i < events.Count - 1; i++)
                {
                    Assert.That(events[i].EndTick, Is.EqualTo(events[i + 1].StartTick),
                        $"Gap/overlap between event {i} and {i+1} in month {month}");
                }
            }
        }

        [Test]
        public void ZeroSeed_UsesStableNonZeroFallback()
        {
            var cycle = new MonthlyWeatherCycle(0UL);
            Assert.That(cycle.WorldSeed, Is.Not.EqualTo(0UL));
            var fallback = new MonthlyWeatherCycle(cycle.WorldSeed);
            foreach (var tick in new[] { 0L, 10000L, 43200L, 86400L })
                Assert.That(cycle.Sample(tick), Is.EqualTo(fallback.Sample(tick)), $"Fallback at tick {tick}");
        }

        [TestCase(WeatherCondition.Clear)]
        [TestCase(WeatherCondition.Drizzle)]
        [TestCase(WeatherCondition.Rain)]
        [TestCase(WeatherCondition.Storm)]
        [TestCase(WeatherCondition.Fog)]
        [TestCase(WeatherCondition.Snow)]
        public void WeatherSample_DefaultDescriptionIsNonEmpty(WeatherCondition condition)
        {
            Assert.That(WeatherSample.DefaultDescription(condition), Is.Not.Null.And.Not.Empty);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void SampledIntensity_IsInUnitRangeAcrossMonth(int month)
        {
            var cycle = new MonthlyWeatherCycle(0xCAFE_BABE_1234_5678UL);
            var firstTick = (month - 1L) * MonthlyWeatherCycle.TicksPerMonth;
            for (var tick = firstTick; tick < firstTick + MonthlyWeatherCycle.TicksPerMonth; tick += 500)
                Assert.That(cycle.Sample(tick).Intensity, Is.InRange(0f, 1f), $"Sample at tick {tick}");
        }

        [TestCase(WeatherCondition.Fog, 0.5f, true)]
        [TestCase(WeatherCondition.Snow, 0.5f, true)]
        [TestCase(WeatherCondition.Clear, 0f, false)]
        [TestCase(WeatherCondition.Rain, 0.5f, false)]
        [TestCase(WeatherCondition.Storm, 0.7f, false)]
        [TestCase(WeatherCondition.Storm, 0.9f, true)]
        public void WeatherSample_VisibilityMatchesConditionAndIntensity(
            WeatherCondition condition, float intensity, bool expectedReducedVisibility)
        {
            var sample = new WeatherSample(condition, intensity, 0f);
            Assert.That(sample.IsReducedVisibility, Is.EqualTo(expectedReducedVisibility));
        }
    }
}
