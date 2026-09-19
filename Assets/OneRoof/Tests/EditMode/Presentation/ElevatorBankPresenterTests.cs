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

        [Test]
        public void GetShaftBounds_And_IsPointerInShaft_DetectsShaftInterior()
        {
            _presenter.EnsureShaftViews(5);

            var bounds = _presenter.GetShaftBounds();
            Assert.That(bounds.size.x, Is.GreaterThan(0.9f));
            Assert.That(bounds.size.y, Is.GreaterThan(5.0f));

            Assert.That(_presenter.IsPointerInShaft(new Vector2(-1.9f, 0f), out var hitBounds), Is.True);
            Assert.That(hitBounds, Is.EqualTo(bounds));

            Assert.That(_presenter.IsPointerInShaft(new Vector2(5.0f, 0f), out _), Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CalculateCarLayout_KeepsAllCarsInsideShaftBoundary(int carCount)
        {
            const float shaftInternalLeft = -2.36f;
            const float shaftInternalRight = -1.44f;

            for (var i = 0; i < carCount; i++)
            {
                ElevatorBankPresenter.CalculateCarLayout(i, carCount, out var x, out var width);

                var carLeft = x - width * 0.5f;
                var carRight = x + width * 0.5f;

                Assert.That(carLeft, Is.GreaterThanOrEqualTo(shaftInternalLeft - 0.001f), $"Car {i}/{carCount} left edge {carLeft} must be >= {shaftInternalLeft}");
                Assert.That(carRight, Is.LessThanOrEqualTo(shaftInternalRight + 0.001f), $"Car {i}/{carCount} right edge {carRight} must be <= {shaftInternalRight}");
                Assert.That(width, Is.GreaterThan(0.15f), $"Car {i}/{carCount} width must be positive and legible");
            }
        }

        [Test]
        public void EnsureShaftViews_WithMinAndMaxFloor_SetsCorrectSpan()
        {
            _presenter.EnsureShaftViews(2, 6);

            Assert.That(_presenter.RenderedShaftFloorCount, Is.EqualTo(5));
            var bounds = _presenter.GetShaftBounds();
            Assert.That(bounds.center.y, Is.EqualTo(TowerStructurePresenter.FloorY(4)).Within(0.01f));
        }
    }
}
