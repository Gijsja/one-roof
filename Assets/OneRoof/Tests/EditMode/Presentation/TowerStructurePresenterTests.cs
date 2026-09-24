using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerStructurePresenterTests
    {
        private GameObject _holder;
        private TowerStructurePresenter _presenter;
        private Material _material;
        private MaterialPropertyBlock _colorBlock;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_Structure_Holder");
            _material = new Material(Shader.Find("Sprites/Default"));
            _colorBlock = new MaterialPropertyBlock();
            _presenter = new TowerStructurePresenter();
            _presenter.Initialize(_holder.transform, _material, _colorBlock);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Clear();
            if (_material != null) Object.DestroyImmediate(_material);
            if (_holder != null) Object.DestroyImmediate(_holder);
        }

        [Test]
        public void EnsureFloorViews_WithNullTopology_CreatesInitialFiveFloors()
        {
            _presenter.EnsureFloorViews(null);

            Assert.That(_presenter.RenderedFloorCount, Is.EqualTo(TowerStructurePresenter.InitialFloorCount));
            Assert.That(_presenter.StructureObjects.Count, Is.GreaterThanOrEqualTo(5 * 3)); // 1 slab + 2 lines per floor
        }

        [Test]
        public void Initialize_AdoptsContiguousAuthoredFloorSlabs()
        {
            Object.DestroyImmediate(_holder);
            _holder = new GameObject("Authored Structure Holder");
            for (var floor = 0; floor < 2; floor++)
            {
                new GameObject($"Floor Slab {floor}").transform.SetParent(_holder.transform, false);
                new GameObject($"Floor Line L {floor}").transform.SetParent(_holder.transform, false);
                new GameObject($"Floor Line R {floor}").transform.SetParent(_holder.transform, false);
            }
            _presenter.Initialize(_holder.transform, _material, _colorBlock);

            Assert.That(_presenter.RenderedFloorCount, Is.EqualTo(2));
            _presenter.EnsureFloorViews(null);
            Assert.That(_holder.transform.Find("Floor Slab 0"), Is.Not.Null);
            Assert.That(_holder.transform.childCount, Is.EqualTo(15));
        }

        [Test]
        public void EnsureFloorViews_AfterTrackingLoss_AdoptsExistingInsteadOfDuplicating()
        {
            _presenter.EnsureFloorViews(null);
            Assert.That(_holder.transform.childCount, Is.EqualTo(15));

            // A fresh presenter that never tracked those quads (domain reload,
            // Clear/Ensure cycle) must adopt them, not create a second set.
            var fresh = new TowerStructurePresenter();
            try
            {
                fresh.Initialize(_holder.transform, _material, _colorBlock);
                fresh.EnsureFloorViews(null);
                Assert.That(fresh.RenderedFloorCount, Is.EqualTo(TowerStructurePresenter.InitialFloorCount));
                Assert.That(_holder.transform.childCount, Is.EqualTo(15));
            }
            finally
            {
                fresh.Clear();
            }
            Assert.That(_holder.transform.childCount, Is.EqualTo(15));
        }

        [Test]
        public void Initialize_RemovesDuplicateSlabAndPreservesCanonicalGeometry()
        {
            var topology = new TowerSimulationSession().TopologyProjection();
            _presenter.EnsureFloorViews(topology);
            var expanded = GameObject.CreatePrimitive(PrimitiveType.Quad);
            expanded.name = "Floor Slab 0";
            expanded.transform.SetParent(_holder.transform, false);
            expanded.transform.position = new Vector3(40f, 30f, 1f);
            expanded.transform.localScale = new Vector3(2f, 1.55f, 1f);
            expanded.GetComponent<MeshRenderer>().sharedMaterial = _material;

            var fresh = new TowerStructurePresenter();
            try
            {
                fresh.Initialize(_holder.transform, _material, _colorBlock);
                fresh.EnsureFloorViews(topology);

                var namedSlabs = 0;
                for (var i = 0; i < _holder.transform.childCount; i++)
                    if (_holder.transform.GetChild(i).name == "Floor Slab 0") namedSlabs++;
                Assert.That(namedSlabs, Is.EqualTo(1));
                var slab = _holder.transform.Find("Floor Slab 0");
                Assert.That(slab.position.x, Is.EqualTo(-1.40f).Within(0.01f));
                Assert.That(slab.position.y, Is.EqualTo(TowerStructurePresenter.FloorY(0)).Within(0.01f));
                Assert.That(slab.localScale.x, Is.EqualTo(16.0f).Within(0.01f));
                var block = new MaterialPropertyBlock();
                slab.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                Assert.That(block.GetColor("_BaseColor").r, Is.EqualTo(0.11f).Within(0.01f));
            }
            finally
            {
                fresh.Clear();
            }
        }

        [Test]
        public void EnsureFloorViews_RepairsMissingColorBlockWithoutRewritingGeometry()
        {
            var topology = new TowerSimulationSession().TopologyProjection();
            _presenter.EnsureFloorViews(topology);
            var slab = _holder.transform.Find("Floor Slab 0");
            var originalPosition = slab.position;
            var originalScale = slab.localScale;

            slab.GetComponent<MeshRenderer>().SetPropertyBlock(new MaterialPropertyBlock());
            _presenter.EnsureFloorViews(topology);

            Assert.That(slab.position, Is.EqualTo(originalPosition));
            Assert.That(slab.localScale, Is.EqualTo(originalScale));
            var block = new MaterialPropertyBlock();
            slab.GetComponent<MeshRenderer>().GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").r, Is.EqualTo(0.11f).Within(0.01f));
        }

        [Test]
        public void FloorY_ComputesDeterministicHeights()
        {
            var y0 = TowerStructurePresenter.FloorY(0);
            var y1 = TowerStructurePresenter.FloorY(1);
            var y2 = TowerStructurePresenter.FloorY(2);

            Assert.That(y1 - y0, Is.EqualTo(TowerStructurePresenter.DefaultFloorHeight).Within(0.001f));
            Assert.That(y2 - y1, Is.EqualTo(TowerStructurePresenter.DefaultFloorHeight).Within(0.001f));
        }

        [Test]
        public void Clear_DestroysObjectsAndResetsFloorCount()
        {
            _presenter.EnsureFloorViews(null);
            Assert.That(_presenter.RenderedFloorCount, Is.EqualTo(5));

            _presenter.Clear();

            Assert.That(_presenter.RenderedFloorCount, Is.EqualTo(0));
            Assert.That(_presenter.StructureObjects.Count, Is.EqualTo(0));
        }
    }
}
