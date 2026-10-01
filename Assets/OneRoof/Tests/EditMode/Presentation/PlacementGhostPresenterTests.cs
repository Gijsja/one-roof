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
    }
}
