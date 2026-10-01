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

        [TestCase(360, "06:00")]
        [TestCase(755, "12:35")]
        [TestCase(1439, "23:59")]
        public void Ticks_MapToHours_AtSixtyTicksPerHour(int tick, string expectedLabel)
        {
            Assert.That(DayClock.FromTick(tick).ClockLabel, Is.EqualTo(expectedLabel));
        }

        [Test]
        public void DayBoundary_RollsToDayTwoMidnight()
        {
            var phase = DayClock.FromTick(DailySchedule.TicksPerDay);

            Assert.That(phase.DayNumber, Is.EqualTo(2));
            Assert.That(phase.Hour, Is.EqualTo(0));
            Assert.That(phase.IsNight, Is.True);
        }

        [TestCase(359, true)]
        [TestCase(360, false)]
        [TestCase(720, false)]
        [TestCase(1199, false)]
        [TestCase(1200, true)]
        public void NightWindow_IsEightPmToSixAm(int tick, bool expectedNight)
        {
            Assert.That(DayClock.FromTick(tick).IsNight, Is.EqualTo(expectedNight));
        }

        [Test]
        public void NegativeTick_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => DayClock.FromTick(-1));
        }
    }
}
