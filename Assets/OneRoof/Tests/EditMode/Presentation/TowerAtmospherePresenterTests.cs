using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Application.Tower;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerAtmospherePresenterTests
    {
        [Test]
        public void UpdateSoundscape_WalkingProjection_TracksFootstepAtResidentLocation()
        {
            var root = new GameObject("Atmosphere Test");
            try
            {
                var presenter = root.AddComponent<TowerAtmospherePresenter>();
                var session = new TowerSimulationSession();
                var projection = new TowerProjection(7, 0, 0, 0f,
                    new List<TransitResidentProjection> { new TransitResidentProjection(42, 2, TransitResidentStatus.Walking, 2, 4f) },
                    new List<ElevatorProjection> { new ElevatorProjection(1, 0, 0, 8, new List<int>()) });

                presenter.UpdateSoundscape(projection, session.Topology);

                Assert.That(presenter.LastFootstepResidentId, Is.EqualTo(42));
                Assert.That(presenter.FootstepFoley.transform.position.y, Is.EqualTo(TowerStructurePresenter.FloorY(2) - 0.58f).Within(0.001f));
                Assert.That(presenter.ElevatorFoley.clip, Is.Not.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
