using System.Linq;
using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Furnishings;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    [TestFixture]
    public sealed class PropContentRegistryTests
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
        public void PropContentRegistry_ResolvesLoungeConversationSet()
        {
            const string id = "prop.furniture.conversationset.v1";
            var record = PropContentRegistry.GetById(id);

            Assert.That(record, Is.Not.Null, $"Prop {id} should be registered in PropContentRegistry");
            Assert.That(record.DisplayName, Is.EqualTo("Lounge Conversation Set"));
            Assert.That(record.ResourcePath, Is.EqualTo("Props/prop_lounge_conversation_set"));
            Assert.That(record.CellWidth, Is.EqualTo(2));
            Assert.That(record.CellHeight, Is.EqualTo(1));
            Assert.That(record.HeightClass, Is.EqualTo("low"));
            Assert.That(record.CollisionMask, Is.EqualTo("solid"));
            Assert.That(record.InteractionPoints, Contains.Item("seat-left"));
            Assert.That(record.InteractionPoints, Contains.Item("seat-right"));
            Assert.That(record.SupportsTheme("residential"), Is.True);
            Assert.That(record.SupportsTheme("lobby"), Is.True);
            Assert.That(record.SupportsTheme("diner"), Is.True);
        }

        [Test]
        public void PropContentRegistry_ResolvesRecreationGameTable()
        {
            const string id = "prop.furniture.gametable.v1";
            var record = PropContentRegistry.GetById(id);

            Assert.That(record, Is.Not.Null, $"Prop {id} should be registered in PropContentRegistry");
            Assert.That(record.DisplayName, Is.EqualTo("Recreation Game Table"));
            Assert.That(record.ResourcePath, Is.EqualTo("Props/prop_recreation_game_table"));
            Assert.That(record.CellWidth, Is.EqualTo(2));
            Assert.That(record.CellHeight, Is.EqualTo(1));
            Assert.That(record.HeightClass, Is.EqualTo("medium"));
            Assert.That(record.CollisionMask, Is.EqualTo("solid"));
            Assert.That(record.InteractionPoints, Contains.Item("seat-left"));
            Assert.That(record.InteractionPoints, Contains.Item("seat-right"));
            Assert.That(record.SupportsTheme("residential"), Is.True);
            Assert.That(record.SupportsTheme("restaurant"), Is.True);
            Assert.That(record.SupportsTheme("diner"), Is.True);
            Assert.That(record.SupportsTheme("office"), Is.True);
        }

        [Test]
        public void PropContentRegistry_ResolvesBedroomVanityDresser()
        {
            const string id = "prop.furniture.vanitydresser.v1";
            var record = PropContentRegistry.GetById(id);

            Assert.That(record, Is.Not.Null, $"Prop {id} should be registered in PropContentRegistry");
            Assert.That(record.DisplayName, Is.EqualTo("Bedroom Vanity Dresser"));
            Assert.That(record.ResourcePath, Is.EqualTo("Props/prop_residential_vanity_dresser"));
            Assert.That(record.CellWidth, Is.EqualTo(2));
            Assert.That(record.CellHeight, Is.EqualTo(2));
            Assert.That(record.HeightClass, Is.EqualTo("tall"));
            Assert.That(record.CollisionMask, Is.EqualTo("solid"));
            Assert.That(record.InteractionPoints, Contains.Item("groom-left"));
            Assert.That(record.InteractionPoints, Contains.Item("groom-right"));
            Assert.That(record.SupportsTheme("residential"), Is.True);
        }

        [Test]
        public void PropCatalog_LoadsValidSprites_ForNewFurnishings()
        {
            var newPropIds = new[]
            {
                "prop.furniture.conversationset.v1",
                "prop.furniture.gametable.v1",
                "prop.furniture.vanitydresser.v1"
            };

            foreach (var propId in newPropIds)
            {
                var sprite = PropCatalog.GetPropSprite(propId);
                Assert.That(sprite, Is.Not.Null, $"PropCatalog should load or create a valid sprite for {propId}");
                Assert.That(sprite.rect.width, Is.GreaterThan(0));
                Assert.That(sprite.rect.height, Is.GreaterThan(0));
                // Sprite pivot should be bottom-center (y = 0.0)
                Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.0f).Within(0.01f),
                    $"Sprite pivot for {propId} must be at y=0 (bottom)");
            }
        }

        [Test]
        public void RoomFurnishingPresenter_PlacesNewProps_AndProvidesDockPositions()
        {
            var go = new GameObject("TestFurnishedRoom");
            try
            {
                var presenter = go.AddComponent<RoomFurnishingPresenter>();

                // West residential room (width >= 3.0) should include conversation set
                presenter.FurnishRoom("residential:family", width: 3.5f, height: 1.42f, isWestSide: true);
                var hasConversation = presenter.PlacedProps.Any(p => p.name.Contains("conversationset"));
                Assert.That(hasConversation, Is.True, "West residential room should place Lounge Conversation Set");

                var hasSeatDock = presenter.TryGetDockPosition(InteractionPointKind.Seat, 0, out var seatPos);
                Assert.That(hasSeatDock, Is.True, "Should resolve Seat dock position in conversation lounge");

                // East residential room (width >= 3.0) should include vanity dresser
                presenter.FurnishRoom("residential:master", width: 3.5f, height: 1.42f, isWestSide: false);
                var hasVanity = presenter.PlacedProps.Any(p => p.name.Contains("vanitydresser"));
                Assert.That(hasVanity, Is.True, "East residential room should place Bedroom Vanity Dresser");

                var hasWorkDock = presenter.TryGetDockPosition(InteractionPointKind.Work, 0, out var workPos);
                Assert.That(hasWorkDock, Is.True, "Should resolve Work dock position at vanity dresser");

                // Diner room (width >= 2.5) should include game table
                presenter.FurnishRoom("commercial:diner", width: 3.0f, height: 1.42f, isWestSide: true);
                var hasGameTable = presenter.PlacedProps.Any(p => p.name.Contains("gametable"));
                Assert.That(hasGameTable, Is.True, "Wide diner room should place Recreation Game Table");

                var hasGameSeatDock = presenter.TryGetDockPosition(InteractionPointKind.Seat, 0, out var gameSeatPos);
                Assert.That(hasGameSeatDock, Is.True, "Should resolve Seat dock position at game table");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
