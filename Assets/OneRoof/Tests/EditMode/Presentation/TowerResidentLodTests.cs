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

        [TestCase(9f, 60, 240, 0)]
        [TestCase(17f, 0, 300, 0)]
        [TestCase(25f, 0, 0, 300)]
        public void ConfiguredFadeRanges_AreCapturedBeforeInitialization(float zoom, int rigs, int sprites, int macro)
        {
            _presenter.Clear();
            var settings = new ResidentPresentationSettings
            {
                RigFadeStart = 10f, RigFadeEnd = 14f,
                MacroFadeStart = 20f, MacroFadeEnd = 22f
            };
            _presenter = new TowerResidentPresenter { ViewPool = _pool, PresentationCamera = _camera };
            _presenter.ConfigurePresentation(settings);
            // Authoring changes after capture must not alter the initialized presentation.
            settings.RigFadeStart = 6.5f; settings.RigFadeEnd = 8f;
            settings.MacroFadeStart = 16f; settings.MacroFadeEnd = 18f;
            _presenter.Initialize(_holder.transform);
            _presenter.EnsureResidentViews(300);
            _camera.orthographicSize = zoom;
            _presenter.UpdateResidentPositions(Snapshot(), null, 0f);
            Assert.That(_presenter.VisibleResidentCount, Is.EqualTo(300));
            Assert.That(_presenter.VisibleRigCount, Is.EqualTo(rigs));
            Assert.That(_presenter.VisibleSpriteCount, Is.EqualTo(sprites));
            Assert.That(_presenter.VisibleMacroCount, Is.EqualTo(macro));
        }

        [TestCase(5f)]
        [TestCase(10f)]
        [TestCase(20f)]
        public void OutsideAndUndergroundResidents_AreExcludedAtEveryLodTier(float zoom)
        {
            _camera.orthographicSize = zoom;
            _presenter.InspectedResidentId = 2;
            var residents = new[]
            {
                new TransitResidentProjection(1, 0, TransitResidentStatus.Outside),
                new TransitResidentProjection(2, 0, TransitResidentStatus.Walking),
                new TransitResidentProjection(3, 0, TransitResidentStatus.Walking)
            };
            _presenter.UpdateResidentPositions(new TowerProjection(0, 0, 0, 0,
                residents, Array.Empty<ElevatorProjection>()), null, 0f,
                undergroundCrewResidentIds: new[] { 2 });
            Assert.That(_presenter.VisibleResidentCount, Is.EqualTo(1));
            Assert.That(_pool.TryGetView(1, out _), Is.False);
            Assert.That(_pool.TryGetView(2, out _), Is.False);
            Assert.That(_presenter.TryGetResidentView(0, out _, out _, out _), Is.False);
            Assert.That(_presenter.TryGetResidentView(1, out _, out _, out _), Is.False);
            Assert.That(_presenter.TryGetResidentView(2, out _, out _, out _), Is.True);
        }

        [TestCase(3)]
        [TestCase(80)]
        public void ConfiguredCapacity_DeterminesRigCount_AndPreservesInspectedPriority(int capacity)
        {
            _pool.MaxCapacity = capacity;
            _camera.orthographicSize = 5f;
            _presenter.InspectedResidentId = 300;
            _presenter.UpdateResidentPositions(Snapshot(), null, 0f);
            Assert.That(_pool.ActiveCount, Is.EqualTo(capacity));
            Assert.That(_presenter.VisibleRigCount, Is.EqualTo(capacity));
            Assert.That(_pool.TryGetView(300, out _), Is.True);
            Assert.That(_presenter.VisibleResidentCount, Is.EqualTo(300));
        }

        private static TowerProjection SingleResident(int id, float cellX)
        {
            return new TowerProjection(0, 0, 0, 0,
                new[] { new TransitResidentProjection(id, 0, TransitResidentStatus.Walking, 0, cellX) },
                Array.Empty<ElevatorProjection>());
        }

        [Test]
        public void ResidentTurnover_ReusesLodRoot_AndResetsFacingAppearanceAndPicking()
        {
            _camera.orthographicSize = 10f;
            _presenter.UpdateResidentPositions(SingleResident(1, 8f), null, 1f);
            _presenter.UpdateResidentPositions(SingleResident(1, 6f), null, 1f);
            Assert.That(_presenter.TryGetResidentView(0, out _, out _, out var previousRoot), Is.True);
            Assert.That(previousRoot.localScale.x, Is.LessThan(0f));
            var renderer = previousRoot.GetComponentInChildren<SpriteRenderer>();
            renderer.color = Color.clear;
            renderer.transform.localPosition = Vector3.one;
            previousRoot.localRotation = Quaternion.Euler(0f, 0f, 30f);

            _presenter.UpdateResidentPositions(SingleResident(2, 10f), null, 0f);
            Assert.That(_presenter.TryGetResidentView(0, out _, out var sprite, out var replacementRoot), Is.True);
            Assert.That(replacementRoot, Is.SameAs(previousRoot));
            Assert.That(replacementRoot.name, Is.EqualTo("Resident LOD 2"));
            Assert.That(replacementRoot.localScale.x, Is.GreaterThan(0f));
            Assert.That(replacementRoot.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(sprite, Is.SameAs(ResidentSpriteCatalog.GetResidentSprite(1)));
            Assert.That(renderer.color.a, Is.EqualTo(1f));
            Assert.That(renderer.transform.localPosition.x, Is.Zero);
            Assert.That(renderer.transform.localPosition.y, Is.EqualTo(Mathf.Sin(2f) * 0.018f).Within(0.0001f));
            Assert.That(_holder.transform.childCount, Is.EqualTo(2), "One LOD root and the macro batch only.");
        }

        [Test]
        public void EmptyPopulation_RetainsInactiveLodForReuse_AndClearDestroysIt()
        {
            _camera.orthographicSize = 10f;
            _presenter.UpdateResidentPositions(SingleResident(1, 8f), null, 0f);
            Assert.That(_presenter.TryGetResidentView(0, out _, out _, out var root), Is.True);
            _presenter.UpdateResidentPositions(new TowerProjection(0, 0, 0, 0,
                Array.Empty<TransitResidentProjection>(), Array.Empty<ElevatorProjection>()), null, 0f);
            Assert.That(root.gameObject.activeSelf, Is.False);
            Assert.That(_presenter.TryGetResidentView(0, out _, out _, out _), Is.False);
            Assert.That(_presenter.TryGetResidentAt(Vector2.zero, 100f, out _, out _, out _, out _), Is.False);
            _presenter.Clear();
            Assert.That(root == null, Is.True);
            Assert.That(_holder.transform.childCount, Is.Zero);
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
