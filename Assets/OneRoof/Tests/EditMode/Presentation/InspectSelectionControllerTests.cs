using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tests.EditMode
{
    [TestFixture]
    public sealed class InspectSelectionControllerTests
    {
        private GameObject _holder;
        private InspectSelectionController _controller;
        private ModeShellSession _modeSession;
        private TowerSimulationSession _simSession;
        private RoomPresenter _roomPresenter;
        private ElevatorBankPresenter _elevatorPresenter;
        private TowerResidentPresenter _residentPresenter;
        private InspectOutlinePresenter _outlinePresenter;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_InspectSelectionController");
            _modeSession = new ModeShellSession();
            _simSession = new TowerSimulationSession();
            _worldMaterial = new Material(Shader.Find("Sprites/Default"));
            _colorBlock = new MaterialPropertyBlock();

            _roomPresenter = new RoomPresenter();
            _roomPresenter.Initialize(_holder.transform, _worldMaterial, _colorBlock);

            _elevatorPresenter = new ElevatorBankPresenter();
            _elevatorPresenter.Initialize(_holder.transform, _worldMaterial, _colorBlock);
            _elevatorPresenter.EnsureShaftViews(5);

            _residentPresenter = new TowerResidentPresenter();
            _residentPresenter.Initialize(_holder.transform);

            _outlinePresenter = _holder.AddComponent<InspectOutlinePresenter>();

            _controller = _holder.AddComponent<InspectSelectionController>();
            _controller.ModeSession = _modeSession;
            _controller.SimulationSession = _simSession;
            _controller.RoomPresenter = _roomPresenter;
            _controller.ElevatorPresenter = _elevatorPresenter;
            _controller.ResidentPresenter = _residentPresenter;
            _controller.OutlinePresenter = _outlinePresenter;
        }

        [TearDown]
        public void TearDown()
        {
            _roomPresenter.Clear();
            _elevatorPresenter.Clear();
            _residentPresenter.Clear();
            if (_worldMaterial != null) Object.DestroyImmediate(_worldMaterial);
            if (_holder != null) Object.DestroyImmediate(_holder);
        }

        [Test]
        public void HitTest_WhenPointingInShaft_ReturnsElevatorShaft()
        {
            var hit = _controller.HitTest(new Vector2(-1.9f, 0f), out var kind, out var id, out var floor, out var bounds, out _, out _);

            Assert.That(hit, Is.True);
            Assert.That(kind, Is.EqualTo(InspectTargetKind.ElevatorShaft));
            Assert.That(id, Is.EqualTo(0));
            Assert.That(floor, Is.EqualTo(0));
            Assert.That(bounds.Contains(new Vector3(-1.9f, 0f, 0f)), Is.True);
        }

        [Test]
        public void HitTest_WhenPointingInRoom_ReturnsRoom()
        {
            var topo = _simSession.Topology;
            Assert.That(topo.Rooms.Count, Is.GreaterThan(0));

            // Pick the first non-shaft room
            Room targetRoom = null;
            foreach (var room in topo.Rooms.Values)
            {
                var ct = room.ContentType.Value ?? "";
                if (!ct.Contains("elevator_shaft"))
                {
                    targetRoom = room;
                    break;
                }
            }
            Assert.That(targetRoom, Is.Not.Null);

            var roomLeft = -2.4f + targetRoom.Bounds.MinX * 0.5f;
            var roomRight = -2.4f + (targetRoom.Bounds.MaxX + 1) * 0.5f;
            var testPos = new Vector2((roomLeft + roomRight) * 0.5f, TowerStructurePresenter.FloorY(targetRoom.Floor));

            var hit = _controller.HitTest(testPos, out var kind, out var id, out var floor, out var bounds, out _, out _);

            Assert.That(hit, Is.True);
            Assert.That(kind, Is.EqualTo(InspectTargetKind.Room));
            Assert.That(id, Is.EqualTo(targetRoom.Id.Value));
            Assert.That(floor, Is.EqualTo(targetRoom.Floor));
            Assert.That(bounds.Contains(new Vector3(testPos.x, testPos.y, 0f)), Is.True);
        }

        [Test]
        public void HitTest_WhenPointingAtResident_PrioritizesResidentOverRoom()
        {
            _residentPresenter.EnsureResidentViews(1);
            var residentView = _residentPresenter.ResidentViews[0];
            residentView.transform.position = new Vector3(2.0f, 0.0f, -0.2f);

            var testPos = new Vector2(2.0f, 0.35f);
            var hit = _controller.HitTest(testPos, out var kind, out var id, out _, out var bounds, out var sprite, out var tr);

            Assert.That(hit, Is.True);
            Assert.That(kind, Is.EqualTo(InspectTargetKind.Resident));
            Assert.That(id, Is.EqualTo(1));
            Assert.That(tr, Is.Not.Null);
            Assert.That(sprite, Is.Not.Null);
        }

        [Test]
        public void HitTest_WhenPointingAtEmptySpace_ReturnsFalse()
        {
            var testPos = new Vector2(100f, 100f);
            var hit = _controller.HitTest(testPos, out var kind, out _, out _, out _, out _, out _);

            Assert.That(hit, Is.False);
            Assert.That(kind, Is.EqualTo(InspectTargetKind.None));
        }

        [Test]
        public void OnModeChanged_WhenLeavingInspectMode_ClearsSelection()
        {
            _modeSession.SwitchMode(InteractionMode.Inspect);
            _modeSession.SelectEntity(42, 1);

            _modeSession.SwitchMode(InteractionMode.Build);

            Assert.That(_controller.SelectedKind, Is.EqualTo(InspectTargetKind.None));
            Assert.That(_controller.SelectedId, Is.Null);
            Assert.That(_outlinePresenter.HasSelectionTarget, Is.False);
        }
    }
}
