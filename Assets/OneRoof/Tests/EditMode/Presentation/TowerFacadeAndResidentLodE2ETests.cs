using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Overlays;
using OneRoof.Application.Transit;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using Object = UnityEngine.Object;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tests.EditMode
{
    // Facade integration coverage; resident LOD scale and transition coverage lives in TowerResidentLodTests.
    public sealed class TowerFacadeAndResidentLodE2ETests
    {
        private GameObject _holder;
        private Material _material;
        private BuildingExteriorPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Facade integration test");
            _material = new Material(Shader.Find("OneRoof/Unlit"));
            _presenter = new BuildingExteriorPresenter();
            _presenter.Initialize(_holder.transform, _material, new MaterialPropertyBlock());
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            Object.DestroyImmediate(_holder);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void ThirtyFloors_SubmitFlankGlazingAndLightConesAsTwoBatches()
        {
            _presenter.EnsureExteriorViews(Topology(floors: 30));
            var batchCount = 0;
            foreach (var renderer in _presenter.Root.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name.StartsWith("Batched"))
                {
                    Assert.That(renderer.enabled, Is.True);
                    Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(100));
                    batchCount++;
                }
            }
            Assert.That(batchCount, Is.EqualTo(2));
            foreach (var cone in _presenter.VolumetricCones)
                Assert.That(cone.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        private static TowerTopologyProjection Topology(int middleMin = -14, int floors = 3)
        {
            var slabs = new Dictionary<int, CellBounds>();
            for (var f = 0; f < floors; f++) slabs[f] = new CellBounds(f, f == 1 ? middleMin : -14, 16);
            var room = new Room(new EntityId(10), new ContentId("room:residential"),
                new CellBounds(1, -14, -13), Array.Empty<EntityId>(), 2);
            return new TowerTopologyProjection(slabs, new Dictionary<EntityId, Room> { [room.Id] = room });
        }

        private static TowerProjection Snapshot(params TransitResidentProjection[] residents) =>
            new TowerProjection(1, 0, 0, 0, residents, Array.Empty<ElevatorProjection>());

        [Test]
        public void Occupancy_LightsOnlyBaysOverlappingTheOccupiedRoom_ThenDarkensOnDeparture()
        {
            _presenter.EnsureExteriorViews(Topology());
            var night = new DayPhase(1, 0, 0, true);
            _presenter.UpdateLighting(night, Snapshot(new TransitResidentProjection(1, 1,
                TransitResidentStatus.InRoom, 1, -14, 10)));
            Assert.That(_presenter.GetFenestrationColor(1, 0), Is.EqualTo(BuildingExteriorPresenter.WindowNightLitColor));
            Assert.That(_presenter.GetFenestrationColor(1, 1), Is.EqualTo(BuildingExteriorPresenter.WindowNightDarkColor));
            _presenter.UpdateLighting(night, Snapshot());
            Assert.That(_presenter.GetFenestrationColor(1, 0), Is.EqualTo(BuildingExteriorPresenter.WindowNightDarkColor));
        }

        [TestCase(TransitResidentStatus.Outside)]
        [TestCase(TransitResidentStatus.Walking)]
        [TestCase(TransitResidentStatus.Queued)]
        [TestCase(TransitResidentStatus.Riding)]
        public void TransitResidents_WithRoomReference_DoNotLightRooms(TransitResidentStatus status)
        {
            _presenter.EnsureExteriorViews(Topology());
            _presenter.UpdateLighting(new DayPhase(1, 0, 0, true),
                Snapshot(new TransitResidentProjection(1, 1, status, 1, -14, 10)));
            Assert.That(_presenter.GetFenestrationColor(1, 0), Is.EqualTo(BuildingExteriorPresenter.WindowNightDarkColor));
        }

        [TestCase(false, 1f, 0f)]
        [TestCase(true, 0.5f, 0.5f)]
        [TestCase(true, 1f, 1f)]
        public void OccupiedWindows_FollowFloorPower(bool connected, float voltage, float expectedStrength)
        {
            _presenter.EnsureExteriorViews(Topology());
            var utilities = new UtilitiesOverlayProjection(new[]
            {
                new UtilitiesFloorProjection(1, voltage, "None", 1f, "None", "None", 0, powerConnected: connected)
            }, null);
            _presenter.UpdateLighting(new DayPhase(1, 0, 0, true),
                Snapshot(new TransitResidentProjection(1, 1, TransitResidentStatus.InRoom, 1, -14, 10)), utilities);
            Assert.That(_presenter.GetFenestrationColor(1, 0), Is.EqualTo(Color.Lerp(
                BuildingExteriorPresenter.WindowNightDarkColor, BuildingExteriorPresenter.WindowNightLitColor, expectedStrength)));
        }

        [Test]
        public void MiddleFloorSlabChanges_RebuildGeometry_AndUnchangedSnapshotsReuseMeshes()
        {
            _presenter.EnsureExteriorViews(Topology());
            var previous = _presenter.FramingMesh;
            _presenter.EnsureExteriorViews(Topology());
            Assert.That(_presenter.FramingMesh, Is.SameAs(previous));
            _presenter.EnsureExteriorViews(Topology(-10));
            Assert.That(_presenter.FramingMesh, Is.Not.SameAs(previous));
            Assert.That(_presenter.Root.Find("Left Facade/Left Wall 1").localPosition.x,
                Is.EqualTo(-2.4f - 5f - BuildingExteriorPresenter.WallThickness * 0.5f).Within(0.001f));
        }

        [Test]
        public void FloorRemoval_RemovesOldWallGeometry()
        {
            _presenter.EnsureExteriorViews(Topology());
            _presenter.EnsureExteriorViews(Topology(floors: 2));
            Assert.That(_presenter.Root.Find("Left Facade/Left Wall 2"), Is.Null);
            Assert.That(_presenter.LeftWindowFrames.Count, Is.EqualTo(2));
        }

        [Test]
        public void PartialFade_UsesBlendingWithoutChangingSharedWorldMaterial()
        {
            _presenter.EnsureExteriorViews(Topology());
            _presenter.SetFacadeEnvelopeAlpha(0.5f);
            var material = _presenter.FramingObject.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(material.GetInt("_SrcBlend"), Is.EqualTo((int)UnityEngine.Rendering.BlendMode.SrcAlpha));
            Assert.That(material.GetInt("_ZWrite"), Is.Zero);
            Assert.That(_material.GetInt("_ZWrite"), Is.EqualTo(1));
            Assert.That(_presenter.FenestrationObject.GetComponent<MeshRenderer>().sharedMaterial,
                Is.EqualTo(_presenter.GlazingMaterial));
            Assert.That(_presenter.VitrineMesh.triangles.Length, Is.EqualTo(_presenter.VitrineMesh.vertexCount / 4 * 6));
            _presenter.SetFacadeEnvelopeAlpha(1f);
            Assert.That(_presenter.FramingObject.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_material));
        }
    }
}
