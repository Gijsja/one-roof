using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Underground;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class UndercityArchitectureTests
    {
        private GameObject _holder;
        private BuildingExteriorPresenter _presenter;
        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("UndercityArchitectureTest");
            _material = new Material(Shader.Find("Sprites/Default"));
            _presenter = new BuildingExteriorPresenter();
            _presenter.Initialize(_holder.transform, _material, new MaterialPropertyBlock());
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Clear();
            Object.DestroyImmediate(_holder);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void Corridor_MatchesTowerStorey_AndLeavesResidentHeadroom()
        {
            _presenter.EnsureExteriorViews(null, CreateLayout());
            var corridor = _presenter.Root.Find("Ground Dressing/Undercity/Underground Corridor 16_1");
            var shell = corridor.GetComponent<MeshRenderer>().bounds;
            Assert.That(shell.size.x, Is.EqualTo(GridPlacementController.DefaultCellWidth * 1.005f).Within(0.01f));
            Assert.That(shell.size.y, Is.EqualTo(TowerStructurePresenter.DefaultFloorHeight * 1.005f).Within(0.01f));
            var model = new GameObject("Resident scale reference");
            try
            {
                var resident = model.AddComponent<OneRoof.Presentation.Population.NpcSkeletalHierarchy>();
                resident.Initialize(3);
                resident.SetCaption(null);
                resident.SetBubbleVisible(false);
                model.transform.position = new Vector3(corridor.position.x,
                    corridor.Find("Walkway").GetComponent<MeshRenderer>().bounds.max.y, -0.1f);
                var body = new Bounds(model.transform.position, Vector3.zero);
                foreach (var renderer in model.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy) body.Encapsulate(renderer.bounds);
                Assert.That(body.max.y, Is.LessThan(corridor.Find("Concrete Ceiling").GetComponent<MeshRenderer>().bounds.min.y - 0.10f),
                    "A full-size Resident must fit below the corridor ceiling with visible headroom.");
            }
            finally { Object.DestroyImmediate(model); }
        }

        [Test]
        public void ConnectedGenerator_HasReadableArchitectureBehindCrew_AndKeepsRoomIdentity()
        {
            _presenter.EnsureExteriorViews(null, CreateLayout());
            var room = _presenter.Root.Find("Ground Dressing/Undercity/Underground Room 11");
            var housing = room.Find("Generator Equipment/Machine Housing");
            Assert.That(housing, Is.Not.Null);
            Assert.That(housing.localPosition.z, Is.GreaterThan(0.12f), "Equipment stays behind resident crew.");
            Assert.That(room.Find("Room Label").GetComponent<TextMesh>().text, Is.EqualTo("Generator"));
            Assert.That(room.Find("Connection Status"), Is.Not.Null);
            Assert.That(room.Find("Left Retaining Wall").localPosition.x, Is.LessThan(-0.8f));
            Assert.That(room.Find("Right Retaining Wall").localPosition.x, Is.GreaterThan(0.8f));
            Assert.That(room.Find("Work Light").localPosition.y, Is.GreaterThan(housing.localPosition.y));
            Assert.That(housing.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_material));
            var housingBounds = housing.GetComponent<MeshRenderer>().bounds;
            Assert.That(housingBounds.size.x / housingBounds.size.y,
                Is.EqualTo(housing.localScale.x / housing.localScale.y).Within(0.01f),
                "The Tower storey mapping must preserve machinery proportions in world space.");
            var equipmentFloor = room.Find("Generator Equipment/Machine Plinth").GetComponent<MeshRenderer>().bounds.min.y;
            var roomFloor = room.Find("Room Floor").GetComponent<MeshRenderer>().bounds.max.y;
            Assert.That(equipmentFloor, Is.EqualTo(roomFloor).Within(0.01f), "The equipment rests on the finished room floor.");
        }

        [Test]
        public void AdjacentCorridors_KeepContinuousWalkway_WithSparseWorkFixtures()
        {
            _presenter.EnsureExteriorViews(null, CreateLayout());
            var even = _presenter.Root.Find("Ground Dressing/Undercity/Underground Corridor 16_1");
            var odd = _presenter.Root.Find("Ground Dressing/Undercity/Underground Corridor 17_1");
            Assert.That(even.Find("Work Light").gameObject.activeSelf, Is.True);
            Assert.That(odd.Find("Work Light").gameObject.activeSelf, Is.False);
            var walkway = even.Find("Walkway").GetComponent<MeshRenderer>().bounds;
            var nextWalkway = odd.Find("Walkway").GetComponent<MeshRenderer>().bounds;
            Assert.That(walkway.max.x, Is.GreaterThanOrEqualTo(nextWalkway.min.x));
            Assert.That(even.Find("Concrete Ceiling"), Is.Not.Null);
        }

        [Test]
        public void RefreshReusesArchitecture_AndRemovingProjectionRetiresAllDressing()
        {
            var layout = CreateLayout();
            _presenter.EnsureExteriorViews(null, layout);
            var room = _presenter.Root.Find("Ground Dressing/Undercity/Underground Room 11");
            var housing = room.Find("Generator Equipment/Machine Housing");
            var childCount = room.GetComponentsInChildren<Transform>(true).Length;
            var equipmentMesh = room.Find("Generator Equipment/Engine Alternator and Control").GetComponent<MeshFilter>().sharedMesh;
            var corridorMesh = _presenter.Root.Find("Ground Dressing/Undercity/Underground Corridor 16_1/Light Wall Wash").GetComponent<MeshFilter>().sharedMesh;
            _presenter.EnsureExteriorViews(null, layout);
            Assert.That(_presenter.Root.Find("Ground Dressing/Undercity/Underground Room 11/Generator Equipment/Machine Housing"), Is.SameAs(housing));
            Assert.That(room.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(childCount));
            _presenter.EnsureExteriorViews(null, null);
            Assert.That(_presenter.Root.Find("Ground Dressing/Undercity/Underground Room 11"), Is.Null);
            Assert.That(_presenter.Root.Find("Ground Dressing/Undercity/Underground Corridor 16_1"), Is.Null);
            Assert.That(equipmentMesh == null, Is.True, "Removed room must release its owned equipment mesh.");
            Assert.That(corridorMesh == null, Is.True, "Removed corridor must release its owned light mesh.");
        }

        [Test]
        public void BelowRoomCorridor_CreatesFloorHatchAtAdjoiningColumn_WithoutSideDoor()
        {
            var roomCells = new[]
            {
                new UndergroundCellSaveData { x = 18, depth = 1 },
                new UndergroundCellSaveData { x = 19, depth = 1 },
                new UndergroundCellSaveData { x = 19, depth = 2 }
            };
            var layout = UndergroundDigState.FromSaveData(roomCells, roomCells,
                corridors: new[] { roomCells[2] }, rooms: new[]
                {
                    new UndergroundRoomSaveData { id = 11, type = (int)UndergroundRoomType.Generator,
                        x = 18, depth = 1, width = 2, height = 1 }
                });
            _presenter.EnsureExteriorViews(null, new UndergroundDigProjection(layout));
            var room = _presenter.Root.Find("Ground Dressing/Undercity/Underground Room 11");
            var mesh = room.Find("Connected Portals and Hatches").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.GreaterThan(0));
            foreach (var vertex in mesh.vertices)
            {
                Assert.That(vertex.x, Is.InRange(0.19f, 0.81f), "Hatch belongs only to the right adjoining cell.");
                Assert.That(vertex.y, Is.LessThan(-0.20f), "A corridor below connects through the floor.");
            }
            Assert.That(room.Find("Left Retaining Wall").gameObject.activeSelf, Is.True);
            Assert.That(room.Find("Right Retaining Wall").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void IsolatedRoom_HasNoPortalsEvenWhenEarthAroundItIsExcavated()
        {
            var cells = new[]
            {
                new UndergroundCellSaveData { x = 17, depth = 1 },
                new UndergroundCellSaveData { x = 18, depth = 1 },
                new UndergroundCellSaveData { x = 19, depth = 1 }
            };
            var layout = UndergroundDigState.FromSaveData(cells, cells, rooms: new[]
            {
                new UndergroundRoomSaveData { id = 11, type = (int)UndergroundRoomType.AccessHub,
                    x = 18, depth = 1, width = 2, height = 1 }
            });
            _presenter.EnsureExteriorViews(null, new UndergroundDigProjection(layout));
            var room = _presenter.Root.Find("Ground Dressing/Undercity/Underground Room 11");
            Assert.That(room.Find("Connected Portals and Hatches").GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.Zero);
            Assert.That(room.Find("Access Hub Equipment/Lockers and Bench"), Is.Not.Null);
        }

        private static UndergroundDigProjection CreateLayout()
        {
            var cells = new[]
            {
                new UndergroundCellSaveData { x = 16, depth = 0 },
                new UndergroundCellSaveData { x = 16, depth = 1 },
                new UndergroundCellSaveData { x = 17, depth = 1 },
                new UndergroundCellSaveData { x = 18, depth = 1 },
                new UndergroundCellSaveData { x = 19, depth = 1 }
            };
            var state = UndergroundDigState.FromSaveData(cells, cells,
                corridors: new[] { cells[1], cells[2] }, shaft: new[] { cells[0] }, core: cells[0],
                rooms: new[] { new UndergroundRoomSaveData { id = 11, type = (int)UndergroundRoomType.Generator,
                    x = 18, depth = 1, width = 2, height = 1 } });
            return new UndergroundDigProjection(state);
        }
    }
}
