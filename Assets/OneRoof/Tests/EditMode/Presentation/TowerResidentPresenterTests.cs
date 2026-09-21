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

        [Test]
        public void TryGetResidentView_And_TryGetResidentAt_FindsPositionedResident()
        {
            _presenter.EnsureResidentViews(3);

            var viewFound = _presenter.TryGetResidentView(1, out var bounds, out var sprite, out var tr);
            Assert.That(viewFound, Is.True);
            Assert.That(tr, Is.Not.Null);

            tr.position = new Vector3(3.5f, 1.2f, -0.2f);
            var hitFound = _presenter.TryGetResidentAt(new Vector2(3.5f, 1.55f), 0.45f, out var hitIdx, out var hitBounds, out _, out _);
            Assert.That(hitFound, Is.True);
            Assert.That(hitIdx, Is.EqualTo(1));
            Assert.That(hitBounds.size.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Initialize_ExistingResidentDisablesLegacyCompositeSprite()
        {
            var resident = new GameObject("Resident View 1");
            resident.transform.SetParent(_holder.transform, false);
            var skeletal = resident.AddComponent<OneRoof.Presentation.Population.NpcSkeletalHierarchy>();
            skeletal.EnsureHierarchy();
            skeletal.MainRenderer.enabled = true;

            _presenter.Initialize(_holder.transform);

            Assert.That(skeletal.MainRenderer.enabled, Is.False);
        }
    }
}
