using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Application.Tower;
using OneRoof.Application.Modes;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using OneRoof.Presentation.Population;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerResidentLodTests
    {
        private GameObject _holder, _cameraObject;
        private TowerResidentPresenter _presenter;
        private Camera _camera;
        private NpcViewPool _pool;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("LOD coverage");
            _pool = _holder.AddComponent<NpcViewPool>(); _pool.MaxCapacity = 60;
            _cameraObject = new GameObject("LOD camera");
            _camera = _cameraObject.AddComponent<Camera>();
            _camera.orthographic = true; _camera.aspect = 1.8f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _presenter = new TowerResidentPresenter { ViewPool = _pool, PresentationCamera = _camera };
            _presenter.Initialize(_holder.transform); _presenter.EnsureResidentViews(300);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Clear();
            UnityEngine.Object.DestroyImmediate(_holder); UnityEngine.Object.DestroyImmediate(_cameraObject);
        }

        private static TowerProjection Snapshot()
        {
            var residents = new List<TransitResidentProjection>();
            for (var i = 0; i < 300; i++) residents.Add(new TransitResidentProjection(i + 1, 0,
                TransitResidentStatus.Walking, 0, -10f + (i % 22)));
            return new TowerProjection(0, 0, 0, 0, residents, Array.Empty<ElevatorProjection>());
        }

        [TestCase(5f, 60, 240, 0)]
        [TestCase(10f, 0, 300, 0)]
        [TestCase(20f, 0, 0, 300)]
        public void ThreeHundredResidents_AllRepresentedAcrossLodTiers(float zoom, int rigs, int sprites, int macro)
        {
            _camera.orthographicSize = zoom;
            _presenter.UpdateResidentPositions(Snapshot(), null, 0f);
            Assert.That(_presenter.VisibleResidentCount, Is.EqualTo(300));
            Assert.That(_presenter.VisibleRigCount, Is.EqualTo(rigs));
            Assert.That(_presenter.VisibleSpriteCount, Is.EqualTo(sprites));
            Assert.That(_presenter.VisibleMacroCount, Is.EqualTo(macro));
            Assert.That(_pool.TotalInstantiatedCount, Is.LessThanOrEqualTo(60));
            if (macro > 0) Assert.That(_presenter.MacroMesh.vertexCount, Is.EqualTo(300 * 16));
        }

        [Test]
        public void ZoomRoundTrip_DoesNotLoseResidents_AndCanPickMacroResident()
        {
            foreach (var zoom in new[] { 5f, 7f, 7.9f, 8f, 10f, 16f, 17f, 18f, 30f, 7f, 5f })
            {
                _camera.orthographicSize = zoom;
                _presenter.UpdateResidentPositions(Snapshot(), null, 0f);
                Assert.That(_presenter.VisibleResidentCount, Is.EqualTo(300), "Zoom " + zoom);
            }
            _camera.orthographicSize = 30f;
            _presenter.UpdateResidentPositions(Snapshot(), null, 0f);
            Assert.That(_presenter.TryGetResidentView(299, out _, out _, out var transform), Is.True);
            Assert.That(_presenter.TryGetResidentAt((Vector2)transform.position + new Vector2(0f, 0.3f),
                0.2f, out _, out _, out _, out _), Is.True);
        }

        [Test]
        public void OccupiedRoom_IsCulledAtItsInteriorPosition_NotItsStaleCorridorCoordinate()
        {
            _camera.orthographicSize = 10f;
            var room = new Room(new EntityId(10), new OneRoof.Domain.Identity.ContentId("residential:apartment"),
                new CellBounds(0, 1, 6), Array.Empty<EntityId>(), 4);
            var topology = new TowerTopologyProjection(new Dictionary<int, CellBounds> { [0] = new CellBounds(0, -14, 16) },
                new Dictionary<EntityId, Room> { [room.Id] = room });
            var residents = new[] { new TransitResidentProjection(1, 0, TransitResidentStatus.InRoom, 0, 1000f, 10),
                new TransitResidentProjection(2, 0, TransitResidentStatus.Outside),
                new TransitResidentProjection(3, 0, TransitResidentStatus.Walking, 0, 1000f) };
            _presenter.UpdateResidentPositions(new TowerProjection(0, 0, 0, 0, residents, Array.Empty<ElevatorProjection>()), topology, 0f);
            Assert.That(_presenter.VisibleResidentCount, Is.EqualTo(1));
            Assert.That(_presenter.TryGetResidentView(0, out _, out _, out _), Is.True);
            Assert.That(_presenter.TryGetResidentView(1, out _, out _, out _), Is.False);
        }

        [Test]
        public void InspectedResident_GetsARigAtMacroZoom_AndRemovedIdsReleaseTheirViews()
        {
            _camera.orthographicSize = 30f; _presenter.InspectedResidentId = 300;
            _presenter.UpdateResidentPositions(Snapshot(), null, 0f);
            Assert.That(_pool.TryGetView(300, out var inspected), Is.True);
            foreach (var limb in inspected.SkeletalHierarchy.LimbRenderers.Values)
                Assert.That(limb.color.a, Is.EqualTo(1f));
            Assert.That(_presenter.VisibleResidentCount, Is.EqualTo(300));
            _presenter.UpdateResidentPositions(new TowerProjection(1, 0, 0, 0,
                Array.Empty<TransitResidentProjection>(), Array.Empty<ElevatorProjection>()), null, 0f);
            Assert.That(_pool.ActiveCount, Is.Zero);
            Assert.That(_presenter.VisibleResidentCount, Is.Zero);
            Assert.That(_presenter.MacroMesh.vertexCount, Is.Zero);
        }

        [TestCase(true, FacadeDisplayMode.LockedCutaway)]
        [TestCase(false, FacadeDisplayMode.LockedFacade)]
        public void FacadeControl_TogglesTheActualAutoView(bool visible, FacadeDisplayMode expected)
        {
            var bar = _holder.AddComponent<OneRoof.UI.Modes.ModeShellBarController>();
            bar.Session = new ModeShellSession(); bar.IsFacadeVisible = visible;
            bar.OnFacadeToggleRequested();
            Assert.That(bar.Session.Projection().FacadeMode, Is.EqualTo(expected));
            bar.OnFacadeToggleRequested();
            Assert.That(bar.Session.Projection().FacadeMode, Is.EqualTo(expected == FacadeDisplayMode.LockedFacade
                ? FacadeDisplayMode.LockedCutaway : FacadeDisplayMode.LockedFacade));
        }

        [Test]
        public void FacadeToggle_CyclesAndNotifies_WithoutChangingInteractionMode()
        {
            var modes = new ModeShellSession(); var notifications = 0;
            modes.ModeChanged += _ => notifications++;
            Assert.That(modes.Projection().FacadeMode, Is.EqualTo(FacadeDisplayMode.Auto));
            modes.ToggleFacadeMode(); Assert.That(modes.Projection().FacadeMode, Is.EqualTo(FacadeDisplayMode.LockedFacade));
            modes.ToggleFacadeMode(); Assert.That(modes.Projection().FacadeMode, Is.EqualTo(FacadeDisplayMode.LockedCutaway));
            modes.ToggleFacadeMode(); Assert.That(modes.Projection().FacadeMode, Is.EqualTo(FacadeDisplayMode.Auto));
            Assert.That(modes.CurrentMode, Is.EqualTo(InteractionMode.Inspect));
            Assert.That(notifications, Is.EqualTo(3));
        }
    }
}
