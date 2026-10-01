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
    public sealed class ExteriorLightingContractTests
    {
        private GameObject _exteriorHolder;

        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        private BuildingExteriorPresenter _exteriorPresenter;

        [SetUp]
        public void SetUp()
        {
            _exteriorHolder = new GameObject("Test_Exterior_Holder");

            var shader = Shader.Find("OneRoof/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            _worldMaterial = new Material(shader) { name = "Test_WorldMaterial" };
            _colorBlock = new MaterialPropertyBlock();

            _exteriorPresenter = new BuildingExteriorPresenter();
            _exteriorPresenter.Initialize(_exteriorHolder.transform, _worldMaterial, _colorBlock);

        }

        [TearDown]
        public void TearDown()
        {
            _exteriorPresenter?.Clear();

            if (_exteriorHolder != null) Object.DestroyImmediate(_exteriorHolder);
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

        private static Color ReadColor(MeshRenderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetColor("_Color");
        }

        [Test]
        public void T1_F2_UpdateLighting_ExecutesWithoutExceptionAcrossAllDayPhases()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60))); // Noon
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(18 * 60))); // Dusk
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(0)));       // Midnight
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(6 * 60)));  // Dawn
        }

        [Test]
        public void WindowLighting_DarkUnoccupiedAndWarmOccupiedBaysRespondToDayPhase()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            var glass = _exteriorPresenter.Root.Find("Left Facade/Left Window Glass 1").GetComponent<MeshRenderer>();
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            var day = ReadColor(glass);
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(0));
            var night = ReadColor(glass);
            Assert.That(night, Is.Not.EqualTo(day), "Generated window color must respond to the day phase.");
            Assert.That(day.b, Is.GreaterThan(night.b));
            var occupied = _exteriorPresenter.Root.Find("Right Facade/Right Window Glass 2").GetComponent<MeshRenderer>();
            var warmNight = ReadColor(occupied);
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            var reflection = ReadColor(occupied);
            Assert.That(warmNight.r, Is.GreaterThan(reflection.r));
            Assert.That(warmNight.b, Is.LessThan(reflection.b));
        }

        [Test]
        public void T1_F2_LightingUpdate_IdempotentOnConsecutiveIdenticalTicks()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            var glass = _exteriorPresenter.Root.Find("Left Facade/Left Window Glass 1").GetComponent<MeshRenderer>();
            var phase = DayClock.FromTick(14 * 60);
            _exteriorPresenter.UpdateLighting(phase);
            var expected = ReadColor(glass);
            var meshColors = _exteriorPresenter.Root.Find("Exterior Volumetric Lighting/Batched Light Cones").GetComponent<MeshFilter>().sharedMesh.colors;
            var count = _exteriorPresenter.Root.GetComponentsInChildren<Transform>().Length;
            _exteriorPresenter.UpdateLighting(phase);
            _exteriorPresenter.UpdateLighting(phase);
            Assert.That(ReadColor(glass), Is.EqualTo(expected));
            Assert.That(_exteriorPresenter.Root.Find("Exterior Volumetric Lighting/Batched Light Cones").GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(meshColors));
            Assert.That(_exteriorPresenter.Root.GetComponentsInChildren<Transform>().Length, Is.EqualTo(count));
        }

        [Test]
        public void T1_F3_ExteriorLightCones_DepthOrderingPositionedInFrontOfCladding()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            var cone = _exteriorPresenter.Root.Find("Exterior Volumetric Lighting/Left Light Cone 1");
            var cladding = _exteriorPresenter.Root.Find("Left Facade/Left Cladding 1");
            Assert.That(cone, Is.Not.Null);
            Assert.That(cone.GetComponent<MeshFilter>().sharedMesh.bounds.center.z + cone.position.z, Is.LessThan(cladding.position.z));
        }

        [Test]
        public void T1_F3_ExteriorLightCones_OutwardProjectionDirectionAwayFromInteriorRooms()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            foreach (var side in new[] { "Left", "Right" })
            {
                var cone = _exteriorPresenter.Root.Find($"Exterior Volumetric Lighting/{side} Light Cone 1");
                var mesh = cone.GetComponent<MeshFilter>().sharedMesh;
                var bounds = mesh.bounds;
                var origin = cone.TransformPoint(mesh.vertices[0]).x;
                if (side == "Left") Assert.That(cone.TransformPoint(bounds.min).x, Is.LessThan(origin));
                else Assert.That(cone.TransformPoint(bounds.max).x, Is.GreaterThan(origin));
            }
        }

        [Test]
        public void T1_F3_ExteriorLightCones_TrapezoidMeshAlphaGradientMonotonicallyDecaysToZero()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            var colors = _exteriorPresenter.Root.Find("Exterior Volumetric Lighting/Batched Light Cones").GetComponent<MeshFilter>().sharedMesh.colors;
            Assert.That(colors.Length, Is.GreaterThan(0));
            for (var i = 0; i < colors.Length; i += 4)
            {
                Assert.That(colors[i + 1].a, Is.Zero, $"Cone {i / 4} terminus");
                Assert.That(colors[i + 2].a, Is.Zero);
                Assert.That(colors[i].a, Is.GreaterThan(0f));
                Assert.That(colors[i + 3].a, Is.EqualTo(colors[i].a));
            }
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_CreatedAtopCommunicationsMast()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var beacon = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Aviation Warning Beacon");
            var mast = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Communications Mast");

            Assert.That(beacon, Is.Not.Null);
            Assert.That(mast, Is.Not.Null);

            Assert.That(beacon.localPosition.x, Is.EqualTo(mast.localPosition.x).Within(0.01f));
            Assert.That(beacon.localPosition.y, Is.GreaterThan(mast.localPosition.y));
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_DepthPositionedAtNegativeZForSkylineVisibility()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var beacon = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Aviation Warning Beacon");
            Assert.That(beacon, Is.Not.Null);
            Assert.That(beacon.localPosition.z, Is.EqualTo(-0.45f).Within(0.01f), "Beacon must sit at Z=-0.45m in front of roof structure.");
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_LightingUpdateModulatesBeaconAlphaAtNight()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            var beacon = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Aviation Warning Beacon").GetComponent<MeshRenderer>();
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            Assert.That(ReadColor(beacon).a, Is.EqualTo(0.2f).Within(0.001f));
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(0));
            var expected = Mathf.Lerp(0.2f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
            Assert.That(ReadColor(beacon).a, Is.EqualTo(expected).Within(0.001f));
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            Assert.That(ReadColor(beacon).a, Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_ColorMatchesStandardAviationWarningRed()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            var renderer = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Aviation Warning Beacon").GetComponent<MeshRenderer>();
            var color = ReadColor(renderer);
            Assert.That(color.r, Is.EqualTo(1f));
            Assert.That(color.g, Is.LessThan(0.30f));
            Assert.That(color.b, Is.LessThan(0.20f));
        }

        [Test]
        public void WindowLighting_DawnAndDuskMidpointsInterpolateGeneratedWindowColor()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            var glass = _exteriorPresenter.Root.Find("Left Facade/Left Window Glass 1").GetComponent<MeshRenderer>();
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            var day = ReadColor(glass);
            _exteriorPresenter.UpdateLighting(DayClock.FromTick(0));
            var night = ReadColor(glass);
            foreach (var tick in new[] { 6 * 60, 20 * 60 })
            {
                _exteriorPresenter.UpdateLighting(DayClock.FromTick(tick));
                var actual = ReadColor(glass);
                var expected = Color.Lerp(day, night, 0.5f);
                Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f), $"Red at tick {tick}");
                Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f), $"Green at tick {tick}");
                Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f), $"Blue at tick {tick}");
                Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f), $"Alpha at tick {tick}");
            }
        }
    }
}
