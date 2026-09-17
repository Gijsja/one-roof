using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class RoomPresenterTests
    {
        private GameObject _holder;
        private RoomPresenter _presenter;
        private Material _material;
        private MaterialPropertyBlock _colorBlock;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_Room_Holder");
            _material = new Material(Shader.Find("Sprites/Default"));
            _colorBlock = new MaterialPropertyBlock();
            _presenter = new RoomPresenter();
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
        public void EnsureRoomViews_WithNullTopology_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _presenter.EnsureRoomViews(null));
            Assert.That(_presenter.RenderedRoomIds.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnsureRoomViews_WithTowerTopology_RendersRooms()
        {
            var session = new TowerSimulationSession();
            var topology = session.Topology;

            _presenter.EnsureRoomViews(topology);

            Assert.That(_presenter.RenderedRoomIds.Count, Is.GreaterThan(0));
            foreach (var roomId in _presenter.RenderedRoomIds)
            {
                Assert.That(topology.Rooms.ContainsKey(roomId), Is.True);
            }
        }

        [Test]
        public void Clear_DestroysRoomObjectsAndClearsTrackedIds()
        {
            var session = new TowerSimulationSession();
            _presenter.EnsureRoomViews(session.Topology);
            Assert.That(_presenter.RenderedRoomIds.Count, Is.GreaterThan(0));

            _presenter.Clear();

            Assert.That(_presenter.RenderedRoomIds.Count, Is.EqualTo(0));
            Assert.That(_holder.transform.childCount, Is.EqualTo(0));
        }
    }
}
