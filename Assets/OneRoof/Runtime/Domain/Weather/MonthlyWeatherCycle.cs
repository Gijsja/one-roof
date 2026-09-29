using System;
using System.Collections.Generic;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Weather
{
    /// <summary>
    /// Deterministic monthly weather cycle service generating atmospheric fronts across
    /// standard 30-day calendar months (43,200 ticks per month).
    /// </summary>
    public sealed class MonthlyWeatherCycle
    {
        public const int DaysPerMonth = 30;
        public const long TicksPerDay = DailySchedule.TicksPerDay; // 1440
        public const long TicksPerMonth = DaysPerMonth * TicksPerDay; // 43,200
        public const long TransitionTicks = 45; // 45 in-game minutes transition curve

        private readonly ulong _worldSeed;
        private int _cachedMonthNumber = -1;
        private List<WeatherEvent> _cachedEvents = new List<WeatherEvent>();

        public MonthlyWeatherCycle(ulong worldSeed = 0x5EED_C0DE_1337UL)
        {
            _worldSeed = worldSeed == 0 ? 0x5EED_C0DE_1337UL : worldSeed;
        }

        public ulong WorldSeed => _worldSeed;

        public static int MonthNumberFromTick(long tick)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick), "Ticks cannot be negative.");
            return (int)(tick / TicksPerMonth) + 1;
        }

        public static int DayOfMonthFromTick(long tick)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick), "Ticks cannot be negative.");
            return (int)((tick % TicksPerMonth) / TicksPerDay) + 1;
        }

        /// <summary>
        /// Samples the atmospheric weather state at the specified simulation tick.
        /// Smoothly eases intensity and wind across front boundaries.
        /// </summary>
        public WeatherSample Sample(long tick)
        {
            if (tick < 0) tick = 0;
            var month = MonthNumberFromTick(tick);
            var events = GetMonthlyEvents(month);

            var eventIndex = -1;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Contains(tick))
                {
                    eventIndex = i;
                    break;
                }
            }

            if (eventIndex < 0)
            {
                return new WeatherSample(WeatherCondition.Clear, 0f, 0f, "Clear Skies");
            }

            var current = events[eventIndex];
            var condition = current.Condition;
            var intensity = current.PeakIntensity;
            var wind = current.WindSpeed;

            var ticksIntoEvent = tick - current.StartTick;

            if (ticksIntoEvent < TransitionTicks && eventIndex > 0)
            {
                var prev = events[eventIndex - 1];
                var t = (float)ticksIntoEvent / TransitionTicks;
                var smoothT = SmoothStep(t);
                intensity = Lerp(prev.PeakIntensity, current.PeakIntensity, smoothT);
                wind = Lerp(prev.WindSpeed, current.WindSpeed, smoothT);
                if (smoothT < 0.5f && prev.Condition != WeatherCondition.Clear)
                {
                    condition = prev.Condition;
                }
            }

            // Only force Clear when the current event IS Clear (avoids flickering
            // Clear samples during a Clear-to-Rain easing window).
            if (intensity < 0.02f && current.Condition == WeatherCondition.Clear)
            {
                condition = WeatherCondition.Clear;
            }

            return new WeatherSample(condition, intensity, wind, current.Description);
        }

        public IReadOnlyList<WeatherEvent> GetMonthlyEvents(int monthNumber)
        {
            if (monthNumber < 1) monthNumber = 1;
            if (_cachedMonthNumber == monthNumber && _cachedEvents != null && _cachedEvents.Count > 0)
            {
                return _cachedEvents;
            }

            _cachedEvents = GenerateMonthEvents(monthNumber);
            _cachedMonthNumber = monthNumber;
            return _cachedEvents;
        }

        private List<WeatherEvent> GenerateMonthEvents(int monthNumber)
        {
            var list = new List<WeatherEvent>();
            var monthStartTick = (long)(monthNumber - 1) * TicksPerMonth;
            var monthEndTick = monthStartTick + TicksPerMonth;

            var monthSeed = _worldSeed ^ ((ulong)monthNumber * 104729UL) ^ 0xA5A5A5A5A5A5A5A5UL;
            var rng = new DeterministicRandomStream(monthSeed);
            var isWinter = IsWinterMonth(monthNumber);

            var currentTick = monthStartTick;
            var previousCondition = WeatherCondition.Clear;

            while (currentTick < monthEndTick)
            {
                WeatherCondition condition;
                int durationHours;
                float intensity;
                float wind;

                var roll = rng.NextInt(0, 100);

                switch (previousCondition)
                {
                    case WeatherCondition.Clear:
                        if (roll < 60)
                        {
                            condition = WeatherCondition.Clear;
                            durationHours = rng.NextInt(12, 49);
                            intensity = 0f;
                            wind = rng.NextInt(-3, 4) * 0.05f;
                        }
                        else if (roll < 85)
                        {
                            condition = WeatherCondition.Drizzle;
                            durationHours = rng.NextInt(4, 11);
                            intensity = 0.20f + (rng.NextInt(0, 15) * 0.01f);
                            wind = -0.3f - (rng.NextInt(0, 5) * 0.05f);
                        }
                        else if (roll < 92)
                        {
                            condition = WeatherCondition.Fog;
                            durationHours = rng.NextInt(3, 9);
                            intensity = 0.30f + (rng.NextInt(0, 20) * 0.01f);
                            wind = rng.NextInt(-1, 2) * 0.03f;
                        }
                        else
                        {
                            if (isWinter && rng.NextInt(0, 100) < 35)
                            {
                                condition = WeatherCondition.Snow;
                                durationHours = rng.NextInt(4, 12);
                                intensity = 0.45f + (rng.NextInt(0, 25) * 0.01f);
                                wind = rng.NextInt(-2, 3) * 0.04f;
                            }
                            else
                            {
                                condition = WeatherCondition.Rain;
                                durationHours = rng.NextInt(6, 15);
                                intensity = 0.55f + (rng.NextInt(0, 20) * 0.01f);
                                wind = -0.8f - (rng.NextInt(0, 8) * 0.08f);
                            }
                        }
                        break;

                    case WeatherCondition.Drizzle:
                        if (roll < 45)
                        {
                            condition = WeatherCondition.Clear;
                            durationHours = rng.NextInt(12, 37);
                            intensity = 0f;
                            wind = rng.NextInt(-2, 3) * 0.05f;
                        }
                        else if (roll < 85)
                        {
                            condition = WeatherCondition.Rain;
                            durationHours = rng.NextInt(5, 13);
                            intensity = 0.60f + (rng.NextInt(0, 15) * 0.01f);
                            wind = -0.9f - (rng.NextInt(0, 7) * 0.07f);
                        }
                        else if (roll < 92)
                        {
                            condition = WeatherCondition.Fog;
                            durationHours = rng.NextInt(2, 6);
                            intensity = 0.25f + (rng.NextInt(0, 15) * 0.01f);
                            wind = 0f;
                        }
                        else
                        {
                            condition = WeatherCondition.Drizzle;
                            durationHours = rng.NextInt(3, 7);
                            intensity = 0.25f + (rng.NextInt(0, 10) * 0.01f);
                            wind = -0.4f;
                        }
                        break;

                    case WeatherCondition.Rain:
                        if (roll < 40)
                        {
                            condition = WeatherCondition.Drizzle;
                            durationHours = rng.NextInt(3, 9);
                            intensity = 0.22f + (rng.NextInt(0, 12) * 0.01f);
                            wind = -0.45f;
                        }
                        else if (roll < 65)
                        {
                            if (isWinter && rng.NextInt(0, 100) < 50)
                            {
                                condition = WeatherCondition.Snow;
                                durationHours = rng.NextInt(3, 9);
                                intensity = 0.70f + (rng.NextInt(0, 15) * 0.01f);
                                wind = rng.NextInt(-2, 3) * 0.05f;
                            }
                            else
                            {
                                condition = WeatherCondition.Storm;
                                durationHours = rng.NextInt(2, 6);
                                intensity = 0.90f + (rng.NextInt(0, 11) * 0.01f);
                                wind = -2.0f - (rng.NextInt(0, 6) * 0.1f);
                            }
                        }
                        else
                        {
                            condition = WeatherCondition.Clear;
                            durationHours = rng.NextInt(12, 37);
                            intensity = 0f;
                            wind = 0f;
                        }
                        break;


                    case WeatherCondition.Storm:
                        if (roll < 75)
                        {
                            condition = WeatherCondition.Rain;
                            durationHours = rng.NextInt(3, 8);
                            intensity = 0.65f + (rng.NextInt(0, 10) * 0.01f);
                            wind = -1.2f;
                        }
                        else if (isWinter && rng.NextInt(0, 100) < 40)
                        {
                            condition = WeatherCondition.Snow;
                            durationHours = rng.NextInt(2, 5);
                            intensity = 0.25f;
                            wind = -0.5f;
                        }
                        else
                        {
                            condition = WeatherCondition.Drizzle;
                            durationHours = rng.NextInt(2, 5);
                            intensity = 0.25f;
                            wind = -0.5f;
                        }
                        break;

                    case WeatherCondition.Fog:
                        if (roll < 70)
                        {
                            condition = WeatherCondition.Clear;
                            durationHours = rng.NextInt(8, 25);
                            intensity = 0f;
                            wind = rng.NextInt(-2, 3) * 0.04f;
                        }
                        else
                        {
                            condition = WeatherCondition.Drizzle;
                            durationHours = rng.NextInt(3, 8);
                            intensity = 0.20f + (rng.NextInt(0, 15) * 0.01f);
                            wind = -0.25f;
                        }
                        break;

                    case WeatherCondition.Snow:
                        if (roll < 50)
                        {
                            condition = WeatherCondition.Clear;
                            durationHours = rng.NextInt(12, 37);
                            intensity = 0f;
                            wind = 0f;
                        }
                        else if (roll < 80)
                        {
                            condition = WeatherCondition.Snow;
                            durationHours = rng.NextInt(2, 7);
                            intensity = 0.30f + (rng.NextInt(0, 20) * 0.01f);
                            wind = rng.NextInt(-2, 3) * 0.04f;
                        }
                        else
                        {
                            condition = WeatherCondition.Rain;
                            durationHours = rng.NextInt(4, 10);
                            intensity = 0.50f + (rng.NextInt(0, 15) * 0.01f);
                            wind = -0.7f;
                        }
                        break;

                    default:
                        condition = WeatherCondition.Clear;
                        durationHours = 24;
                        intensity = 0f;
                        wind = 0f;
                        break;
                }

                var durationTicks = (long)durationHours * DayClock.TicksPerHour;
                var endTick = Math.Min(monthEndTick, currentTick + durationTicks);

                list.Add(new WeatherEvent(condition, currentTick, endTick, intensity, wind));
                previousCondition = condition;
                currentTick = endTick;
            }

            return list;
        }

        /// <summary>
        /// Returns true when <paramref name="monthNumber"/> maps to a calendar winter month
        /// (December, January, or February) in a repeating 12-month cycle.
        /// </summary>
        private static bool IsWinterMonth(int monthNumber)
        {
            var calendarMonth = ((monthNumber - 1) % 12) + 1;
            return calendarMonth == 12 || calendarMonth == 1 || calendarMonth == 2;
        }

        private static float SmoothStep(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return t * t * (3f - 2f * t);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
