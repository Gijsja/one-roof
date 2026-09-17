using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerResidentPresenterTests
    {
        private GameObject _holder;
        private TowerResidentPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_Resident_Holder");
            _presenter = new TowerResidentPresenter();
            _presenter.Initialize(_holder.transform);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Clear();
            if (_holder != null) Object.DestroyImmediate(_holder);
        }

        [Test]
        public void EnsureResidentViews_CreatesRequestedViewCount()
        {
            _presenter.EnsureResidentViews(10);

            Assert.That(_presenter.ResidentCount, Is.EqualTo(10));
            Assert.That(_presenter.ResidentViews.Count, Is.EqualTo(10));
            Assert.That(_presenter.ResidentSkeletons.Count, Is.EqualTo(10));
        }

        [Test]
        public void UpdateResidentPositions_WithSimulationProjection_PositionsResidents()
        {
            var session = new TowerSimulationSession();
            var snapshot = session.Projection();
            var topology = session.Topology;

            _presenter.EnsureResidentViews(snapshot.Residents.Count);
            _presenter.UpdateResidentPositions(snapshot, topology, 0f);

            Assert.That(_presenter.ResidentCount, Is.EqualTo(snapshot.Residents.Count));
            for (var i = 0; i < _presenter.ResidentCount; i++)
            {
                var view = _presenter.ResidentViews[i];
                Assert.That(view, Is.Not.Null);
                Assert.That(view.transform.position.z, Is.Not.EqualTo(0f)); // Positioned with depth offset
            }
        }

        [Test]
        public void Clear_DestroysAllObjectsAndResetsCount()
        {
            _presenter.EnsureResidentViews(15);
            Assert.That(_presenter.ResidentCount, Is.EqualTo(15));

            _presenter.Clear();

            Assert.That(_presenter.ResidentCount, Is.EqualTo(0));
            Assert.That(_holder.transform.childCount, Is.EqualTo(0));
        }
    }
}
