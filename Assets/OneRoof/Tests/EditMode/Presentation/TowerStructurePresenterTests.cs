using NUnit.Framework;
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
