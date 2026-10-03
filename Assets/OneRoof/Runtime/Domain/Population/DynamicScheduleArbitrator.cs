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

            var isAtHome = person.CurrentLocation.Equals(OneRoof.Domain.Identity.WorldLocation.InRoom(person.HomeRoomId));

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
        /// If <paramref name="isBlockTransition"/> is false, only urgent need overrides, finished-meal
        /// returns, and stranded-resident catch-up trigger trips. Idle residents stay silent mid-block,
        /// so a resident who simply has no trip yet never generates demand away from a boundary.
        /// </summary>
        public TripPurpose? ArbitrateDestination(PersonRecord person, Tick tick, bool isBlockTransition = true)
        {
            if (person == null || person.CurrentActivity == ActivityKind.Commuting) return null;
            var decision = Decide(person, tick, isBlockTransition);
            return decision.Kind == RoutineDecisionKind.Travel ? decision.Purpose : null;
        }

        /// <summary>Separates required attendance from optional no-action and actual travel.</summary>
        public RoutineDecision Decide(PersonRecord person, Tick tick, bool isBlockTransition, long anticipatedDepartureTick = -1)
        {
            if (person == null) return default;
            if (person.Schedule.IsEmploymentRoutine)
                return DecideEmployment(person, tick, isBlockTransition, anticipatedDepartureTick);
            var label = person.Schedule.ActiveLabelAt(tick);
            var scheduled = ScheduledPurpose(label);
            var episode = EpisodeStartAt(person, tick);
            if (person.CurrentActivity == ActivityKind.Commuting)
                return isBlockTransition && (scheduled == TripPurpose.Work || scheduled == TripPurpose.Home)
                    ? new RoutineDecision(RoutineDecisionKind.Travel, scheduled, true, episode)
                    : default;

            var purpose = EvaluateDestination(person, tick, isBlockTransition);
            if (purpose.HasValue)
            {
                return new RoutineDecision(RoutineDecisionKind.Travel, purpose, true,
                    purpose == scheduled ? episode : person.PurposeStartedAtTick);
            }

            // Only physical attendance at a required destination establishes a local outcome.
            if (isBlockTransition && scheduled == TripPurpose.Work && person.CurrentLocation.Equals(person.WorkplaceLocation))
                return new RoutineDecision(RoutineDecisionKind.LocalActivity, TripPurpose.Work, true, episode);
            if (isBlockTransition && scheduled == TripPurpose.Home &&
                person.CurrentLocation.Equals(OneRoof.Domain.Identity.WorldLocation.InRoom(person.HomeRoomId)))
                return new RoutineDecision(RoutineDecisionKind.LocalActivity, TripPurpose.Home, true, episode);
            return new RoutineDecision(RoutineDecisionKind.None, null, false, episode);
        }

        internal static long EpisodeStartAt(PersonRecord person, Tick tick)
        {
            var block = person.Schedule.ActiveBlockAt(tick);
            var start = tick.Value / DailySchedule.TicksPerDay * DailySchedule.TicksPerDay + (block?.EpisodeStartDayTick ?? 0);
            if (block?.Label == DailySchedule.LabelWork && person.Schedule.IsEmploymentRoutine &&
                person.WorkSegmentStartTick == start) return person.WorkDemandEpisodeStartTick;
            return Math.Max(0, start);
        }

        internal static TripPurpose? ScheduledPurpose(string label)
        {
            if (label == DailySchedule.LabelWork) return TripPurpose.Work;
            if (label == DailySchedule.LabelSleep) return TripPurpose.Home;
            if (label == DailySchedule.LabelHome || label == DailySchedule.LabelPreparation) return TripPurpose.Home;
            if (label == DailySchedule.LabelEat) return TripPurpose.Food;
            if (label == DailySchedule.LabelLeisure) return TripPurpose.Leisure;
            return null;
        }

        private RoutineDecision DecideEmployment(PersonRecord person, Tick tick, bool transition, long departure)
        {
            person.EnsureRoutineDay(tick);
            var block = person.Schedule.ActiveBlockAt(tick).Value;
            var label = block.Label;
            var dayBase = tick.Value / DailySchedule.TicksPerDay * DailySchedule.TicksPerDay;
            if (label == DailySchedule.LabelWork)
                person.PrepareWorkAppointment(block, dayBase, tick);
            var episode = EpisodeStartAt(person, tick);
            var scheduled = ScheduledPurpose(label);
            if (person.CurrentActivity == ActivityKind.Commuting)
                return transition && (scheduled == TripPurpose.Work || scheduled == TripPurpose.Home)
                    ? Decision(person, RoutineDecisionKind.Travel, scheduled.Value, true, episode, departure) : default;
            var urgent = ArbitrateUrgentNeed(person);
            if (urgent.HasValue)
                return Decision(person, RoutineDecisionKind.Travel, urgent.Value, true,
                    urgent == scheduled ? episode : person.PurposeStartedAtTick, departure);
            var home = person.CurrentLocation.Equals(OneRoof.Domain.Identity.WorldLocation.InRoom(person.HomeRoomId));
            var work = person.CurrentLocation.Equals(person.WorkplaceLocation);
            if (home && person.GetNeedSatisfaction(NeedKind.Energy) < CriticalEnergyThreshold)
                return new RoutineDecision(RoutineDecisionKind.None, null, false, episode);
            if (departure >= 0 && person.WorkSegmentStartTick > tick.Value)
                return Decision(person, work ? RoutineDecisionKind.LocalActivity : RoutineDecisionKind.Travel,
                    TripPurpose.Work, true, person.WorkDemandEpisodeStartTick, departure);
            if (label == DailySchedule.LabelWork)
            {
                if (work)
                {
                    person.UpdateActivity(ActivityKind.Working);
                    ResidentPurposeSystem.AdvancePerson(person, tick);
                }
                return Decision(person, work ? RoutineDecisionKind.LocalActivity : RoutineDecisionKind.Travel,
                    TripPurpose.Work, true, episode, departure);
            }
            if (label == DailySchedule.LabelEat)
            {
                if (person.CurrentActivity == ActivityKind.Working) person.UpdateActivity(ActivityKind.Idle);
                if (person.CurrentActivity == ActivityKind.Eating && person.GetNeedSatisfaction(NeedKind.Hunger) < SatisfiedNeedThreshold)
                    return new RoutineDecision(RoutineDecisionKind.None, null, false, episode);
                if (person.GetNeedSatisfaction(NeedKind.Hunger) < SatisfiedNeedThreshold)
                    return new RoutineDecision(RoutineDecisionKind.Travel, TripPurpose.Food, true, episode);
                // A completed meal resumes work at its appointment, otherwise returns to home time.
                if (!home && !person.HasCommittedPurposeAt(tick))
                    return new RoutineDecision(RoutineDecisionKind.Travel, TripPurpose.Home, true, episode);
                return new RoutineDecision(RoutineDecisionKind.None, null, false, episode);
            }
            if (label == DailySchedule.LabelLeisure && person.RoutineDayChoice > 0 &&
                tick.Value - dayBase - block.EpisodeStartDayTick >= (person.RoutineDayChoice - 1) * 15 &&
                person.GetNeedSatisfaction(NeedKind.Social) < SatisfiedNeedThreshold)
            {
                if (person.CurrentActivity == ActivityKind.Leisure && !home)
                    return new RoutineDecision(RoutineDecisionKind.None, null, false, episode);
                return new RoutineDecision(RoutineDecisionKind.Travel, TripPurpose.Leisure, false, episode);
            }
            if (label == DailySchedule.LabelSleep || label == DailySchedule.LabelHome || label == DailySchedule.LabelPreparation)
            {
                if (home)
                {
                    person.UpdateActivity(label == DailySchedule.LabelSleep ? ActivityKind.Sleeping : ActivityKind.Idle);
                    ResidentPurposeSystem.AdvancePerson(person, tick);
                }
                return new RoutineDecision(home ? RoutineDecisionKind.LocalActivity : RoutineDecisionKind.Travel,
                    TripPurpose.Home, true, episode);
            }
            return new RoutineDecision(RoutineDecisionKind.None, null, false, episode);
        }

        private static RoutineDecision Decision(PersonRecord person, RoutineDecisionKind kind, TripPurpose purpose,
            bool required, long episode, long departure) => new RoutineDecision(kind, purpose, required, episode,
                person.WorkAppointmentStartTick, person.WorkAppointmentEndTick, departure);

        private TripPurpose? EvaluateDestination(PersonRecord person, Tick tick, bool isBlockTransition)
        {
            if (person == null) return null;
            if (person.CurrentActivity == ActivityKind.Commuting) return null;
            var isOutsideShift = person.CurrentPurpose == ResidentPurposeKind.WorkingOutside &&
                                 person.CurrentLocation.IsOutside;
            if (isOutsideShift && person.CurrentActivity == ActivityKind.Eating &&
                person.GetNeedSatisfaction(NeedKind.Hunger) >= SatisfiedNeedThreshold)
                person.UpdateActivity(ActivityKind.Working);
            if (isOutsideShift && tick.Value >= person.PurposeEndsAtTick)
                return TripPurpose.Home;
            if (isOutsideShift && !CanInterruptOutsideWork(person, tick)) return null;

            var activeLabel = person.Schedule.ActiveLabelAt(tick);
            var urgent = ArbitrateUrgentNeed(person);
            if (urgent.HasValue)
            {
                return urgent.Value;
            }

            if (!isBlockTransition)
            {
                return ArbitrateMidBlockRecovery(person, activeLabel);
            }

            var isAtHome = person.CurrentLocation.Equals(OneRoof.Domain.Identity.WorldLocation.InRoom(person.HomeRoomId));
            var isAtWork = person.CurrentLocation.Equals(person.WorkplaceLocation);
            var hunger = person.GetNeedSatisfaction(NeedKind.Hunger);
            var social = person.GetNeedSatisfaction(NeedKind.Social);

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
                    if (!isAtHome)
                    {
                        return TripPurpose.Home;
                    }
                    return null;

                case DailySchedule.LabelLeisure:
                default:
                    if (social < LowSocialThreshold && person.CurrentActivity != ActivityKind.Leisure)
                    {
                        return TripPurpose.Leisure;
                    }
                    if (!isAtHome)
                    {
                        return TripPurpose.Home;
                    }

                    return null;
            }
        }

        private static bool CanInterruptOutsideWork(PersonRecord person, Tick tick)
        {
            if (person.CurrentPurpose != ResidentPurposeKind.WorkingOutside || !person.CurrentLocation.IsOutside)
                return false;
            return tick.Value >= person.PurposeEndsAtTick || person.CurrentActivity == ActivityKind.Eating ||
                   person.GetNeedSatisfaction(NeedKind.Hunger) < CriticalHungerThreshold ||
                   person.GetNeedSatisfaction(NeedKind.Energy) < CriticalEnergyThreshold ||
                   person.GetNeedSatisfaction(NeedKind.Hygiene) < CriticalHygieneThreshold;
        }

        /// <summary>
        /// Mid-block recovery for a continuous 24/7 cycle. Handles two cases that block-boundary
        /// evaluation alone misses when a resident is still travelling across the boundary:
        /// finished meals (a satisfied eater sitting in the diner) and stranded residents
        /// (a committed Sleeping/Working/Leisure activity at a location that disagrees with the
        /// current schedule block). Idle residents are deliberately left silent mid-block.
        /// May set a matching in-place activity when no trip is needed and returns null then.
        /// </summary>
        private TripPurpose? ArbitrateMidBlockRecovery(PersonRecord person, string activeLabel)
        {
            var isAtHome = person.CurrentLocation.Equals(OneRoof.Domain.Identity.WorldLocation.InRoom(person.HomeRoomId));
            var isAtWork = person.CurrentLocation.Equals(person.WorkplaceLocation);
            var hunger = person.GetNeedSatisfaction(NeedKind.Hunger);
            var social = person.GetNeedSatisfaction(NeedKind.Social);

            // A minimum-length episode can span a schedule boundary. Once it
            // releases, catch up to the block that is now active.
            if (person.CurrentPurpose != ResidentPurposeKind.None)
            {
                if (activeLabel == DailySchedule.LabelSleep && !isAtHome) return TripPurpose.Home;
                if (activeLabel == DailySchedule.LabelWork && !isAtWork) return TripPurpose.Work;
                if (activeLabel == DailySchedule.LabelEat &&
                    person.CurrentActivity != ActivityKind.Eating && hunger < SatisfiedNeedThreshold)
                    return TripPurpose.Food;
            }

            // 1. Finished meal: hunger restored but resident still sits in the diner.
            if (person.CurrentActivity == ActivityKind.Eating && hunger >= SatisfiedNeedThreshold)
            {
                switch (activeLabel)
                {
                    case DailySchedule.LabelSleep:
                        if (!isAtHome)
                        {
                            return TripPurpose.Home;
                        }
                        person.UpdateActivity(ActivityKind.Sleeping);
                        return null;

                    case DailySchedule.LabelWork:
                        if (!isAtWork)
                        {
                            return TripPurpose.Work;
                        }
                        person.UpdateActivity(ActivityKind.Working);
                        return null;

                    case DailySchedule.LabelEat:
                        return TripPurpose.Home;

                    case DailySchedule.LabelLeisure:
                    default:
                        // Reached only when no urgent need fired, so social is already
                        // comfortable: relax where you ate instead of shuttling to the
                        // lobby and back all evening (elevator churn).
                        person.UpdateActivity(ActivityKind.Leisure);
                        return null;
                }
            }

            // 2. Stranded catch-up: missed the boundary while commuting and now sits in a
            // committed activity that disagrees with the current block.
            switch (activeLabel)
            {
                case DailySchedule.LabelSleep:
                    if (isAtHome && person.CurrentActivity == ActivityKind.Idle)
                    {
                        person.UpdateActivity(ActivityKind.Sleeping);
                        return null;
                    }
                    if (person.CurrentActivity == ActivityKind.Working)
                    {
                        return TripPurpose.Home;
                    }
                    if (person.CurrentActivity == ActivityKind.Leisure && social >= LowSocialThreshold)
                    {
                        return TripPurpose.Home;
                    }
                    return null;

                case DailySchedule.LabelWork:
                    if (isAtWork && person.CurrentActivity == ActivityKind.Idle)
                    {
                        person.UpdateActivity(ActivityKind.Working);
                        return null;
                    }
                    if (!isAtWork && person.CurrentActivity == ActivityKind.Sleeping)
                    {
                        return TripPurpose.Work;
                    }
                    if (!isAtWork && person.CurrentActivity == ActivityKind.Leisure && social >= LowSocialThreshold)
                    {
                        return TripPurpose.Work;
                    }
                    return null;

                default:
                    return null;
            }
        }
    }
}
