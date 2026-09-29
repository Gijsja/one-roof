using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    /// <summary>Measures the complete deterministic tick with 30 floors, 300 residents, and an operating district.</summary>
    public sealed class UndergroundTickBudgetTests
    {
        [Test]
        [Category("Performance")]
        [Explicit("Run on the reference machine for a 300-resident tick budget measurement.")]
        public void ThirtyFloors_ThreeHundredResidents_FourteenUndergroundRooms_P95TickStaysUnderFourMilliseconds()
        {
            var sim = CreateLoadedSimulation();
            Assert.That(sim.ResidentCount, Is.EqualTo(300));
            Assert.That(sim.Topology.FloorCount, Is.EqualTo(30));
            Assert.That(sim.Underground.Rooms.Count, Is.EqualTo(14));

            // Warm JIT and caches before timing. The sample crosses a daily settlement
            // so underground staffing, supplies and investigator work are included.
            for (var i = 0; i < 100; i++) sim.AdvanceOneTick();
            var samples = new long[1500];
            var tickModTen = new byte[samples.Length];
            for (var i = 0; i < samples.Length; i++)
            {
                var start = Stopwatch.GetTimestamp();
                sim.AdvanceOneTick();
                samples[i] = Stopwatch.GetTimestamp() - start;
                tickModTen[i] = (byte)(sim.CurrentTick % 10);
            }
            var leasingTicks = new long[150];
            var otherTicks = new long[1350];
            var leasingCount = 0;
            var otherCount = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                if (tickModTen[i] == 0) leasingTicks[leasingCount++] = samples[i];
                else otherTicks[otherCount++] = samples[i];
            }
            Array.Sort(leasingTicks);
            Array.Sort(otherTicks);
            Array.Sort(samples);
            var p50 = Milliseconds(samples[samples.Length / 2]);
            var p95 = Milliseconds(samples[(int)Math.Ceiling(samples.Length * .95) - 1]);
            var worst = Milliseconds(samples[samples.Length - 1]);
            TestContext.WriteLine($"30 floors, 300 residents, 14 rooms, 1500 ticks: p50={p50:F3} ms, p95={p95:F3} ms, worst={worst:F3} ms");
            TestContext.WriteLine($"Leasing ticks p95={Milliseconds(leasingTicks[(int)Math.Ceiling(leasingCount * .95) - 1]):F3} ms; other ticks p95={Milliseconds(otherTicks[(int)Math.Ceiling(otherCount * .95) - 1]):F3} ms");
            Assert.That(sim.CurrentTick, Is.EqualTo(1600));
            Assert.That(p95, Is.LessThan(4.0), "The 300-resident deterministic tick must meet the 4 ms p95 budget.");
        }

        private static double Milliseconds(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        private static TowerSimulation CreateLoadedSimulation()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(new TowerEconomyState(1_000_000));
            for (var floor = 5; floor < 30; floor++)
                AssertAccepted(sim.ExecuteCommand(new BuildFloorSlabCommand(floor, -14, 17)));
            var template = FiftyResidentFixture.Create();
            for (var batch = 1; batch < 6; batch++)
            {
                var offset = batch * 10000;
                foreach (var original in template.Households)
                {
                    var members = new List<EntityId>(original.MemberIds.Count);
                    foreach (var member in original.MemberIds) members.Add(new EntityId(member.Value + offset));
                    sim.Population.AddHousehold(new HouseholdRecord(new EntityId(original.Id.Value + offset),
                        members, original.HomeRoomId, original.Budget, original.Satisfaction, original.CashBalance));
                }
                foreach (var original in template.Persons)
                    sim.Population.AddPerson(new PersonRecord(new EntityId(original.Id.Value + offset),
                        new EntityId(original.HouseholdId.Value + offset), original.HomeRoomId, original.WorkplaceRoomId,
                        original.Schedule, original.Needs, original.Traits));
            }

            // Fill the four 3-cell basement bands using the existing square brushes.
            for (var depth = 0; depth < 12; depth += 3)
            {
                for (var x = 0; x < 32; x += 3)
                {
                    var size = Math.Min(3, 32 - x);
                    AssertAccepted(sim.ExecuteCommand(new DigUndergroundCommand(x, depth, size)));
                    AssertAccepted(sim.ExecuteCommand(new BuildUndergroundFloorCommand(x, depth, size)));
                }
                for (var x = 30; x < 32; x++)
                {
                    AssertAccepted(sim.ExecuteCommand(new DigUndergroundCommand(x, depth + 2, 1)));
                    AssertAccepted(sim.ExecuteCommand(new BuildUndergroundFloorCommand(x, depth + 2, 1)));
                }
            }
            AssertAccepted(sim.ExecuteCommand(new BuildUndergroundCoreCommand(16, 11)));
            for (var depth = 2; depth < 12; depth += 3)
            {
                AssertAccepted(sim.ExecuteCommand(new BuildUndergroundCorridorCommand(0, depth, 16)));
                AssertAccepted(sim.ExecuteCommand(new BuildUndergroundCorridorCommand(17, depth, 15)));
            }
            var roomIndex = 0;
            for (var depth = 0; depth < 12 && roomIndex < 14; depth += 3)
            foreach (var x in new[] { 3, 7, 11, 18, 22, 26 })
            {
                if (roomIndex >= 14) break;
                AssertAccepted(sim.ExecuteCommand(new ZoneUndergroundRoomCommand((UndergroundRoomType)roomIndex++, x, depth, 3, 2)));
            }
            return sim;
        }

        private static void AssertAccepted(CommandResult result) =>
            Assert.That(result.Accepted, Is.True, result.Reason);
    }
}
