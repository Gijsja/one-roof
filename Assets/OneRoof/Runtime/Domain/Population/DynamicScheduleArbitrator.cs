using System;
using OneRoof.Domain.Time;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Pure C# domain service that arbitrates a resident's destination intent by
    /// weighing circadian schedule blocks against urgent physiological and social needs (ADR-047).
    ///
    /// When critical needs are depleted (e.g. extreme hunger or exhaustion), the resident's
    /// autonomous survival decisions override rigid schedule boundaries.
    /// </summary>
    public sealed class DynamicScheduleArbitrator
    {
        public const float CriticalHungerThreshold  = 0.35f;
        public const float CriticalEnergyThreshold  = 0.25f;
        public const float CriticalHygieneThreshold = 0.25f;
        public const float LowSocialThreshold       = 0.30f;
        public const float SatisfiedNeedThreshold   = 0.85f;

        /// <summary>
        /// Evaluates if an urgent physiological or social need requires an immediate destination override.
        /// Returns a TripPurpose if an override is needed, or null if needs are within acceptable thresholds.
        /// </summary>
        public TripPurpose? ArbitrateUrgentNeed(PersonRecord person)
        {
            if (person == null) return null;
            if (person.CurrentActivity == ActivityKind.Commuting) return null;

            var hunger = person.GetNeedSatisfaction(NeedKind.Hunger);
            var energy = person.GetNeedSatisfaction(NeedKind.Energy);
            var hygiene = person.GetNeedSatisfaction(NeedKind.Hygiene);
            var social = person.GetNeedSatisfaction(NeedKind.Social);

            var isAtHome = person.CurrentRoomId.Equals(person.HomeRoomId);

            // 1. Critical exhaustion override: must return home to rest
            if (energy < CriticalEnergyThreshold)
            {
                if (!isAtHome)
                {
                    return TripPurpose.Home;
                }

                if (person.CurrentActivity != ActivityKind.Sleeping)
                {
                    person.UpdateActivity(ActivityKind.Sleeping);
                }
                return null;
            }

            // 2. Critical hunger override: must seek food
            if (hunger < CriticalHungerThreshold)
            {
                if (person.CurrentActivity == ActivityKind.Eating)
                {
                    return null;
                }

                return TripPurpose.Food;
            }

            // 3. Critical hygiene override: return home to refresh
            if (hygiene < CriticalHygieneThreshold)
            {
                if (!isAtHome)
                {
                    return TripPurpose.Hygiene;
                }

                return null;
            }

            // 4. Critical social isolation
            if (social < LowSocialThreshold && person.CurrentActivity != ActivityKind.Leisure)
            {
                return TripPurpose.Leisure;
            }

            return null;
        }

        /// <summary>
        /// Evaluates destination choice at <paramref name="tick"/>.
        /// If <paramref name="isBlockTransition"/> is true, scheduled intent is evaluated if no urgent need overrides it.
        /// If <paramref name="isBlockTransition"/> is false, only urgent need overrides trigger trips.
        /// </summary>
        public TripPurpose? ArbitrateDestination(PersonRecord person, Tick tick, bool isBlockTransition = true)
        {
            if (person == null) return null;
            if (person.CurrentActivity == ActivityKind.Commuting) return null;

            var urgent = ArbitrateUrgentNeed(person);
            if (urgent.HasValue)
            {
                return urgent.Value;
            }

            if (!isBlockTransition)
            {
                return null;
            }

            var isAtHome = person.CurrentRoomId.Equals(person.HomeRoomId);
            var isAtWork = person.CurrentRoomId.Equals(person.WorkplaceRoomId);
            var hunger = person.GetNeedSatisfaction(NeedKind.Hunger);
            var social = person.GetNeedSatisfaction(NeedKind.Social);

            var activeLabel = person.Schedule.ActiveLabelAt(tick);

            switch (activeLabel)
            {
                case DailySchedule.LabelSleep:
                    if (!isAtHome)
                    {
                        return TripPurpose.Home;
                    }
                    if (person.CurrentActivity != ActivityKind.Sleeping)
                    {
                        person.UpdateActivity(ActivityKind.Sleeping);
                    }
                    return null;

                case DailySchedule.LabelWork:
                    if (!isAtWork)
                    {
                        return TripPurpose.Work;
                    }
                    if (person.CurrentActivity != ActivityKind.Working)
                    {
                        person.UpdateActivity(ActivityKind.Working);
                    }
                    return null;

                case DailySchedule.LabelEat:
                    if (person.CurrentActivity == ActivityKind.Eating)
                    {
                        if (hunger >= SatisfiedNeedThreshold)
                        {
                            return TripPurpose.Home;
                        }
                        return null;
                    }

                    if (hunger < SatisfiedNeedThreshold)
                    {
                        return TripPurpose.Food;
                    }
                    return null;

                case DailySchedule.LabelLeisure:
                default:
                    if (social < LowSocialThreshold && person.CurrentActivity != ActivityKind.Leisure)
                    {
                        return TripPurpose.Leisure;
                    }

                    return null;
            }
        }
    }
}
