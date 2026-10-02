using System.Linq;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using OneRoof.UI.Modes;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class PlacementGhostPresenterTests
    {
        private GameObject _holder;
        private PlacementGhostPresenter _ghostPresenter;
        [SetUp] public void SetUp() { _holder = new GameObject("Ghost test"); _ghostPresenter = _holder.AddComponent<PlacementGhostPresenter>(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_holder); }
        [Test]
        public void GhostPresenter_ShowAndHide_TogglesVisibilityAndValidity()
        {
            Assert.That(_ghostPresenter.IsVisible, Is.False);

            _ghostPresenter.ShowGhost(new Vector3(1f, 2f, 0f), new Vector2(3f, 1.5f), isValid: true);

            Assert.That(_ghostPresenter.IsVisible, Is.True);
            Assert.That(_ghostPresenter.IsValid, Is.True);
            Assert.That(_ghostPresenter.CurrentPosition.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(_ghostPresenter.CurrentPosition.y, Is.EqualTo(2f).Within(0.01f));

            _ghostPresenter.ShowGhost(new Vector3(2f, 3f, 0f), new Vector2(3f, 1.5f), isValid: false);
            Assert.That(_ghostPresenter.IsValid, Is.False);

            _ghostPresenter.HideGhost();
            Assert.That(_ghostPresenter.IsVisible, Is.False);
        }

        [Test]
        public void GhostPresenter_TintChangesLeaveSharedPrimitiveUntouched_AndReleaseOwnedMesh()
        {
            var unrelated = GameObject.CreatePrimitive(PrimitiveType.Quad);
            try
            {
                var sharedMesh = unrelated.GetComponent<MeshFilter>().sharedMesh;
                var originalColors = sharedMesh.colors;
                _ghostPresenter.ShowGhost(Vector3.zero, Vector2.one, isValid: true);
                var previewMesh = _holder.GetComponentInChildren<MeshFilter>(true).sharedMesh;
                Assert.That(previewMesh, Is.Not.SameAs(sharedMesh),
                    "Placement tint must not alter the mesh used by Tower geometry and excavation masks.");

                _ghostPresenter.ShowGhost(Vector3.zero, Vector2.one, isValid: true);
                Assert.That(previewMesh.colors.Length, Is.EqualTo(4));
                Assert.That(sharedMesh.colors, Is.EqualTo(originalColors));
                var validColors = previewMesh.colors;
                _ghostPresenter.ShowGhost(Vector3.zero, Vector2.one, isValid: false);
                Assert.That(previewMesh.colors, Is.Not.EqualTo(validColors), "Validity updates the preview tint.");
                Assert.That(sharedMesh.colors, Is.EqualTo(originalColors));
                Assert.That(unrelated.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(sharedMesh));

                // EditMode does not invoke MonoBehaviour lifecycle for this unawakened component.
                typeof(PlacementGhostPresenter).GetMethod("OnDestroy",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(_ghostPresenter, null);
                Object.DestroyImmediate(_holder);
                _holder = null;
                Assert.That(previewMesh == null, Is.True, "The runtime mesh must be released with its owner.");
                Assert.That(sharedMesh != null, Is.True, "Destroying the preview must preserve Unity's primitive mesh.");
            }
            finally { Object.DestroyImmediate(unrelated); }
        }
    }
}
