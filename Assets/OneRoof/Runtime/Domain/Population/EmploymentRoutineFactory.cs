using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Population
{
    public enum EmploymentCohort { Legacy, Early, Day, Late, Overnight }

    /// <summary>Shared production/fixture routines. Jobs, identity and seed determine obligations; transit never does.</summary>
    public static class EmploymentRoutineFactory
    {
        public static DailySchedule Create(EntityId residentId, PersonTrait trait, string workplaceContent, uint seed)
        {
            residentId.EnsureValid();
            var content = workplaceContent ?? string.Empty;
            EmploymentCohort[] cohorts;
            if (content.StartsWith("service:clinic", StringComparison.Ordinal) ||
                content.StartsWith("service:security", StringComparison.Ordinal) ||
                content.StartsWith("service:maintenance", StringComparison.Ordinal))
                cohorts = new[] { EmploymentCohort.Early, EmploymentCohort.Day, EmploymentCohort.Late, EmploymentCohort.Overnight };
            else if (content.Contains("diner"))
                cohorts = new[] { EmploymentCohort.Early, EmploymentCohort.Late };
            else if (content.Contains("office")) cohorts = new[] { EmploymentCohort.Day };
            else cohorts = new[] { EmploymentCohort.Early, EmploymentCohort.Day, EmploymentCohort.Late };
            var cohort = cohorts[residentId.Value % cohorts.Length];
            var mixedSeed = Mix(seed ^ (uint)residentId.Value);
            var offset = (long)(mixedSeed % 31) - 15;
            if (trait.Kind == PersonTraitKind.EarlyBird) offset -= 15;
            else if (trait.Kind == PersonTraitKind.NightOwl) offset += 15;
            var start = (cohort == EmploymentCohort.Early ? 360 : cohort == EmploymentCohort.Day ? 540 :
                cohort == EmploymentCohort.Late ? 840 : 1320) + offset;
            var blocks = new List<ScheduleBlock>();
            Add(blocks, DailySchedule.LabelWork, start, start + 240, start, start, start + 510);
            Add(blocks, DailySchedule.LabelEat, start + 240, start + 270);
            // Lunch return is a separate attendance episode inside the same eight-hour appointment.
            Add(blocks, DailySchedule.LabelWork, start + 270, start + 510, start + 270, start, start + 510);
            Add(blocks, DailySchedule.LabelHome, start + 510, start + 600);
            Add(blocks, DailySchedule.LabelEat, start + 600, start + 630);
            Add(blocks, DailySchedule.LabelLeisure, start + 630, start + 720);
            Add(blocks, DailySchedule.LabelHome, start + 720, start + 900);
            Add(blocks, DailySchedule.LabelSleep, start + 900, start + 1380);
            Add(blocks, DailySchedule.LabelPreparation, start + 1380, start + 1410);
            Add(blocks, DailySchedule.LabelEat, start + 1410, start + 1440);
            blocks.Sort((a, b) => a.StartTick.CompareTo(b.StartTick));
            return DailySchedule.FromBlocks(blocks, cohort, seed);
        }

        private static void Add(List<ScheduleBlock> blocks, string label, long start, long end,
            long? episode = null, long? appointmentStart = null, long? appointmentEnd = null)
        {
            var day = start / DailySchedule.TicksPerDay;
            var localStart = start - day * DailySchedule.TicksPerDay;
            var localEnd = end - day * DailySchedule.TicksPerDay;
            var logicalStart = (episode ?? start) - day * DailySchedule.TicksPerDay;
            var logicalAppointment = (appointmentStart ?? start) - day * DailySchedule.TicksPerDay;
            var logicalEnd = (appointmentEnd ?? end) - day * DailySchedule.TicksPerDay;
            blocks.Add(new ScheduleBlock(label, new Tick(localStart), new Tick(Math.Min(1440, localEnd)),
                logicalStart, logicalAppointment, logicalEnd));
            if (localEnd > 1440)
                blocks.Add(new ScheduleBlock(label, new Tick(0), new Tick(localEnd - 1440),
                    logicalStart - 1440, logicalAppointment - 1440, logicalEnd - 1440));
        }

        /// <summary>Initial snapshot reserves follow time since sleep/meals, rather than fixture-wide fresh reserves.</summary>
        public static NeedState[] InitialNeeds(DailySchedule schedule, Tick tick)
        {
            var now = tick.Value % 1440;
            long sinceMeal = 1440, sinceSleep = 1440;
            foreach (var block in schedule.Blocks)
            {
                var elapsed = (now - block.EndTick.Value + 1440) % 1440;
                if (block.Label == DailySchedule.LabelEat) sinceMeal = Math.Min(sinceMeal, elapsed);
                if (block.Label == DailySchedule.LabelSleep) sinceSleep = Math.Min(sinceSleep, elapsed);
            }
            var active = schedule.ActiveBlockAt(tick).Value;
            var sleeping = active.Label == DailySchedule.LabelSleep;
            var sleepElapsed = now - active.EpisodeStartDayTick;
            var energy = sleeping ? Math.Min(1f, .2f + sleepElapsed * ResidentNeedsSystem.SleepEnergyRecoveryRate)
                : Math.Max(0f, 1f - sinceSleep * ResidentNeedsSystem.DefaultEnergyDecayRate);
            return new[] {
                new NeedState(NeedKind.Hunger, Math.Max(0f, .9f - sinceMeal * ResidentNeedsSystem.DefaultHungerDecayRate)),
                new NeedState(NeedKind.Energy, energy),
                new NeedState(NeedKind.Social, Math.Max(0f, .9f - sinceSleep * ResidentNeedsSystem.DefaultSocialDecayRate * .5f)),
                new NeedState(NeedKind.Hygiene, sleeping ? 1f : Math.Max(0f, 1f - sinceSleep * ResidentNeedsSystem.DefaultHygieneDecayRate)),
                new NeedState(NeedKind.Purpose, active.Label == DailySchedule.LabelWork ? 1f : .8f) };
        }

        internal static uint Mix(uint value)
        {
            unchecked { value ^= value >> 16; value *= 0x7feb352d; value ^= value >> 15; value *= 0x846ca68b; return value ^ (value >> 16); }
        }
    }
}
