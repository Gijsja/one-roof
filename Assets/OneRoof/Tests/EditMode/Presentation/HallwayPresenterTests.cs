using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Topology;
using EntityId = OneRoof.Domain.Identity.EntityId;
using ContentId = OneRoof.Domain.Identity.ContentId;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class HallwayPresenterTests
    {
        [Test]
        public void Frontages_NumberRealEntrancesAndLeaveWindowAndDinerBayUnfilled()
        {
            var holder = new GameObject("Frontage test");
            var material = new Material(Shader.Find("Sprites/Default"));
            var presenter = new HallwayPresenter();
            try
            {
                presenter.Initialize(holder.transform, material, new MaterialPropertyBlock());
                var homeId = new EntityId(15); var dinerId = new EntityId(25); var portalId = new EntityId(101);
                var rooms = new Dictionary<EntityId, Room>
                {
                    [homeId] = new Room(homeId, new ContentId("residential:apartment"), new CellBounds(0, 2, 7), new[] { portalId }, 2),
                    [dinerId] = new Room(dinerId, new ContentId("commercial:diner"), new CellBounds(0, 8, 17), null, 5),
                    [new EntityId(99)] = new Room(new EntityId(99), new ContentId("transit:elevator_shaft"), new CellBounds(0, 0, 1), null, 0)
                };
                var slabs = new Dictionary<int, CellBounds> { [0] = new CellBounds(0, 0, 17) };
                var portals = new Dictionary<EntityId, Portal>
                {
                    [portalId] = new Portal(portalId, PortalType.Door, new CellCoordinate(3, 0), homeId)
                };
                presenter.EnsureHallways(new TowerTopologyProjection(slabs, rooms, portals));
                Assert.That(presenter.NumberedDoorCount, Is.EqualTo(2));
                Assert.That(presenter.OpenBayCount, Is.EqualTo(1));
                Assert.That(presenter.TryGetRoomAccess(homeId, out var home), Is.True);
                Assert.That(home.Number, Is.EqualTo("15"));
                Assert.That(home.DoorX, Is.EqualTo(-2.4f + 3 * .5f + .25f).Within(.001f));
                AssertHole(presenter.FrontageMesh, home.WindowBounds.center);
                // Interior floor and upper wall must remain open at full hallway opacity.
                AssertHole(presenter.FrontageMesh, new Vector2(home.WindowBounds.center.x, TowerStructurePresenter.FloorY(0) - .45f));
                AssertHole(presenter.FrontageMesh, new Vector2(home.WindowBounds.center.x, TowerStructurePresenter.FloorY(0) + .60f));
                Assert.That(presenter.TryGetRoomAccess(dinerId, out var diner), Is.True);
                Assert.That(diner.HasDinerOpening, Is.True);
                Assert.That(diner.WindowBounds.height, Is.GreaterThan(home.WindowBounds.height));
                AssertHole(presenter.FrontageMesh, diner.WindowBounds.center);
                var mesh = presenter.FrontageMesh;
                rooms.Remove(dinerId);
                presenter.EnsureHallways(new TowerTopologyProjection(slabs, rooms, portals));
                Assert.That(presenter.FrontageMesh, Is.SameAs(mesh));
                Assert.That(presenter.NumberedDoorCount, Is.EqualTo(1));
                Assert.That(presenter.OpenBayCount, Is.Zero);
                Assert.That(presenter.TryGetRoomAccess(homeId, out home), Is.True);
                Assert.That(home.Number, Is.EqualTo("15"));
            }
            finally { presenter.Clear(); Object.DestroyImmediate(holder); Object.DestroyImmediate(material); }
        }

        [Test]
        public void FrontageFade_OwnsTransparentMaterialAndCleansUpWithoutMutatingSource()
        {
            var holder = new GameObject("Fade test");
            var source = new Material(Shader.Find("OneRoof/Unlit"));
            var presenter = new HallwayPresenter();
            try
            {
                var originalDepth = source.GetFloat("_ZWrite");
                presenter.Initialize(holder.transform, source, new MaterialPropertyBlock());
                var owned = presenter.FrontageMaterial;
                Assert.That(owned, Is.Not.SameAs(source));
                Assert.That(owned.GetFloat("_ZWrite"), Is.Zero);
                Assert.That(source.GetFloat("_ZWrite"), Is.EqualTo(originalDepth));
                presenter.SetWallAlpha(.25f);
                Assert.That(presenter.WallAlpha, Is.EqualTo(.25f));
                foreach (var renderer in holder.GetComponentsInChildren<MeshRenderer>())
                {
                    var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                    Assert.That(block.GetColor("_BaseColor").a, Is.EqualTo(.25f));
                }
                presenter.SetWallAlpha(-1f);
                Assert.That(presenter.WallAlpha, Is.Zero);
                presenter.Clear();
                Assert.That(owned == null, Is.True);
                Assert.That(source == null, Is.False);
                Assert.That(holder.transform.childCount, Is.Zero);
            }
            finally { presenter.Clear(); Object.DestroyImmediate(holder); Object.DestroyImmediate(source); }
        }

        private static void AssertHole(Mesh mesh, Vector2 point)
        {
            var vertices = mesh.vertices;
            // Every frontage primitive is an axis-aligned quad. No quad may cover the hole's center.
            for (var i = 0; i < vertices.Length; i += 4)
            {
                var bounds = new Rect(vertices[i].x, vertices[i].y, vertices[i + 2].x - vertices[i].x, vertices[i + 2].y - vertices[i].y);
                Assert.That(bounds.Contains(point), Is.False, "Opening must expose the actual room behind it.");
            }
        }

        [Test]
        public void Hallway_FollowsSlabExpansionAndRemoval_WithoutReplacingMesh()
        {
            var holder = new GameObject("Hallway test");
            var material = new Material(Shader.Find("Sprites/Default"));
            var presenter = new HallwayPresenter();
            try
            {
                presenter.Initialize(holder.transform, material, new MaterialPropertyBlock());
                var slabs = new Dictionary<int, CellBounds> { [0] = new CellBounds(0, 0, 3) };
                presenter.EnsureHallways(new TowerTopologyProjection(slabs));
                var root = holder.transform.Find("Hallway Circulation");
                var mesh = root.GetComponent<MeshFilter>().sharedMesh;
                var originalWidth = mesh.bounds.size.x;
                slabs[0] = new CellBounds(0, -4, 7);
                slabs[1] = new CellBounds(1, -4, 7);
                presenter.EnsureHallways(new TowerTopologyProjection(slabs));
                Assert.That(presenter.RenderedFloorCount, Is.EqualTo(2));
                Assert.That(root.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
                Assert.That(mesh.bounds.size.x, Is.GreaterThan(originalWidth));
                Assert.That(mesh.bounds.size.y, Is.GreaterThan(1f));
                presenter.SetVisible(false);
                slabs.Remove(1);
                presenter.EnsureHallways(new TowerTopologyProjection(slabs));
                Assert.That(presenter.RenderedFloorCount, Is.EqualTo(1));
                Assert.That(root.gameObject.activeSelf, Is.False);
                Assert.That(mesh.bounds.size.y, Is.LessThan(.3f));
                presenter.SetVisible(true);
                Assert.That(root.gameObject.activeSelf, Is.True);
                presenter.Clear();
                Assert.That(presenter.RenderedFloorCount, Is.Zero);
                Assert.That(holder.transform.childCount, Is.Zero);
                Assert.That(mesh == null, Is.True, "Owned mesh must be destroyed on cleanup.");
            }
            finally
            {
                presenter.Clear();
                Object.DestroyImmediate(holder);
                Object.DestroyImmediate(material);
            }
        }
    }
}
