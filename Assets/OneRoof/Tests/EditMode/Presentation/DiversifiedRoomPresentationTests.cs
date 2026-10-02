using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Architecture;
using OneRoof.Presentation.Furnishings;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class DiversifiedRoomPresentationTests
    {
        [SetUp]
        public void SetUp()
        {
            ArchitecturalFixtureCatalog.ClearCache();
            PropCatalog.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            ArchitecturalFixtureCatalog.ClearCache();
            PropCatalog.ClearCache();
        }

        [Test]
        public void RoomBackdropPresenter_PlacesBathroomDoor_InResidentialRooms()
        {
            var go = new GameObject("ResidentialRoom");
            try
            {
                var presenter = go.AddComponent<RoomBackdropPresenter>();
                presenter.Setup("residential:studio", width: 3.0f, height: 1.42f, worldLeft: -3f, worldRight: 0f, centerY: 0f, isWestSide: true);

                Assert.That(presenter.BathroomDoorRenderer, Is.Not.Null, "Residential room must have BathroomDoorRenderer");
                Assert.That(presenter.BathroomDoorRenderer.sprite, Is.Not.Null, "Bathroom door must have a valid sprite");
                Assert.That(presenter.BathroomDoorRenderer.gameObject.name, Is.EqualTo("BathroomDoor"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomBackdropPresenter_DoesNotPlaceBathroomDoor_InCommercialOffices()
        {
            var go = new GameObject("OfficeRoom");
            try
            {
                var presenter = go.AddComponent<RoomBackdropPresenter>();
                presenter.Setup("commercial:office", width: 4.0f, height: 1.42f, worldLeft: -4f, worldRight: 0f, centerY: 0f, isWestSide: true);

                Assert.That(presenter.BathroomDoorRenderer, Is.Null, "Commercial office should not have BathroomDoorRenderer");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomBackdropPresenter_DiversifiesWallpaperPalette_AcrossAdjoiningResidentialRooms()
        {
            var colors = new System.Collections.Generic.List<Color>();

            for (var i = 0; i < 4; i++)
            {
                var go = new GameObject($"Room_{i}");
                try
                {
                    var presenter = go.AddComponent<RoomBackdropPresenter>();
                    var left = -6f + i * 3f;
                    presenter.Setup("residential:apartment", width: 3.0f, height: 1.42f, worldLeft: left, worldRight: left + 3f, centerY: 0f, isWestSide: true);
                    colors.Add(presenter.BackdropRenderer.color);
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            // Verify that not all rooms share the exact same wallpaper color
            var distinctColorCount = colors.Select(c => $"{c.r:F2},{c.g:F2},{c.b:F2}").Distinct().Count();
            Assert.That(distinctColorCount, Is.GreaterThan(1), "Adjacent rooms should exhibit diversified wallpaper palettes");
        }

        [Test]
        public void RoomBackdropPresenter_PlacesSecondaryWindow_InWideThreeByThreeRooms()
        {
            var go = new GameObject("GrandSuite");
            try
            {
                var presenter = go.AddComponent<RoomBackdropPresenter>();
                presenter.Setup("residential:suite", width: 4.5f, height: 1.42f, worldLeft: -4.5f, worldRight: 0f, centerY: 0f, isWestSide: true);

                Assert.That(presenter.WindowRenderer, Is.Not.Null);
                Assert.That(presenter.WindowSecondaryRenderer, Is.Not.Null, "3x3 grand suites should have a secondary window");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomFurnishingPresenter_ProvidesDistinctSleepDockPositions_InThreeByThreeSuite()
        {
            var go = new GameObject("SuiteFurnishings");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();
                presenter.FurnishRoom("residential:suite", width: 4.5f, height: 1.42f, isWestSide: true);

                // Slot 0, 1, 2 must all resolve valid dock positions
                Assert.That(presenter.TryGetDockPosition(InteractionPointKind.Sleep, 0, out var slot0), Is.True);
                Assert.That(presenter.TryGetDockPosition(InteractionPointKind.Sleep, 1, out var slot1), Is.True);
                Assert.That(presenter.TryGetDockPosition(InteractionPointKind.Sleep, 2, out var slot2), Is.True);

                // All 3 residents must sleep at distinct positions without overlapping
                Assert.That(Vector3.Distance(slot0, slot1), Is.GreaterThan(0.2f), "Resident 0 and 1 must have distinct sleep positions");
                Assert.That(Vector3.Distance(slot0, slot2), Is.GreaterThan(0.2f), "Resident 0 and 2 must have distinct sleep positions");
                Assert.That(Vector3.Distance(slot1, slot2), Is.GreaterThan(0.2f), "Resident 1 and 2 must have distinct sleep positions");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildingCatalog_IncludesMicroPodAndSuiteDefinitions()
        {
            Assert.That(BuildingCatalog.TryGetRoomDefinition("residential:micro", out var micro), Is.True);
            Assert.That(micro.WidthInCells, Is.EqualTo(3));
            Assert.That(micro.Capacity, Is.EqualTo(1));

            Assert.That(BuildingCatalog.TryGetRoomDefinition("residential:suite", out var suite), Is.True);
            Assert.That(suite.WidthInCells, Is.EqualTo(9));
            Assert.That(suite.Capacity, Is.EqualTo(3));
        }
    }
}
