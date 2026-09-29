using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Weather;
using OneRoof.Presentation.Tower;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class PixelRainPresenterTests
    {
        private GameObject _holder;
        private GameObject _cameraObject;
        private PixelRainPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Pixel Rain Test Holder");
            _cameraObject = new GameObject("Test Camera", typeof(Camera));
            _presenter = _holder.AddComponent<PixelRainPresenter>();
            _presenter.Initialize(_cameraObject.GetComponent<Camera>());
        }

        [TearDown]
        public void TearDown()
        {
            if (_presenter != null) _presenter.Clear();
            if (_holder != null) Object.DestroyImmediate(_holder);
            if (_cameraObject != null) Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void SyncEnvelope_CreatesRoofEaves_AtTopOverhangsAndTerraceSetbacks()
        {
            // Build a stepped 2-floor topology:
            // Floor 0: minX = -14, maxX = 16 (wider)
            // Floor 1: minX = -10, maxX = 10 (narrower setback)
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -14, 16) },
                { 1, new CellBounds(1, -10, 10) }
            };
            var rooms = new Dictionary<EntityId, Room>();
            var projection = new TowerTopologyProjection(slabs, rooms);

            _presenter.SyncEnvelope(projection);

            Assert.That(_presenter.RoofEaveCount, Is.GreaterThanOrEqualTo(4));

            // Verify top floor eaves exist (floor 1)
            var hasTopLeft = false;
            var hasTopRight = false;
            var hasSetbackLeft = false;
            var hasSetbackRight = false;

            foreach (var eave in _presenter.RoofEaves)
            {
                if (eave.Floor == 1 && eave.IsWestSide) hasTopLeft = true;
                if (eave.Floor == 1 && !eave.IsWestSide) hasTopRight = true;
                if (eave.Floor == 0 && eave.IsWestSide) hasSetbackLeft = true;
                if (eave.Floor == 0 && !eave.IsWestSide) hasSetbackRight = true;
            }

            Assert.That(hasTopLeft, Is.True, "Top floor west eave must exist.");
            Assert.That(hasTopRight, Is.True, "Top floor east eave must exist.");
            Assert.That(hasSetbackLeft, Is.True, "Stepped terrace west eave must exist.");
            Assert.That(hasSetbackRight, Is.True, "Stepped terrace east eave must exist.");
        }

        [Test]
        public void IsInsideBuilding_AccuratelyDiscriminates_InteriorRoomsFromOutsideWorld()
        {
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -10, 10) }
            };
            var projection = new TowerTopologyProjection(slabs);
            _presenter.SyncEnvelope(projection);

            var floor0Y = TowerStructurePresenter.FloorY(0);
            var left = -2.4f + -10 * 0.5f; // -7.4f
            var right = -2.4f + (10 + 1) * 0.5f; // 3.1f

            // Inside check: center of floor 0 interior room
            Assert.That(_presenter.IsInsideBuilding((left + right) * 0.5f, floor0Y), Is.True,
                "Center of floor slab should be recognized as inside building.");

            // Outside West: x < left
            Assert.That(_presenter.IsInsideBuilding(left - 2f, floor0Y), Is.False,
                "West of building must be outside world.");

            // Outside East: x > right
            Assert.That(_presenter.IsInsideBuilding(right + 2f, floor0Y), Is.False,
                "East of building must be outside world.");

            // Outside Sky: above roof
            Assert.That(_presenter.IsInsideBuilding((left + right) * 0.5f, floor0Y + 3f), Is.False,
                "Sky above roof must be outside world.");

            // Below street / ground
            Assert.That(_presenter.IsInsideBuilding((left + right) * 0.5f, floor0Y - 2f), Is.False,
                "Below ground must not be inside building.");
        }

        [Test]
        public void GetRoofY_ReturnsElevationAboveRoof_AndGroundLevelOutside()
        {
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -8, 8) }
            };
            var projection = new TowerTopologyProjection(slabs);
            _presenter.SyncEnvelope(projection);

            var expectedRoofY = TowerStructurePresenter.FloorY(0) + 0.88f;
            Assert.That(_presenter.GetRoofY(-2.4f), Is.EqualTo(expectedRoofY).Within(0.001f));

            var expectedGroundY = TowerStructurePresenter.FloorY(0) - 0.70f;
            Assert.That(_presenter.GetRoofY(20f), Is.EqualTo(expectedGroundY).Within(0.001f));
        }

        [Test]
        public void SetWeather_ConfiguresIntensityAndWind_Correctly()
        {
            _presenter.SetWeather(WeatherCondition.Clear);
            Assert.That(_presenter.Condition, Is.EqualTo(WeatherCondition.Clear));

            _presenter.SetWeather(WeatherCondition.Storm);
            Assert.That(_presenter.Condition, Is.EqualTo(WeatherCondition.Storm));
            Assert.That(_presenter.WindSpeed, Is.LessThan(-1.5f), "Storm should have strong wind slant.");

            _presenter.SetWeather(WeatherCondition.Drizzle);
            Assert.That(_presenter.Condition, Is.EqualTo(WeatherCondition.Drizzle));
        }

        [Test]
        public void UpdateWeather_SimulatesRainAndRunoff_InHeadlessModeWithoutCrashing()
        {
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -10, 10) },
                { 1, new CellBounds(1, -10, 10) }
            };
            var projection = new TowerTopologyProjection(slabs);
            _presenter.SyncEnvelope(projection);

            _presenter.SetWeather(WeatherCondition.Rain, 1.0f);

            // Simulate 60 frames
            for (var frame = 0; frame < 60; frame++)
            {
                _presenter.UpdateWeather(0.016f);
            }

            Assert.That(_presenter.ActiveDropCount, Is.GreaterThan(0), "Rain drops should be simulated.");
            Assert.That(_presenter.RainMeshRenderer.enabled, Is.True, "MeshRenderer should be active.");
            Assert.That(_holder.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Rain VFX must remain completely collider-free.");
        }

        [Test]
        public void Clear_ResetsBuffersAndState_Cleanly()
        {
            _presenter.SetWeather(WeatherCondition.Rain, 0.8f);
            _presenter.UpdateWeather(0.5f);

            _presenter.Clear();

            Assert.That(_presenter.ActiveDropCount, Is.EqualTo(0));
            Assert.That(_presenter.ActiveDripCount, Is.EqualTo(0));
            Assert.That(_presenter.ActiveSplashCount, Is.EqualTo(0));
            Assert.That(_presenter.RoofEaveCount, Is.EqualTo(0));
            Assert.That(_presenter.RainMeshRenderer, Is.Null);
        }

        [Test]
        public void SetWeather_FromWeatherSample_AppliesConditionAndIntensity()
        {
            var sample = new WeatherSample(WeatherCondition.Storm, 0.95f, -2.5f, "Severe Thunderstorm");
            _presenter.SetWeather(sample);

            Assert.That(_presenter.Condition, Is.EqualTo(WeatherCondition.Storm));
            Assert.That(_presenter.TargetIntensity, Is.EqualTo(0.95f).Within(0.001f));
            Assert.That(_presenter.WindSpeed, Is.EqualTo(-2.5f).Within(0.001f));
        }

        [Test]
        public void SetWeather_Snow_DoesNotCrash_AndProducesNoRainDrops()
        {
            var sample = new WeatherSample(WeatherCondition.Snow, 0.8f, -0.1f, "Light Snowfall");
            _presenter.SetWeather(sample);
            Assert.That(_presenter.Condition, Is.EqualTo(WeatherCondition.Snow));

            // Simulate 30 frames — must not throw
            for (var frame = 0; frame < 30; frame++)
                _presenter.UpdateWeather(0.016f);

            // Rain drops must be 0 (snow replaces rain particles)
            Assert.That(_presenter.ActiveDropCount, Is.EqualTo(0),
                "Snow condition must not spawn rain drops.");
        }

        [Test]
        public void SetWeather_Fog_DoesNotCrash_AndProducesNoRainDrops()
        {
            var sample = new WeatherSample(WeatherCondition.Fog, 0.5f, 0f, "Morning Fog");
            _presenter.SetWeather(sample);
            Assert.That(_presenter.Condition, Is.EqualTo(WeatherCondition.Fog));

            for (var frame = 0; frame < 30; frame++)
                _presenter.UpdateWeather(0.016f);

            Assert.That(_presenter.ActiveDropCount, Is.EqualTo(0),
                "Fog condition must not spawn rain drops.");
            Assert.That(_presenter.ActiveFogPuffCount, Is.GreaterThan(0),
                "Fog condition should produce visible atmospheric puffs.");
            Assert.That(_presenter.ActiveFogPuffCount, Is.LessThanOrEqualTo(6),
                "Fog must stay sparse enough to keep the tower and interface readable.");

            var mesh = _presenter.RainMeshRenderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var colors = mesh.colors32;
            for (var puffIndex = 0; puffIndex < _presenter.ActiveFogPuffCount; puffIndex++)
            {
                var centerIndex = puffIndex * 9;
                var center = vertices[centerIndex];
                Assert.That(colors[centerIndex].a, Is.LessThanOrEqualTo(13),
                    "Fog must stay translucent at its center.");
                for (var ringIndex = 1; ringIndex <= 8; ringIndex++)
                {
                    var edgeIndex = centerIndex + ringIndex;
                    Assert.That(colors[edgeIndex].a, Is.EqualTo(0),
                        "The radial fog edge must fade fully to transparent.");
                    Assert.That(Mathf.Abs(vertices[edgeIndex].x - center.x), Is.LessThanOrEqualTo(1f),
                        "Fog puff width must stay local instead of spanning the tower as a panel.");
                }
            }
        }

        [Test]
        public void UpdateWeather_ClearSky_LeavesNoActiveParticles()
        {
            // Start rainy, then clear, simulate fade-out
            _presenter.SetWeather(WeatherCondition.Rain, 0.8f);
            for (var i = 0; i < 5; i++) _presenter.UpdateWeather(0.016f);

            _presenter.SetWeather(WeatherCondition.Clear, 0f);
            // Simulate 5 seconds of fade-out (intensity ramps down at 0.5/s)
            for (var i = 0; i < 400; i++) _presenter.UpdateWeather(0.016f);

            Assert.That(_presenter.ActiveDropCount, Is.EqualTo(0), "No rain drops after full clear-sky fade.");
            Assert.That(_presenter.ActiveSplashCount, Is.EqualTo(0), "No splashes after full clear-sky fade.");
        }
    }
}
