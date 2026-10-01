using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerAtmosphereArchitecturalTests
    {
        private GameObject _atmosphereHolder;

        private TowerAtmospherePresenter _atmospherePresenter;

        [SetUp]
        public void SetUp()
        {
            _atmosphereHolder = new GameObject("Test_Atmosphere_Holder");

            _atmospherePresenter = _atmosphereHolder.AddComponent<TowerAtmospherePresenter>();
            _atmospherePresenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _atmospherePresenter?.Clear();

            if (_atmosphereHolder != null) Object.DestroyImmediate(_atmosphereHolder);
        }

        private static TowerTopologyProjection CreateTowerTopology(int floorCount, int minX = -14, int maxX = 16)
        {
            var slabs = new Dictionary<int, CellBounds>();
            for (var f = 0; f < floorCount; f++)
            {
                slabs[f] = new CellBounds(f, minX, maxX);
            }
            return new TowerTopologyProjection(slabs);
        }

        [Test]
        public void T1_F3_ExteriorLightCones_ContractProhibitsRegistrationInTowerAtmospherePresenter()
        {
            var topo = CreateTowerTopology(5);
            var snapshot = new TowerProjection(0, 0, 0, 0f, new List<OneRoof.Application.Transit.TransitResidentProjection>(), new List<ElevatorProjection>());
            _atmospherePresenter.UpdateSoundscape(snapshot, topo);

            Assert.That(_atmospherePresenter.WindowLightCount, Is.EqualTo(0),
                "Atmosphere presenter must only track interior non-transit rooms, not exterior volumetric cones.");
        }

        [Test]
        public void T1_F15_NightLightingHasNoFullBuildingTintQuad()
        {
            _atmospherePresenter.UpdateDayNight(DayClock.FromTick(0), 5); // Midnight
            Assert.That(_atmospherePresenter.NightTintAlpha, Is.GreaterThan(0.20f));
            Assert.That(_atmospherePresenter.transform.Find("NightTint"), Is.Null,
                "The old full-building tint quad must not cover foreground assets.");

            _atmospherePresenter.UpdateDayNight(DayClock.FromTick(12 * 60), 5); // Noon
            Assert.That(_atmospherePresenter.NightTintAlpha, Is.EqualTo(0f));
        }
    }
}
