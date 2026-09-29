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
                        // Within the 45-tick transition window, no sample should be Clear
                        // unless the intensity is legitimately near zero
                        var boundary = next.StartTick;
                        for (var t = 1; t <= 44; t++)
                        {
                            var sample = cycle.Sample(boundary + t);
                            // Once we are 10+ ticks into a Rain/Storm front, condition must not flip back to Clear
                            if (t >= 10 && sample.Intensity > 0.05f)
                            {
                                Assert.That(sample.Condition, Is.Not.EqualTo(WeatherCondition.Clear),
                                    $"Flicker at tick {boundary + t} (month {month}, event {i}→{i+1})");
                            }
                        }
                        foundTransition = true;
                        break;
                    }
                }
                if (foundTransition) break;
            }

            if (!foundTransition)
            {
                Assert.Ignore("No Clear→Rain/Storm transition found in the tested seed — skip.");
            }
        }

        [Test]
        public void GenerateMonthEvents_CoversFogAndSnow_AsValidStates()
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
        public void WeatherSample_IsReducedVisibility_ForFogAndSnow()
        {
            var fogSample = new WeatherSample(WeatherCondition.Fog, 0.5f, 0f);
            Assert.That(fogSample.IsReducedVisibility, Is.True, "Fog must flag reduced visibility");

            var snowSample = new WeatherSample(WeatherCondition.Snow, 0.5f, 0f);
            Assert.That(snowSample.IsReducedVisibility, Is.True, "Snow must flag reduced visibility");

            var clearSample = new WeatherSample(WeatherCondition.Clear, 0f, 0f);
            Assert.That(clearSample.IsReducedVisibility, Is.False, "Clear must not flag reduced visibility");

            var mildRainSample = new WeatherSample(WeatherCondition.Rain, 0.5f, -0.5f);
            Assert.That(mildRainSample.IsReducedVisibility, Is.False, "Rain below 0.7 intensity must not flag reduced visibility");

            var severeStormSample = new WeatherSample(WeatherCondition.Storm, 0.9f, -2f);
            Assert.That(severeStormSample.IsReducedVisibility, Is.True, "Storm > 0.7 must flag reduced visibility");
        }
    }
}
