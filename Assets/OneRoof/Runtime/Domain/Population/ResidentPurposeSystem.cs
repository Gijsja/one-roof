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

            switch (person.CurrentActivity)
            {
                case ActivityKind.Working:
                    person.CommitPurpose(person.CurrentLocation.IsOutside
                        ? ResidentPurposeKind.WorkingOutside : ResidentPurposeKind.WorkingInside,
                        tick, person.CurrentLocation.IsOutside ? OutsideWorkTicks : HomeActivityTicks);
                    return;
                case ActivityKind.Eating:
                    person.CommitPurpose(ResidentPurposeKind.EatingAtDiner, tick, MealTicks);
                    return;
                case ActivityKind.Sleeping:
                    person.CommitPurpose(ResidentPurposeKind.Sleeping, tick, HomeActivityTicks);
                    return;
                case ActivityKind.Leisure:
                case ActivityKind.Idle:
                    if (person.CurrentActivity == ActivityKind.Idle &&
                        person.Schedule.ActiveLabelAt(tick) != DailySchedule.LabelLeisure)
                        return;
                    var isHome = !person.CurrentLocation.IsOutside &&
                                 person.CurrentRoomId.Equals(person.HomeRoomId);
                    var episode = tick.Value / HomeActivityTicks;
                    var variant = (int)((person.Id.Value + episode) % 4);
                    var purpose = isHome
                        ? variant == 0 ? ResidentPurposeKind.Sitting
                            : variant == 1 ? ResidentPurposeKind.Reading
                            : variant == 2 ? ResidentPurposeKind.Learning
                            : ResidentPurposeKind.Chilling
                        : ResidentPurposeKind.Socializing;
                    person.CommitPurpose(purpose, tick, HomeActivityTicks);
                    return;
            }
        }
    }
}
