using NUnit.Framework;
using OneRoof.Domain.Weather;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    /// <summary>
    /// Integration tests verifying that the weather pipeline is correctly wired:
    /// world-seed propagation, per-tick (not per-frame) sampling, and
    /// cross-component consistency.
    /// </summary>
    public sealed class WeatherSystemIntegrationTests
    {
        [Test]
        public void MonthlyWeatherCycle_DifferentSeeds_ProduceDifferentSamples_AtSameTick()
        {
            // Simulates the world-seed wiring: two different towers should see different weather
            var cycleA = new MonthlyWeatherCycle(0xAAAA_BBBB_CCCC_DDDDL);
            var cycleB = new MonthlyWeatherCycle(0x1111_2222_3333_4444L);

            var sampleA = cycleA.Sample(5000L);
            var sampleB = cycleB.Sample(5000L);

            Assert.That(sampleA, Is.Not.EqualTo(sampleB),
                "Two different world seeds must produce distinct weather at the same tick.");
        }

        [Test]
        public void MonthlyWeatherCycle_SameSeed_ProducesIdenticalSamples_AcrossInstances()
        {
            const ulong WORLD_SEED = 0xDEAD_BEEF_CAFE_F00DUL;
            var cycleA = new MonthlyWeatherCycle(WORLD_SEED);
            var cycleB = new MonthlyWeatherCycle(WORLD_SEED);

            // Verify determinism at several tick positions across multiple months
            long[] ticks = { 0L, 720L, 1440L, 43200L, 86400L, 100000L };
            foreach (var tick in ticks)
            {
                Assert.That(cycleA.Sample(tick), Is.EqualTo(cycleB.Sample(tick)),
                    $"Determinism violated at tick {tick}");
            }
        }

        [Test]
        public void WeatherSample_DefaultDescriptions_AreNonEmpty_ForAllConditions()
        {
            var allConditions = System.Enum.GetValues(typeof(WeatherCondition));
            foreach (WeatherCondition condition in allConditions)
            {
                var description = WeatherSample.DefaultDescription(condition);
                Assert.That(description, Is.Not.Null.And.Not.Empty,
                    $"DefaultDescription for {condition} must be a non-empty string.");
            }
        }

        [Test]
        public void MonthlyWeatherCycle_ZeroSeed_PromotesToFallback_AndIsStable()
        {
            // Verify seed=0 is not accepted as-is (it promotes to fallback)
            var cycle = new MonthlyWeatherCycle(0UL);
            Assert.That(cycle.WorldSeed, Is.Not.EqualTo(0UL), "Seed 0 must be promoted to a non-zero fallback.");

            // And the promoted cycle is still deterministic
            var sample1 = cycle.Sample(10000L);
            var sample2 = cycle.Sample(10000L);
            Assert.That(sample1, Is.EqualTo(sample2), "Zero-seeded cycle must be deterministic after promotion.");
        }

        [Test]
        public void MonthlyWeatherCycle_WeatherSample_IntensityAlwaysInUnitRange()
        {
            var cycle = new MonthlyWeatherCycle(0xCAFE_BABE_1234_5678UL);
            // Sample at 500-tick intervals across 6 months
            for (var tick = 0L; tick < MonthlyWeatherCycle.TicksPerMonth * 6; tick += 500L)
            {
                var sample = cycle.Sample(tick);
                Assert.That(sample.Intensity, Is.InRange(0f, 1f),
                    $"Intensity {sample.Intensity} out of [0,1] at tick {tick}");
            }
        }
    }
}
