using NUnit.Framework;
using OneRoof.Domain.Time;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerAtmosphereDayNightTests
    {
        [Test]
        public void UpdateDayNight_SetsNightTintAlphaByPhase()
        {
            var go = new GameObject("Atmosphere");
            try
            {
                var atmosphere = go.AddComponent<TowerAtmospherePresenter>();

                atmosphere.UpdateDayNight(new DayPhase(1, 12, 0, false), 5);
                Assert.That(atmosphere.NightTintAlpha, Is.EqualTo(0f));

                atmosphere.UpdateDayNight(new DayPhase(1, 0, 0, true), 5);
                Assert.That(atmosphere.NightTintAlpha, Is.EqualTo(0.34f).Within(0.001f));

                atmosphere.UpdateDayNight(new DayPhase(1, 12, 0, false), 5);
                Assert.That(atmosphere.NightTintAlpha, Is.EqualTo(0f));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
