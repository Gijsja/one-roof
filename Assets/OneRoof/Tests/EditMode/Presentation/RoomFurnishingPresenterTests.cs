using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Presentation.Furnishings;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class RoomFurnishingPresenterTests
    {
        [SetUp]
        public void SetUp()
        {
            PropCatalog.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            PropCatalog.ClearCache();
        }

        [Test]
        public void PropContentRegistry_DeclaresMandatoryPropsWithValidDimensions()
        {
            var props = PropContentRegistry.GetAll();
            Assert.That(props.Count, Is.GreaterThanOrEqualTo(13));

            foreach (var p in props)
            {
                Assert.That(string.IsNullOrEmpty(p.ContentId), Is.False);
                Assert.That(string.IsNullOrEmpty(p.ResourcePath), Is.False);
                Assert.That(p.CellWidth, Is.GreaterThan(0));
                Assert.That(p.CellHeight, Is.GreaterThan(0));
                Assert.That(p.CompatibleThemes.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void PropCatalog_ResolvesSpritesWithBottomCenterPivot()
        {
            var sampleKeys = new[]
            {
                "prop.furniture.bed.v1",
                "prop.furniture.sofa.v1",
                "prop.workplace.desk.v1",
                "prop.commercial.counter.v1",
                "prop.civic.reception.v1"
            };

            foreach (var key in sampleKeys)
            {
                var sprite = PropCatalog.GetPropSprite(key);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.rect.width, Is.GreaterThan(0));
                Assert.That(sprite.rect.height, Is.GreaterThan(0));
                // Pivot must be anchored bottom-center (y = 0)
                Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.0f).Within(0.01f));
            }
        }

        [Test]
        public void RoomFurnishingPresenter_FurnishesThemedRooms()
        {
            var themes = new[]
            {
                (theme: "residential:studio", minProps: 3),
                (theme: "commercial:office", minProps: 3),
                (theme: "commercial:diner", minProps: 2),
                (theme: "lobby", minProps: 3),
                (theme: "commercial:retail", minProps: 3),
                (theme: "service:clinic", minProps: 3),
                (theme: "service:maintenance_workshop", minProps: 2),
                (theme: "service:security_station", minProps: 2),
                (theme: "utility:electrical_substation", minProps: 2),
                (theme: "utility:water_pump", minProps: 2),
                (theme: "utility:water_riser", minProps: 1),
                (theme: "utility:waste_collection", minProps: 2),
            };

            foreach (var (theme, minProps) in themes)
            {
                var go = new GameObject("FurnishedRoom");
                try
                {
                    var presenter = go.AddComponent<RoomFurnishingPresenter>();
                    presenter.FurnishRoom(theme, width: 3.0f, height: 1.42f, isWestSide: true);

                    Assert.That(presenter.PlacedProps.Count, Is.GreaterThanOrEqualTo(minProps),
                        $"Theme {theme} should have spawned at least {minProps} props");

                    // Every prop must have a SpriteRenderer with valid sprite
                    foreach (var prop in presenter.PlacedProps)
                    {
                        var sr = prop.GetComponent<SpriteRenderer>();
                        Assert.That(sr, Is.Not.Null);
                        Assert.That(sr.sprite, Is.Not.Null);
                        Assert.That(sr.sortingOrder, Is.EqualTo(-2));
                    }
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void RoomFurnishingPresenter_ClearProps_DestroysAllChildren()
        {
            var go = new GameObject("FurnishedRoom");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();
                presenter.FurnishRoom("residential:studio", 3.0f, 1.42f, true);
                Assert.That(presenter.PlacedProps.Count, Is.GreaterThan(0));

                presenter.ClearProps();
                Assert.That(presenter.PlacedProps.Count, Is.EqualTo(0));
                Assert.That(go.transform.childCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomFurnishingPresenter_PropsUseTransparentMaterial_WithAlphaBlending()
        {
            var go = new GameObject("FurnishedRoom");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();
                presenter.FurnishRoom("residential:studio", 3.0f, 1.42f, true);

                Assert.That(presenter.PlacedProps.Count, Is.GreaterThan(0));
                foreach (var prop in presenter.PlacedProps)
                {
                    var sr = prop.GetComponent<SpriteRenderer>();
                    Assert.That(sr.sharedMaterial, Is.Not.Null);
                    Assert.That(sr.sharedMaterial.renderQueue, Is.EqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent),
                        "Prop material must be assigned to Transparent render queue to prevent black alpha bounding boxes.");
                    if (sr.sharedMaterial.HasProperty("_ZWrite"))
                    {
                        Assert.That(sr.sharedMaterial.GetInt("_ZWrite"), Is.EqualTo(0));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomFurnishingPresenter_SyncFurnishings_PlacesSpecifiedPropsAndClearsOnEmpty()
        {
            var go = new GameObject("SyncRoom");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();

                // Empty initial sync
                presenter.SyncFurnishings(System.Array.Empty<string>(), 3.0f, 1.42f, true);
                Assert.That(presenter.PlacedProps.Count, Is.EqualTo(0));

                // Move-in starter: Bed only
                var starter = new[] { "prop.furniture.bed.v1" };
                presenter.SyncFurnishings(starter, 3.0f, 1.42f, true);
                Assert.That(presenter.PlacedProps.Count, Is.EqualTo(1));
                Assert.That(presenter.PlacedProps[0].name, Is.EqualTo("Prop_prop.furniture.bed.v1"));

                // Progressive upgrade from outside: Bed + Planter + Sofa
                var upgraded = new[] { "prop.furniture.bed.v1", "prop.decor.planter.v1", "prop.furniture.sofa.v1" };
                presenter.SyncFurnishings(upgraded, 3.0f, 1.42f, true);
                Assert.That(presenter.PlacedProps.Count, Is.EqualTo(3));

                // Vacate / Move-out: Empty again
                presenter.SyncFurnishings(null, 3.0f, 1.42f, true);
                Assert.That(presenter.PlacedProps.Count, Is.EqualTo(0));
                Assert.That(go.transform.childCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomFurnishingPresenter_NeedsFurnishingSync_AvoidsRedundantRecreation()
        {
            var go = new GameObject("SyncCheckRoom");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();
                var props = new[] { "prop.furniture.bed.v1", "prop.decor.planter.v1" };

                Assert.That(presenter.NeedsFurnishingSync(props), Is.True);
                presenter.SyncFurnishings(props, 3.0f, 1.42f, true);
                var initialBed = presenter.PlacedProps[0];

                // Calling sync with identical list should not recreate GameObjects
                Assert.That(presenter.NeedsFurnishingSync(props), Is.False);
                presenter.SyncFurnishings(props, 3.0f, 1.42f, true);
                Assert.That(presenter.PlacedProps[0], Is.SameAs(initialBed));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomFurnishingPresenter_SetClutterAlpha_UpdatesRenderersCorrectly()
        {
            var go = new GameObject("ClutterAlphaRoom");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();
                var props = new[] { "prop.furniture.bed.v1", "prop.civic.coatrack.v1", "prop.decor.planter.v1" };
                presenter.SyncFurnishings(props, 3.0f, 1.42f, true);

                presenter.SetClutterAlpha(0.5f);
                Assert.That(presenter.ClutterAlpha, Is.EqualTo(0.5f).Within(0.001f));

                foreach (var prop in presenter.PlacedProps)
                {
                    var sr = prop.GetComponent<SpriteRenderer>();
                    Assert.That(sr.color.a, Is.EqualTo(0.5f).Within(0.001f));
                    Assert.That(sr.enabled, Is.True);
                }

                // Alpha near zero disables renderers
                presenter.SetClutterAlpha(0f);
                foreach (var prop in presenter.PlacedProps)
                {
                    var sr = prop.GetComponent<SpriteRenderer>();
                    Assert.That(sr.enabled, Is.False);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoomFurnishingPresenter_SyncFurnishings_PlacesExtraFurnitureAtExactCoordinates()
        {
            var go = new GameObject("CoordinatesRoom");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();
                var props = new[]
                {
                    "prop.civic.coatrack.v1",
                    "prop.furniture.bed.v1",
                    "prop.decor.planter.v1",
                    "prop.furniture.sofa.v1",
                    "prop.furniture.table.v1",
                    "prop.workplace.desk.v1",
                    "prop.furniture.bookcase.v1",
                    "prop.domestic.kitchenette.v1",
                    "prop.furniture.gametable.v1",
                    "prop.furniture.vanitydresser.v1"
                };

                const float width = 3.2f;
                const float height = 1.42f;
                const float baselineY = -height * 0.5f;

                // Test West side
                presenter.SyncFurnishings(props, width, height, isWestSide: true);
                Assert.That(presenter.PlacedProps.Count, Is.EqualTo(10));

                var coatrack = presenter.PlacedProps[0];
                Assert.That(coatrack.transform.localPosition.x, Is.EqualTo(width * 0.5f - 0.25f).Within(0.001f));
                Assert.That(coatrack.transform.localPosition.y, Is.EqualTo(baselineY).Within(0.001f));
                Assert.That(coatrack.transform.localPosition.z, Is.EqualTo(0.42f).Within(0.001f));

                var bed = presenter.PlacedProps[1];
                Assert.That(bed.transform.localPosition.x, Is.EqualTo(-width * 0.5f + 0.65f).Within(0.001f));
                Assert.That(bed.transform.localPosition.z, Is.EqualTo(0.40f).Within(0.001f));

                var table = presenter.PlacedProps[4];
                Assert.That(table.transform.localPosition.x, Is.EqualTo(width * 0.5f - 0.95f).Within(0.001f));
                Assert.That(table.transform.localPosition.z, Is.EqualTo(0.28f).Within(0.001f));

                var desk = presenter.PlacedProps[5];
                Assert.That(desk.transform.localPosition.x, Is.EqualTo(-0.05f).Within(0.001f));
                Assert.That(desk.transform.localPosition.z, Is.EqualTo(0.42f).Within(0.001f));

                var bookcase = presenter.PlacedProps[6];
                Assert.That(bookcase.transform.localPosition.x, Is.EqualTo(0.42f).Within(0.001f));
                Assert.That(bookcase.transform.localPosition.z, Is.EqualTo(0.46f).Within(0.001f));

                var kitchenette = presenter.PlacedProps[7];
                Assert.That(kitchenette.transform.localPosition.x, Is.EqualTo(-0.15f).Within(0.001f));
                Assert.That(kitchenette.transform.localPosition.z, Is.EqualTo(0.44f).Within(0.001f));

                var gametable = presenter.PlacedProps[8];
                Assert.That(gametable.transform.localPosition.x, Is.EqualTo(0.15f).Within(0.001f));
                Assert.That(gametable.transform.localPosition.z, Is.EqualTo(0.36f).Within(0.001f));

                var vanity = presenter.PlacedProps[9];
                Assert.That(vanity.transform.localPosition.x, Is.EqualTo(width * 0.5f - 1.45f).Within(0.001f));
                Assert.That(vanity.transform.localPosition.z, Is.EqualTo(0.38f).Within(0.001f));

                // Verify dock positions for table, coatrack, kitchenette, desk
                Assert.That(presenter.TryGetDockPosition(OneRoof.Domain.Topology.InteractionPointKind.Cook, 0, out var cookPos), Is.True);
                Assert.That(cookPos.x, Is.EqualTo(kitchenette.transform.position.x).Within(0.001f));

                Assert.That(presenter.TryGetDockPosition(OneRoof.Domain.Topology.InteractionPointKind.Work, 0, out var workPos), Is.True);
                Assert.That(presenter.TryGetDockPosition(OneRoof.Domain.Topology.InteractionPointKind.Seat, 0, out var seatPos), Is.True);
                Assert.That(presenter.TryGetDockPosition(OneRoof.Domain.Topology.InteractionPointKind.Browse, 0, out var browsePos), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
