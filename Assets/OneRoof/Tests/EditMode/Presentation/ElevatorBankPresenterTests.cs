using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ElevatorBankPresenterTests
    {
        private GameObject _holder;
        private ElevatorBankPresenter _presenter;
        private Material _material;
        private MaterialPropertyBlock _colorBlock;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_ElevatorBank_Holder");
            _material = new Material(Shader.Find("Sprites/Default"));
            _colorBlock = new MaterialPropertyBlock();
            _presenter = new ElevatorBankPresenter();
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
        public void EnsureShaftViews_CreatesShaftComponents()
        {
            _presenter.EnsureShaftViews(5);

            Assert.That(_presenter.RenderedShaftFloorCount, Is.EqualTo(5));
            Assert.That(_holder.transform.childCount, Is.GreaterThanOrEqualTo(7)); // cavity, 2 rails, 2 columns, penthouse, pit
        }

        [Test]
        public void EnsureElevatorViews_CreatesCarsAndPositionsThem()
        {
            _presenter.EnsureElevatorViews(2);

            Assert.That(_presenter.ElevatorViews.Count, Is.EqualTo(2));
            Assert.That(_presenter.ElevatorViews[0], Is.Not.Null);
            Assert.That(_presenter.ElevatorViews[1], Is.Not.Null);
        }

        [Test]
        public void Clear_DestroysAllViewsAndResetsCounters()
        {
            _presenter.EnsureShaftViews(5);
            _presenter.EnsureElevatorViews(2);

            _presenter.Clear();

            Assert.That(_presenter.RenderedShaftFloorCount, Is.EqualTo(0));
            Assert.That(_presenter.ElevatorViews.Count, Is.EqualTo(0));
        }
    }
}
