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
    public sealed class OutsideCityArchitecturalTests
    {
        private GameObject _cameraHolder;
        private GameObject _cityHolder;

        private Material _worldMaterial;

        private OutsideCityPresenter _cityPresenter;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _cameraHolder = new GameObject("Test_Camera_Holder", typeof(Camera));
            _cityHolder = new GameObject("Test_City_Holder");

            _camera = _cameraHolder.GetComponent<Camera>();
            _camera.transform.position = new Vector3(0f, 0f, -10f);

            var shader = Shader.Find("OneRoof/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            _worldMaterial = new Material(shader) { name = "Test_WorldMaterial" };

            _cityPresenter = _cityHolder.AddComponent<OutsideCityPresenter>();
            _cityPresenter.Initialize(_camera, _worldMaterial);

        }

        [TearDown]
        public void TearDown()
        {

            if (_cameraHolder != null) Object.DestroyImmediate(_cameraHolder);
            if (_cityHolder != null) Object.DestroyImmediate(_cityHolder);
            if (_worldMaterial != null) Object.DestroyImmediate(_worldMaterial);
        }

        private static Color ReadColor(MeshRenderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetColor("_Color");
        }

        [Test]
        public void T1_F2_OutsideCityWindows_UpdateLightingModulatesWindowGlowBlock()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            Assert.That(_cityPresenter.Windows.Count, Is.GreaterThan(0));
            var window = _cityPresenter.Windows[0];
            _cityPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            var day = ReadColor(window);
            _cityPresenter.UpdateLighting(DayClock.FromTick(0));
            var night = ReadColor(window);
            Assert.That(night.r, Is.GreaterThan(day.r));
            Assert.That(night.b, Is.LessThan(day.b));
        }

        [Test]
        public void T1_F14_ParallaxBackdrop_ThreeLayersCreatedWithExactDepths()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            Assert.That(_cityPresenter.LayerCount, Is.EqualTo(3));

            var layers = _cityPresenter.Layers;
            Assert.That(layers[0].name, Is.EqualTo("Distant Skyline"));
            Assert.That(layers[1].name, Is.EqualTo("Middle Skyline"));
            Assert.That(layers[2].name, Is.EqualTo("Street Skyline"));
        }

        [Test]
        public void T1_F14_ParallaxBackdrop_DifferentialParallaxRatiosEnforced()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);

            var start0 = _cityPresenter.Layers[0].localPosition.x;
            var start1 = _cityPresenter.Layers[1].localPosition.x;
            var start2 = _cityPresenter.Layers[2].localPosition.x;

            _cameraHolder.transform.position = new Vector3(10f, 0f, -10f);
            _cityPresenter.UpdateParallax();

            var delta0 = _cityPresenter.Layers[0].localPosition.x - start0;
            var delta1 = _cityPresenter.Layers[1].localPosition.x - start1;
            var delta2 = _cityPresenter.Layers[2].localPosition.x - start2;

            Assert.That(delta0, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(delta1, Is.EqualTo(2.0f).Within(0.001f));
            Assert.That(delta2, Is.EqualTo(3.6f).Within(0.001f));
        }

        [Test]
        public void T1_F14_ParallaxBackdrop_StreetApronAndLampsCreatedUnderLayerTwo()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            var streetLayer = _cityPresenter.Layers[2];

            var apron = streetLayer.Find("Street apron");
            var lamp0 = streetLayer.Find("Lamp post 0");
            var light0 = streetLayer.Find("Lamp light 0");

            Assert.That(apron, Is.Not.Null);
            Assert.That(lamp0, Is.Not.Null);
            Assert.That(light0, Is.Not.Null);
        }

        [Test]
        public void StreetLamp_LightingChangesFromDayReflectionToWarmNightColor()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            Assert.That(_cityPresenter.Windows.Count, Is.GreaterThan(0));
            var window = _cityPresenter.Layers[2].Find("Lamp light 0").GetComponent<MeshRenderer>();
            _cityPresenter.UpdateLighting(DayClock.FromTick(12 * 60));
            var day = ReadColor(window);
            _cityPresenter.UpdateLighting(DayClock.FromTick(0));
            var night = ReadColor(window);
            Assert.That(night.r, Is.GreaterThan(day.r));
            Assert.That(night.b, Is.LessThan(day.b));
        }

        [Test]
        public void CameraPan_UpdatesEachBackdropLayerAndReturnsToOrigin()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 10);
            var initialX0 = _cityPresenter.Layers[0].localPosition.x;
            var initialX1 = _cityPresenter.Layers[1].localPosition.x;
            var initialX2 = _cityPresenter.Layers[2].localPosition.x;

            // Pan left (-10m)
            _cameraHolder.transform.position = new Vector3(-10f, 5f, -10f);
            _cityPresenter.UpdateParallax();

            var leftX0 = _cityPresenter.Layers[0].localPosition.x;
            var leftX1 = _cityPresenter.Layers[1].localPosition.x;
            var leftX2 = _cityPresenter.Layers[2].localPosition.x;

            Assert.That(leftX0 - initialX0, Is.EqualTo(-0.8f).Within(0.001f));
            Assert.That(leftX1 - initialX1, Is.EqualTo(-2.0f).Within(0.001f));
            Assert.That(leftX2 - initialX2, Is.EqualTo(-3.6f).Within(0.001f));

            // Pan right (+10m)
            _cameraHolder.transform.position = new Vector3(10f, 5f, -10f);
            _cityPresenter.UpdateParallax();

            var rightX0 = _cityPresenter.Layers[0].localPosition.x;
            var rightX1 = _cityPresenter.Layers[1].localPosition.x;
            var rightX2 = _cityPresenter.Layers[2].localPosition.x;

            Assert.That(rightX0 - initialX0, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(rightX1 - initialX1, Is.EqualTo(2.0f).Within(0.001f));
            Assert.That(rightX2 - initialX2, Is.EqualTo(3.6f).Within(0.001f));

            // Return to origin (0m)
            _cameraHolder.transform.position = new Vector3(0f, 5f, -10f);
            _cityPresenter.UpdateParallax();

            Assert.That(_cityPresenter.Layers[0].localPosition.x, Is.EqualTo(initialX0).Within(0.001f));
            Assert.That(_cityPresenter.Layers[1].localPosition.x, Is.EqualTo(initialX1).Within(0.001f));
            Assert.That(_cityPresenter.Layers[2].localPosition.x, Is.EqualTo(initialX2).Within(0.001f));
        }
    }
}
