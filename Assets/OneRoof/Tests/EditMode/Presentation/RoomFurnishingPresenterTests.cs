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
    }
}
