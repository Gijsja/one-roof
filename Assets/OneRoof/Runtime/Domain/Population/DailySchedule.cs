using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// An ordered sequence of <see cref="ScheduleBlock"/> entries that covers one full day cycle.
    /// Blocks are contiguous: each block's EndTick equals the next block's StartTick,
    /// and together they span exactly <see cref="TicksPerDay"/> ticks starting at tick 0.
    /// </summary>
    public sealed class DailySchedule
    {
        /// <summary>Number of ticks in one simulated day (24 hours × 60 ticks/hour).</summary>
        public const long TicksPerDay = 1440;

        // Block labels used by the standard factory and by callers resolving activity.
        public const string LabelSleep = "Sleep";
        public const string LabelWork = "Work";
        public const string LabelEat = "Eat";
        public const string LabelLeisure = "Leisure";

        private DailySchedule(IReadOnlyList<ScheduleBlock> blocks)
        {
            Blocks = blocks;
        }

        /// <summary>Ordered schedule blocks covering the full day cycle.</summary>
        public IReadOnlyList<ScheduleBlock> Blocks { get; }

        /// <summary>
        /// Returns the label of the block active at <paramref name="dayTick"/>,
        /// where <paramref name="dayTick"/> is the simulation tick modulo <see cref="TicksPerDay"/>.
        /// Returns <c>null</c> if no block covers the given tick (should not occur for a valid schedule).
        /// </summary>
        public string ActiveLabelAt(Tick dayTick)
        {
            var t = new Tick(dayTick.Value % TicksPerDay);
            foreach (var block in Blocks)
            {
                if (block.Contains(t))
                {
                    return block.Label;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the <see cref="ScheduleBlock"/> active at <paramref name="dayTick"/> (modulo day length),
        /// or <c>null</c> if none matches.
        /// </summary>
        public ScheduleBlock? ActiveBlockAt(Tick dayTick)
        {
            var t = new Tick(dayTick.Value % TicksPerDay);
            foreach (var block in Blocks)
            {
                if (block.Contains(t))
                {
                    return block;
                }
            }

            return null;
        }

        // ── Factory ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a standard four-block schedule (Sleep → Work → Eat → Leisure) for the given trait,
        /// using <paramref name="rng"/> to add small jitter to block boundaries.
        /// The schedule is deterministic when <paramref name="rng"/> is identically seeded.
        /// </summary>
        public static DailySchedule Standard(PersonTrait trait, IRandomStream rng)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            // Base split (ticks): Sleep=480, Work=480, Eat=120, Leisure=360 → total 1440
            // Jitter window: ±30 ticks per boundary, shifted by trait.
            const long baseSleepEnd   = 480;   // ~08:00
            const long baseWorkEnd    = 960;   // ~16:00
            const long baseEatEnd     = 1080;  // ~18:00
            // Leisure fills to TicksPerDay.

            var traitShift = trait.Kind switch
            {
                PersonTraitKind.EarlyBird  => -60L,
                PersonTraitKind.NightOwl   => +60L,
                _                          => 0L,
            };

            var jitter1 = rng.NextInt(-30, 31);  // sleep/work boundary jitter
            var jitter2 = rng.NextInt(-20, 21);  // work/eat boundary jitter
            var jitter3 = rng.NextInt(-15, 16);  // eat/leisure boundary jitter

            var sleepEnd   = Clamp(baseSleepEnd  + traitShift + jitter1, 360, 660);
            var workEnd    = Clamp(baseWorkEnd   + traitShift + jitter2, sleepEnd + 240, 1100);
            var eatEnd     = Clamp(baseEatEnd    + traitShift + jitter3, workEnd  + 60,  1200);

            var blocks = new List<ScheduleBlock>(4)
            {
                new ScheduleBlock(LabelSleep,   new Tick(0),        new Tick(sleepEnd)),
                new ScheduleBlock(LabelWork,    new Tick(sleepEnd), new Tick(workEnd)),
                new ScheduleBlock(LabelEat,     new Tick(workEnd),  new Tick(eatEnd)),
                new ScheduleBlock(LabelLeisure, new Tick(eatEnd),   new Tick(TicksPerDay)),
            };

            return new DailySchedule(new ReadOnlyCollection<ScheduleBlock>(blocks));
        }

        /// <summary>Constructs a schedule directly from an ordered, non-overlapping block list (for tests).</summary>
        public static DailySchedule FromBlocks(IList<ScheduleBlock> blocks)
        {
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));
            if (blocks.Count == 0) throw new ArgumentException("Schedule must have at least one block.", nameof(blocks));

            return new DailySchedule(new ReadOnlyCollection<ScheduleBlock>(new List<ScheduleBlock>(blocks)));
        }

        private static long Clamp(long value, long min, long max) =>
            value < min ? min : value > max ? max : value;
    }
}
