using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Architecture;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class FloorThemeCatalogTests
    {
        [SetUp]
        public void SetUp()
        {
            FloorThemeCatalog.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            FloorThemeCatalog.ClearCache();
        }

        [Test]
        public void BuiltInThemes_AreRegisteredAndConfiguredWithStandardDeckThickness()
        {
            var expectedThemeIds = new[]
            {
                FloorThemeCatalog.ConcreteSlab,
                FloorThemeCatalog.HardwoodParquet,
                FloorThemeCatalog.HighTechGridTile,
                FloorThemeCatalog.RetroCheckerboard,
                FloorThemeCatalog.ServiceUtilityGrate,
                FloorThemeCatalog.LuxuryCarpet
            };

            foreach (var id in expectedThemeIds)
            {
                var theme = FloorThemeCatalog.GetTheme(id);
                Assert.That(theme, Is.Not.Null, $"Theme {id} must be registered.");
                Assert.That(theme.Id, Is.EqualTo(id));
                Assert.That(theme.DisplayName, Is.Not.Null.And.Not.Empty);

                // Deck thickness must match 0.27f to fill gap between y - 0.74f and y - 1.01f
                Assert.That(theme.TotalThickness, Is.EqualTo(FloorTheme.StandardDeckThickness).Within(0.001f));
                Assert.That(theme.TreadThickness, Is.EqualTo(0.05f).Within(0.001f));
                Assert.That(theme.CoreThickness, Is.EqualTo(0.17f).Within(0.001f));
                Assert.That(theme.SoffitThickness, Is.EqualTo(0.05f).Within(0.001f));

                // Colors must have non-zero alpha
                Assert.That(theme.SurfaceColor.a, Is.GreaterThan(0.9f));
                Assert.That(theme.CoreColor.a, Is.GreaterThan(0.9f));
                Assert.That(theme.SoffitColor.a, Is.GreaterThan(0.9f));
            }
        }

        [Test]
        public void MapContentTypeToTheme_MapsRoomTypesToAppropriateDeckThemes()
        {
            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("residential:studio"), Is.EqualTo(FloorThemeCatalog.HardwoodParquet));
            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("room:apartment_luxury"), Is.EqualTo(FloorThemeCatalog.HardwoodParquet));

            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("commercial:office"), Is.EqualTo(FloorThemeCatalog.HighTechGridTile));
            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("service:security_station"), Is.EqualTo(FloorThemeCatalog.HighTechGridTile));

            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("commercial:diner"), Is.EqualTo(FloorThemeCatalog.RetroCheckerboard));
            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("commercial:retail"), Is.EqualTo(FloorThemeCatalog.RetroCheckerboard));

            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("utility:electrical_substation"), Is.EqualTo(FloorThemeCatalog.ServiceUtilityGrate));
            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("service:maintenance_workshop"), Is.EqualTo(FloorThemeCatalog.ServiceUtilityGrate));

            Assert.That(FloorThemeCatalog.MapContentTypeToTheme("unknown:content"), Is.EqualTo(FloorThemeCatalog.ConcreteSlab));
        }

        [Test]
        public void ResolveDominantTheme_IdentifiesMajorityRoomThemeOnFloor()
        {
            var residentialRooms = new[]
            {
                new Room(new EntityId(1), new ContentId("residential:studio"), new CellBounds(1, 0, 4), null, 5),
                new Room(new EntityId(2), new ContentId("residential:studio"), new CellBounds(1, 5, 9), null, 5),
                new Room(new EntityId(3), new ContentId("commercial:office"), new CellBounds(1, 10, 12), null, 4)
            };

            var dominant = FloorThemeCatalog.ResolveDominantTheme(residentialRooms);
            Assert.That(dominant, Is.EqualTo(FloorThemeCatalog.HardwoodParquet));
        }

        [Test]
        public void ProceduralFallbackSprite_GeneratesValidSpriteWithTexture()
        {
            var theme = FloorThemeCatalog.GetTheme(FloorThemeCatalog.HardwoodParquet);
            var sprite = FloorThemeCatalog.GetOrLoadSurfaceSprite(theme);

            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.GreaterThanOrEqualTo(16));
            Assert.That(sprite.texture.height, Is.GreaterThanOrEqualTo(16));
            Assert.That(sprite.border, Is.Not.EqualTo(Vector4.zero), "Sliced border should be defined for 9-slice support.");
        }
    }
}
