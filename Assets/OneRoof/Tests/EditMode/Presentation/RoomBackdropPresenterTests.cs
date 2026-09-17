using NUnit.Framework;
using OneRoof.Presentation.Architecture;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class RoomBackdropPresenterTests
    {
        [SetUp]
        public void SetUp()
        {
            ArchitecturalFixtureCatalog.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            ArchitecturalFixtureCatalog.ClearCache();
        }

        [Test]
        public void Catalog_ResolvesNonNullSpritesForAllThemes()
        {
            var themes = new[] { "residential", "room:apartment", "commercial:office", "office", "commercial:diner", "lobby" };
            foreach (var theme in themes)
            {
                var sprite = ArchitecturalFixtureCatalog.GetRoomBackdrop(theme);
                Assert.That(sprite, Is.Not.Null, $"Sprite for theme {theme} should not be null");
                Assert.That(sprite.rect.width, Is.GreaterThan(0));
                Assert.That(sprite.rect.height, Is.GreaterThan(0));
            }
        }

        [Test]
        public void Catalog_ResolvesFixturesWithDeclaredPivots()
        {
            var aptDoor = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.ApartmentDoor);
            Assert.That(aptDoor, Is.Not.Null);
            // Door pivot bottom-center (y = 0)
            Assert.That(aptDoor.pivot.y / aptDoor.rect.height, Is.EqualTo(0.0f).Within(0.01f));

            var window = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.ResidentialWindow);
            Assert.That(window, Is.Not.Null);
            // Window pivot center (y = 0.5)
            Assert.That(window.pivot.y / window.rect.height, Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void RoomBackdropPresenter_BuildsSlicedBackdrop_ForDynamicRoomWidths()
        {
            var testCases = new[]
            {
                (width: 1.0f, height: 1.42f, theme: "amenity:stairwell"),
                (width: 2.0f, height: 1.42f, theme: "residential:studio"),
                (width: 4.0f, height: 1.42f, theme: "commercial:office"),
            };

            foreach (var tc in testCases)
            {
                var go = new GameObject("TestRoom");
                try
                {
                    var presenter = go.AddComponent<RoomBackdropPresenter>();
                    presenter.Setup(tc.theme, tc.width, tc.height, -2f, -2f + tc.width, 0f, isWestSide: true);

                    Assert.That(presenter.BackdropRenderer, Is.Not.Null);
                    Assert.That(presenter.BackdropRenderer.drawMode, Is.EqualTo(SpriteDrawMode.Sliced));
                    Assert.That(presenter.BackdropRenderer.size.x, Is.EqualTo(tc.width - 0.04f).Within(0.02f));
                    Assert.That(presenter.BackdropRenderer.size.y, Is.EqualTo(tc.height).Within(0.02f));
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void RoomBackdropPresenter_PlacesDoorAndWindowOnCorrectSides()
        {
            // West-side apartment: corridor is on East (+X), exterior is on West (-X)
            var westRoom = new GameObject("WestRoom");
            try
            {
                var presenter = westRoom.AddComponent<RoomBackdropPresenter>();
                presenter.Setup("residential:studio", 2.0f, 1.42f, -4f, -2f, 0f, isWestSide: true);

                Assert.That(presenter.DoorRenderer, Is.Not.Null);
                Assert.That(presenter.WindowRenderer, Is.Not.Null);

                // Door should be on positive local X (towards shaft / east)
                Assert.That(presenter.DoorRenderer.transform.localPosition.x, Is.GreaterThan(0f));
                // Window should be on negative local X (towards exterior / west)
                Assert.That(presenter.WindowRenderer.transform.localPosition.x, Is.LessThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(westRoom);
            }
        }
    }
}
