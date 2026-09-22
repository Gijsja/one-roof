using NUnit.Framework;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class DayClockTests
    {
        [Test]
        public void TickZero_IsDayOneMidnight()
        {
            var phase = DayClock.FromTick(0);

            Assert.That(phase.DayNumber, Is.EqualTo(1));
            Assert.That(phase.Hour, Is.EqualTo(0));
            Assert.That(phase.Minute, Is.EqualTo(0));
            Assert.That(phase.IsNight, Is.True);
            Assert.That(phase.ClockLabel, Is.EqualTo("00:00"));
        }

        [Test]
        public void Ticks_MapToHours_AtSixtyTicksPerHour()
        {
            Assert.That(DayClock.FromTick(360).ClockLabel, Is.EqualTo("06:00"));
            Assert.That(DayClock.FromTick(755).ClockLabel, Is.EqualTo("12:35"));
            Assert.That(DayClock.FromTick(1439).ClockLabel, Is.EqualTo("23:59"));
        }

        [Test]
        public void DayBoundary_RollsToDayTwoMidnight()
        {
            var phase = DayClock.FromTick(DailySchedule.TicksPerDay);

            Assert.That(phase.DayNumber, Is.EqualTo(2));
            Assert.That(phase.Hour, Is.EqualTo(0));
            Assert.That(phase.IsNight, Is.True);
        }

        [Test]
        public void NightWindow_IsEightPmToSixAm()
        {
            Assert.That(DayClock.FromTick(6 * 60 - 1).IsNight, Is.True);
            Assert.That(DayClock.FromTick(6 * 60).IsNight, Is.False);
            Assert.That(DayClock.FromTick(12 * 60).IsNight, Is.False);
            Assert.That(DayClock.FromTick(19 * 60 + 59).IsNight, Is.False);
            Assert.That(DayClock.FromTick(20 * 60).IsNight, Is.True);
        }

        [Test]
        public void NegativeTick_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => DayClock.FromTick(-1));
        }
    }
}
