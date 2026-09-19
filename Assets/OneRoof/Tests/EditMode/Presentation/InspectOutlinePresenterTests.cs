using NUnit.Framework;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    [TestFixture]
    public sealed class InspectOutlinePresenterTests
    {
        private GameObject _holder;
        private InspectOutlinePresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_InspectOutlinePresenter");
            _presenter = _holder.AddComponent<InspectOutlinePresenter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void EnsureMaterials_InitializesHoverAndSelectionMaterials()
        {
            _presenter.EnsureMaterials();

            Assert.That(_presenter.HoverMaterial, Is.Not.Null);
            Assert.That(_presenter.SelectionMaterial, Is.Not.Null);
            Assert.That(_presenter.HoverMaterial.name, Contains.Substring("InspectHoverOutline_Mat"));
            Assert.That(_presenter.SelectionMaterial.name, Contains.Substring("InspectSelectionOutline_Mat"));
        }

        [Test]
        public void HighlightHoverBox_SetsHoverTargetAndBounds()
        {
            var bounds = new Bounds(new Vector3(1.5f, -2.0f, 0f), new Vector3(3.0f, 1.48f, 1f));
            _presenter.HighlightHoverBox(bounds);

            Assert.That(_presenter.HasHoverTarget, Is.True);
            Assert.That(_presenter.HoverBounds, Is.EqualTo(bounds));
        }

        [Test]
        public void HighlightSelectBox_SetsSelectionTargetAndBounds()
        {
            var bounds = new Bounds(new Vector3(-1.9f, 0.5f, 0f), new Vector3(1.06f, 7.5f, 1f));
            _presenter.HighlightSelectBox(bounds);

            Assert.That(_presenter.HasSelectionTarget, Is.True);
            Assert.That(_presenter.SelectionBounds, Is.EqualTo(bounds));
        }

        [Test]
        public void HighlightHoverSprite_SetsSpriteTarget()
        {
            var targetGo = new GameObject("ResidentTestTarget");
            targetGo.transform.position = new Vector3(0.5f, 1.0f, 0f);
            var sprite = Sprite.Create(new Texture2D(8, 8), new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
            var bounds = new Bounds(targetGo.transform.position, new Vector3(0.5f, 0.75f, 1f));

            _presenter.HighlightHoverSprite(sprite, targetGo.transform, bounds);

            Assert.That(_presenter.HasHoverTarget, Is.True);
            Assert.That(_presenter.HoverBounds, Is.EqualTo(bounds));

            Object.DestroyImmediate(targetGo);
            if (sprite != null && sprite.texture != null) Object.DestroyImmediate(sprite.texture);
            if (sprite != null) Object.DestroyImmediate(sprite);
        }

        [Test]
        public void ClearHover_ResetsHoverState()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            _presenter.HighlightHoverBox(bounds);
            Assert.That(_presenter.HasHoverTarget, Is.True);

            _presenter.ClearHover();

            Assert.That(_presenter.HasHoverTarget, Is.False);
            Assert.That(_presenter.HoverBounds, Is.EqualTo(default(Bounds)));
        }

        [Test]
        public void ClearSelection_ResetsSelectionState()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            _presenter.HighlightSelectBox(bounds);
            Assert.That(_presenter.HasSelectionTarget, Is.True);

            _presenter.ClearSelection();

            Assert.That(_presenter.HasSelectionTarget, Is.False);
            Assert.That(_presenter.SelectionBounds, Is.EqualTo(default(Bounds)));
        }

        [Test]
        public void ClearAll_ResetsBothHoverAndSelection()
        {
            _presenter.HighlightHoverBox(new Bounds(Vector3.left, Vector3.one));
            _presenter.HighlightSelectBox(new Bounds(Vector3.right, Vector3.one));

            Assert.That(_presenter.HasHoverTarget, Is.True);
            Assert.That(_presenter.HasSelectionTarget, Is.True);

            _presenter.ClearAll();

            Assert.That(_presenter.HasHoverTarget, Is.False);
            Assert.That(_presenter.HasSelectionTarget, Is.False);
        }
    }
}
