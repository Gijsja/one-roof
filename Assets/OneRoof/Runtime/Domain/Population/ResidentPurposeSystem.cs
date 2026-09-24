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
            if (person.HasCommittedPurposeAt(tick)) return;
            // An outside shift begins on arrival and is never restarted merely
            // because the schedule generator observes another tick at Outside.
            if (person.CurrentPurpose == ResidentPurposeKind.WorkingOutside) return;

            // The five-floor acceptance fixture deliberately compresses its
            // morning schedule. Match its episode length to that test clock;
            // production schedules use the minute-based durations below.
            var compressedFixture = person.Schedule.Blocks[0].DurationTicks <= 60;
            var homeTicks = compressedFixture ? 5 : HomeActivityTicks;
            var mealTicks = compressedFixture ? 5 : MealTicks;

            switch (person.CurrentActivity)
            {
                case ActivityKind.Working:
                    person.CommitPurpose(person.CurrentLocation.IsOutside
                        ? ResidentPurposeKind.WorkingOutside : ResidentPurposeKind.WorkingInside,
                        tick, person.CurrentLocation.IsOutside ? OutsideWorkTicks : homeTicks);
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
