using NUnit.Framework;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class OutsideCityPresenterTests
    {
        private GameObject _holder;
        private GameObject _cameraObject;
        private Material _material;
        private OutsideCityPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Outside City Test");
            _cameraObject = new GameObject("Outside City Camera", typeof(Camera));
            _material = new Material(Shader.Find("Sprites/Default"));
            _presenter = _holder.AddComponent<OutsideCityPresenter>();
            _presenter.Initialize(_cameraObject.GetComponent<Camera>(), _material);
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null) Object.DestroyImmediate(_holder);
            if (_cameraObject != null) Object.DestroyImmediate(_cameraObject);
            if (_material != null) Object.DestroyImmediate(_material);
        }

        [Test]
        public void CityLayersParallaxAtDifferentRatesWithoutCollidersOrDuplicateGeometry()
        {
            var ground = new CellBounds(0, -14, 17);
            _presenter.SyncGround(ground, 5);
            Assert.That(_presenter.LayerCount, Is.EqualTo(3));
            Assert.That(_presenter.FacadeCount, Is.GreaterThan(20));
            Assert.That(_presenter.WindowCount, Is.GreaterThan(50));
            Assert.That(_holder.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(_presenter.Layers[0].GetComponentInChildren<MeshFilter>().sharedMesh.normals[0].z,
                Is.LessThan(0f), "The city quads must face the tower camera.");
            var renderers = _holder.GetComponentsInChildren<MeshRenderer>(true).Length;

            var starts = new float[3];
            for (var i = 0; i < starts.Length; i++) starts[i] = _presenter.Layers[i].localPosition.x;
            _cameraObject.transform.position = new Vector3(10f, 0f, -10f);
            _presenter.UpdateParallax();
            Assert.That(_presenter.Layers[0].localPosition.x - starts[0], Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(_presenter.Layers[1].localPosition.x - starts[1], Is.EqualTo(2f).Within(0.001f));
            Assert.That(_presenter.Layers[2].localPosition.x - starts[2], Is.EqualTo(3.6f).Within(0.001f));

            _presenter.SyncGround(ground, 5);
            Assert.That(_holder.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(renderers));
        }

        [Test]
        public void NightLightingWarmsWindowsAndGroundExpansionMovesTheCity()
        {
            _presenter.SyncGround(new CellBounds(0, -14, 17), 5);
            var block = new MaterialPropertyBlock();
            _presenter.UpdateLighting(new DayPhase(1, 12, 0, false));
            _presenter.Windows[0].GetPropertyBlock(block);
            var dayColor = block.GetColor("_BaseColor");

            _presenter.UpdateLighting(new DayPhase(1, 22, 0, true));
            _presenter.Windows[0].GetPropertyBlock(block);
            var nightColor = block.GetColor("_BaseColor");
            Assert.That(nightColor.r, Is.GreaterThan(dayColor.r));
            Assert.That(_presenter.Windows[0].sharedMaterial.shader.name,
                Is.EqualTo("AllIn1SpriteShader/AllIn1SpriteShader"));
            Assert.That(_presenter.Windows[0].sharedMaterial.IsKeywordEnabled("GLOW_ON"), Is.True);
            Assert.That(block.GetFloat("_Glow"), Is.GreaterThan(0.5f));

            var before = _presenter.Layers[0].localPosition.x;
            _presenter.SyncGround(new CellBounds(0, -14, 21), 5);
            Assert.That(_presenter.Layers[0].localPosition.x, Is.EqualTo(before + 2f).Within(0.001f));
        }

        [Test]
        public void GroundStartKeepsSkylineNearLobbyScaleAndRemovesOrphanedCityRoots()
        {
            var orphan = new GameObject("Outside City View");
            orphan.transform.SetParent(_holder.transform, false);
            _presenter.SyncGround(new CellBounds(0, -14, 17), 1);

            var roots = 0;
            foreach (Transform child in _holder.transform)
                if (child.name == "Outside City View") roots++;
            Assert.That(roots, Is.EqualTo(1));
            Assert.That(_holder.transform.Find("Outside City View/Outside Sky"), Is.Null,
                "The camera background should not have a rectangular sky edge.");

            var farFacade = _presenter.Layers[0].Find("Building 7");
            Assert.That(farFacade, Is.Not.Null);
            Assert.That(farFacade.localScale.y, Is.LessThan(5.2f),
                "A single-floor lobby should retain a readable silhouette beside the city.");

            var block = new MaterialPropertyBlock();
            farFacade.GetComponent<MeshRenderer>().GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").r, Is.LessThan(0.5f),
                "Generated facades must never start as white rectangles.");
        }
    }
}
