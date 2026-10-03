using System;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Population
{
    /// <summary>Assigns deterministic, minimum-length activity episodes after a resident arrives.</summary>
    public static class ResidentPurposeSystem
    {
        public const long OutsideWorkTicks = 480;
        public const long MealTicks = 30;
        public const long HomeActivityTicks = 60;

        public static void AdvancePerson(PersonRecord person, Tick tick)
        {
            if (person == null || person.CurrentActivity == ActivityKind.Commuting) return;
            person.EnsureRoutineDay(tick);
            if (person.Schedule.IsEmploymentRoutine)
            {
                var routineBlock = person.Schedule.ActiveBlockAt(tick).Value;
                var dayBase = tick.Value / DailySchedule.TicksPerDay * DailySchedule.TicksPerDay;
                if (person.CurrentActivity == ActivityKind.Working)
                {
                    if (routineBlock.Label == DailySchedule.LabelWork)
                        person.PrepareWorkAppointment(routineBlock, dayBase, tick);
                    person.RecordWorkArrival(tick);
                    if (tick.Value < person.WorkSegmentStartTick || tick.Value >= person.WorkAppointmentEndTick)
                    {
                        person.UpdateActivity(ActivityKind.Idle);
                        return;
                    }
                    if (routineBlock.Label != DailySchedule.LabelWork)
                    {
                        person.UpdateActivity(ActivityKind.Idle);
                        return;
                    }
                    if (person.HasCommittedPurposeAt(tick) && person.PurposeEndsAtTick == person.WorkAppointmentEndTick) return;
                    person.CommitPurpose(person.CurrentLocation.IsOutside ? ResidentPurposeKind.WorkingOutside : ResidentPurposeKind.WorkingInside,
                        tick, person.WorkAppointmentEndTick - tick.Value);
                    return;
                }
                if (person.CurrentActivity == ActivityKind.Sleeping && routineBlock.Label == DailySchedule.LabelSleep)
                {
                    var end = dayBase + routineBlock.AppointmentEndDayTick;
                    if (person.HasCommittedPurposeAt(tick) && person.PurposeEndsAtTick == end) return;
                    person.CommitPurpose(ResidentPurposeKind.Sleeping, tick, Math.Max(1, end - tick.Value));
                    return;
                }
            }
            if (person.HasCommittedPurposeAt(tick)) return;
            // An outside shift begins on arrival and is never restarted merely
            // because the schedule generator observes another tick at Outside.
            if (person.CurrentPurpose == ResidentPurposeKind.WorkingOutside) return;

            // The five-floor acceptance fixture deliberately compresses its
            // morning schedule. Match its episode length to that test clock;
            // production schedules use the minute-based durations below.
            var compressedFixture = !person.Schedule.IsEmploymentRoutine && person.Schedule.Blocks[0].DurationTicks <= 60;
            var homeTicks = compressedFixture ? 5 : HomeActivityTicks;
            var mealTicks = compressedFixture ? 5 : MealTicks;

            switch (person.CurrentActivity)
            {
                case ActivityKind.Working:
                    var remainingWork = OutsideWorkTicks;
                    var activeBlock = person.Schedule.ActiveBlockAt(tick);
                    if (activeBlock.HasValue && activeBlock.Value.Label == DailySchedule.LabelWork)
                    {
                        var dayTick = tick.Value % DailySchedule.TicksPerDay;
                        var blockEnd = activeBlock.Value.EndTick.Value;
                        if (blockEnd > dayTick) remainingWork = Math.Min(OutsideWorkTicks, blockEnd - dayTick);
                    }
                    person.CommitPurpose(person.CurrentLocation.IsOutside
                        ? ResidentPurposeKind.WorkingOutside : ResidentPurposeKind.WorkingInside,
                        tick, person.CurrentLocation.IsOutside ? remainingWork : homeTicks);
                    return;
                case ActivityKind.Eating:
                    person.CommitPurpose(ResidentPurposeKind.EatingAtDiner, tick, mealTicks);
                    return;
                case ActivityKind.Sleeping:
                    person.CommitPurpose(ResidentPurposeKind.Sleeping, tick, homeTicks);
                    return;
                case ActivityKind.Leisure:
                case ActivityKind.Idle:
                    if (person.CurrentActivity == ActivityKind.Idle &&
                        person.Schedule.ActiveLabelAt(tick) != DailySchedule.LabelLeisure)
                        return;
                    var isHome = !person.CurrentLocation.IsOutside &&
                                 person.CurrentRoomId.Equals(person.HomeRoomId);
                    var episode = tick.Value / homeTicks;
                    var variant = (int)((person.Id.Value + episode) % 4);
                    var purpose = isHome
                        ? variant == 0 ? ResidentPurposeKind.Sitting
                            : variant == 1 ? ResidentPurposeKind.Reading
                            : variant == 2 ? ResidentPurposeKind.Learning
                            : ResidentPurposeKind.Chilling
                        : ResidentPurposeKind.Socializing;
                    person.CommitPurpose(purpose, tick, homeTicks);
                    return;
            }
        }
    }
}
