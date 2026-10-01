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
    public sealed class ExteriorArchitecturalPresentationE2ETests
    {
        private GameObject _exteriorHolder;
        private GameObject _cameraHolder;
        private GameObject _cityHolder;
        private GameObject _atmosphereHolder;

        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        private BuildingExteriorPresenter _exteriorPresenter;
        private OutsideCityPresenter _cityPresenter;
        private TowerAtmospherePresenter _atmospherePresenter;
        private Camera _camera;

        private PixelRainPresenter _rainPresenter;
        [SetUp]
        public void SetUp()
        {
            _exteriorHolder = new GameObject("Test_Exterior_Holder");
            _cameraHolder = new GameObject("Test_Camera_Holder", typeof(Camera));
            _cityHolder = new GameObject("Test_City_Holder");
            _atmosphereHolder = new GameObject("Test_Atmosphere_Holder");

            _camera = _cameraHolder.GetComponent<Camera>();
            _camera.transform.position = new Vector3(0f, 0f, -10f);

            var shader = Shader.Find("OneRoof/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            _worldMaterial = new Material(shader) { name = "Test_WorldMaterial" };
            _colorBlock = new MaterialPropertyBlock();
            _rainPresenter = _cameraHolder.AddComponent<PixelRainPresenter>();

            _exteriorPresenter = new BuildingExteriorPresenter();
            _exteriorPresenter.Initialize(_exteriorHolder.transform, _worldMaterial, _colorBlock);

            _cityPresenter = _cityHolder.AddComponent<OutsideCityPresenter>();
            _cityPresenter.Initialize(_camera, _worldMaterial);

            _atmospherePresenter = _atmosphereHolder.AddComponent<TowerAtmospherePresenter>();
            _atmospherePresenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _rainPresenter?.Clear();
            _exteriorPresenter?.Clear();
            _atmospherePresenter?.Clear();

            if (_exteriorHolder != null) Object.DestroyImmediate(_exteriorHolder);
            if (_cameraHolder != null) Object.DestroyImmediate(_cameraHolder);
            if (_cityHolder != null) Object.DestroyImmediate(_cityHolder);
            if (_atmosphereHolder != null) Object.DestroyImmediate(_atmosphereHolder);
            if (_worldMaterial != null) Object.DestroyImmediate(_worldMaterial);
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

        private static TowerTopologyProjection CreateSteppedTopology(params (int floor, int minX, int maxX)[] slabs)
        {
            var dict = new Dictionary<int, CellBounds>();
            foreach (var (floor, minX, maxX) in slabs)
            {
                dict[floor] = new CellBounds(floor, minX, maxX);
            }
            return new TowerTopologyProjection(dict);
        }

        [Test]
        public void T1_F3_ExteriorLightCones_DepthOrderingPositionedBehindPixelRain()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            _rainPresenter.Initialize(_camera);
            _rainPresenter.SetWeather(OneRoof.Domain.Weather.WeatherCondition.Storm, 1f);
            _rainPresenter.UpdateWeather(2f);
            Assert.That(_rainPresenter.ActiveDropCount, Is.GreaterThan(0));
            var cone = _exteriorPresenter.Root.Find("Exterior Volumetric Lighting/Left Light Cone 1");
            Assert.That(cone.GetComponent<MeshFilter>().sharedMesh.bounds.center.z + cone.position.z,
                Is.GreaterThan(_rainPresenter.RainMeshRenderer.GetComponent<MeshFilter>().sharedMesh.vertices[0].z));
        }

        [Test]
        public void T3_Interaction_WindowLightingAndWeatherRain_DepthOrderingPreventsZConflict()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            _rainPresenter.Initialize(_camera);
            _rainPresenter.SetWeather(OneRoof.Domain.Weather.WeatherCondition.Storm, 1f);
            _rainPresenter.UpdateWeather(2f);
            Assert.That(_rainPresenter.ActiveDropCount, Is.GreaterThan(0));
            var cone = _exteriorPresenter.Root.Find("Exterior Volumetric Lighting/Left Light Cone 1");
            var glass = _exteriorPresenter.Root.Find("Left Facade/Left Window Glass 1");
            var coneZ = cone.GetComponent<MeshFilter>().sharedMesh.bounds.center.z + cone.position.z;
            Assert.That(_rainPresenter.RainMeshRenderer.GetComponent<MeshFilter>().sharedMesh.vertices[0].z, Is.LessThan(coneZ));
            Assert.That(coneZ, Is.LessThan(glass.position.z));
        }

        [Test]
        public void T3_Interaction_RooftopCoolingTowersAndSetbackTerraces_NoGeometryCollision()
        {
            var topo = CreateSteppedTopology(
                (0, -16, 20),
                (1, -12, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var terraceSlab0 = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            var chiller = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Rooftop HVAC Chiller");

            Assert.That(terraceSlab0, Is.Not.Null);
            Assert.That(chiller, Is.Not.Null);

            Assert.That(chiller.localPosition.y, Is.GreaterThan(terraceSlab0.localPosition.y),
                "Top-level mechanical chiller must sit strictly above intermediate setback terraces.");
        }

        [Test]
        public void T3_Interaction_ExteriorLightConesAndActiveRooms_ConesProjectStrictlyIntoExteriorAir()
        {
            var topology = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topology);
            Assert.That(topology.TryGetFloorSlab(1, out var slab), Is.True);
            // StageBounds includes facade wall thickness; room cutaway ends at the slab edges.
            var roomLeft = -2.4f + slab.MinX * 0.5f;
            var roomRight = -2.4f + (slab.MaxX + 1) * 0.5f;
            foreach (var side in new[] { "Left", "Right" })
            {
                var cone = _exteriorPresenter.Root.Find($"Exterior Volumetric Lighting/{side} Light Cone 1");
                Assert.That(cone, Is.Not.Null);
                var vertices = cone.GetComponent<MeshFilter>().sharedMesh.vertices;
                Assert.That(vertices, Is.Not.Empty);
                foreach (var vertex in vertices)
                {
                    var position = cone.TransformPoint(vertex);
                    if (side == "Left")
                        Assert.That(position.x, Is.LessThan(roomLeft), $"Left cone vertex at {position}");
                    else
                        Assert.That(position.x, Is.GreaterThan(roomRight), $"Right cone vertex at {position}");
                }
            }
        }

        [Test]
        public void ThirtyFloorPresentation_MaintainsHierarchyAcrossHourlyLightingChanges()
        {
            // Full 30-floor City Status skyscraper with 3-tier setbacks
            var slabs = new List<(int floor, int minX, int maxX)>();
            for (var f = 0; f < 30; f++)
            {
                if (f < 5) slabs.Add((f, -16, 18));       // Podium
                else if (f < 15) slabs.Add((f, -12, 14));  // Mid-rise
                else if (f < 25) slabs.Add((f, -8, 10));   // High-rise
                else slabs.Add((f, -5, 7));                // Crown
            }

            var topo = CreateSteppedTopology(slabs.ToArray());
            _exteriorPresenter.EnsureExteriorViews(topo);
            _cityPresenter.SyncGround(new CellBounds(0, -16, 18), 30);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(30));

            // Run through full 24-hour day/night cycle (hourly intervals)
            for (var hour = 0; hour < 24; hour++)
            {
                var tick = hour * 60;
                var phase = DayClock.FromTick(tick);

                _exteriorPresenter.UpdateLighting(phase);
                _cityPresenter.UpdateLighting(phase);
                _atmospherePresenter.UpdateDayNight(phase, 30);

                if (phase.IsNight)
                {
                    Assert.That(_atmospherePresenter.NightTintAlpha, Is.GreaterThan(0.10f));
                }
            }

            // Ensure hierarchy and stage bounds remain completely intact
            Assert.That(_exteriorPresenter.Root, Is.Not.Null);
            Assert.That(_exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab"), Is.Not.Null);
            Assert.That(_exteriorPresenter.StageBounds.RoofDeckY, Is.GreaterThan(40.0f));
        }
    }
}
