using System.Linq;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

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
        public void EnsureRoomViews_AdoptsAuthoredRoomInsteadOfCreatingDuplicate()
        {
            var session = new TowerSimulationSession();
            var room = System.Linq.Enumerable.First(session.Topology.Rooms.Values);
            var authored = new GameObject($"RoomView_{room.Id}");
            authored.transform.SetParent(_holder.transform, false);

            _presenter.EnsureRoomViews(session.Topology);

            Assert.That(_holder.transform.Find($"RoomView_{room.Id}"), Is.SameAs(authored.transform));
            var matching = 0;
            for (var i = 0; i < _holder.transform.childCount; i++)
                if (_holder.transform.GetChild(i).name == authored.name) matching++;
            Assert.That(matching, Is.EqualTo(1));
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

        [Test]
        public void TryGetRoomBounds_WithValidRoom_ReturnsTrueAndValidBounds()
        {
            var session = new TowerSimulationSession();
            var topo = session.Topology;
            Assert.That(topo.Rooms.Count, Is.GreaterThan(0));
            var firstRoom = System.Linq.Enumerable.First(topo.Rooms.Values);

            var found = _presenter.TryGetRoomBounds(firstRoom.Id, topo, out var bounds);

            Assert.That(found, Is.True);
            Assert.That(bounds.size.x, Is.GreaterThan(0f));
            Assert.That(bounds.size.y, Is.GreaterThan(0f));
        }

        [Test]
        public void TryGetRoomAt_WhenWithinBounds_ReturnsRoomId()
        {
            var session = new TowerSimulationSession();
            var topo = session.Topology;
            Room firstRoom = null;
            foreach (var r in topo.Rooms.Values)
            {
                var ct = r.ContentType.Value ?? "";
                if (!ct.Contains("elevator_shaft"))
                {
                    firstRoom = r;
                    break;
                }
            }
            Assert.That(firstRoom, Is.Not.Null);

            var roomLeft = -2.4f + firstRoom.Bounds.MinX * 0.5f;
            var roomRight = -2.4f + (firstRoom.Bounds.MaxX + 1) * 0.5f;
            var pos = new Vector2((roomLeft + roomRight) * 0.5f, TowerStructurePresenter.FloorY(firstRoom.Floor));

            var found = _presenter.TryGetRoomAt(pos, topo, out var hitId, out var bounds);

            Assert.That(found, Is.True);
            Assert.That(hitId, Is.EqualTo(firstRoom.Id));
            Assert.That(bounds.Contains(new Vector3(pos.x, pos.y, 0f)), Is.True);
        }

        [Test]
        public void EnsureRoomViews_WhenRoomDemolished_DestroysRoomAndAllChildrenWithoutOrphans()
        {
            var session = new TowerSimulationSession();
            var topo = session.Topology;

            _presenter.EnsureRoomViews(topo);
            var initialChildCount = _holder.transform.childCount;
            Assert.That(initialChildCount, Is.GreaterThan(0));

            // Demolish one room from simulation
            Room targetRoom = null;
            foreach (var r in topo.Rooms.Values)
            {
                var ct = r.ContentType.Value ?? "";
                if (ct.StartsWith("residential"))
                {
                    targetRoom = r;
                    break;
                }
            }
            Assert.That(targetRoom, Is.Not.Null);

            var demolishResult = session.ExecuteCommand(new OneRoof.Domain.Commands.DemolishRoomCommand(targetRoom.Id, force: true));
            Assert.That(demolishResult.Accepted, Is.True);
            _presenter.EnsureRoomViews(session.Topology);

            Assert.That(_presenter.RenderedRoomIds.Contains(targetRoom.Id), Is.False);
            Assert.That(_holder.transform.Find($"RoomView_{targetRoom.Id}"), Is.Null);
            Assert.That(_holder.transform.childCount, Is.LessThan(initialChildCount));
        }

        [Test]
        public void EnsureRoomViews_NewRoomsReceiveConstructionTransition()
        {
            var session = new TowerSimulationSession();
            _presenter.EnsureRoomViews(session.Topology);

            Transform root = null;
            for (var i = 0; i < _holder.transform.childCount; i++)
            {
                var candidate = _holder.transform.GetChild(i);
                if (candidate.name.StartsWith("RoomView_")) { root = candidate; break; }
            }
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<VisualEffectsPresenter>(), Is.Not.Null);
        }
    }
}
