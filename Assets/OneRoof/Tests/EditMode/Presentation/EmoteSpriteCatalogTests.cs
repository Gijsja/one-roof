using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Presentation.Population;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class EmoteSpriteCatalogTests
    {
        [SetUp]
        public void SetUp()
        {
            EmoteSpriteCatalog.ClearCache();
        }

        [Test]
        public void GetSprite_ForNone_ReturnsNull()
        {
            var sprite = EmoteSpriteCatalog.GetSprite(NpcEmoteKind.None);
            Assert.That(sprite, Is.Null);
        }

        [Test]
        public void GetSprite_ForValidKind_ReturnsSprite()
        {
            var sprite = EmoteSpriteCatalog.GetSprite(NpcEmoteKind.Ellipsis, 0);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.width, Is.GreaterThan(0));
            Assert.That(sprite.rect.height, Is.GreaterThan(0));
        }

        [Test]
        public void GetSprite_FrameIndicesClampToValidRange()
        {
            var frame0 = EmoteSpriteCatalog.GetSprite(NpcEmoteKind.Sweat, 0);
            var frameLarge = EmoteSpriteCatalog.GetSprite(NpcEmoteKind.Sweat, 99);
            Assert.That(frame0, Is.Not.Null);
            Assert.That(frameLarge, Is.Not.Null);
        }

        [Test]
        public void GetRecord_ReturnsMatchingEmoteRecord()
        {
            var record = EmoteSpriteCatalog.GetRecord(NpcEmoteKind.Anger);
            Assert.That(record, Is.Not.Null);
            Assert.That(record.ProposedKey, Is.EqualTo("emote_anger_vein"));
        }
    }
}
