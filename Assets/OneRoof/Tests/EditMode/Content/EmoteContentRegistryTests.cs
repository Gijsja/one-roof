using NUnit.Framework;
using OneRoof.Content;

namespace OneRoof.Content.Tests.EditMode
{
    public sealed class EmoteContentRegistryTests
    {
        [Test]
        public void Registry_Contains24CanonicalEmotes()
        {
            Assert.That(EmoteContentRegistry.Count, Is.EqualTo(24));
            Assert.That(EmoteContentRegistry.AllRecords.Count, Is.EqualTo(24));
        }

        [Test]
        public void Registry_AllRecordsConformToSchema()
        {
            foreach (var record in EmoteContentRegistry.AllRecords)
            {
                Assert.That(record.ContentId, Does.StartWith("emote.bubble."));
                Assert.That(record.ContentId, Does.EndWith(".v1"));
                Assert.That(record.ProposedKey, Does.StartWith("emote_"));
                Assert.That(record.ResourcePath, Does.StartWith("Emotes/"));
                Assert.That(record.Columns.Count, Is.EqualTo(3));
                Assert.That(record.FrameCount, Is.EqualTo(3));
                Assert.That(record.FrameDuration, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void Registry_LookupByKind_ReturnsExpectedRecord()
        {
            var ellipsis = EmoteContentRegistry.GetByKind(NpcEmoteKind.Ellipsis);
            Assert.That(ellipsis, Is.Not.Null);
            Assert.That(ellipsis.ProposedKey, Is.EqualTo("emote_ellipsis_wait"));
            Assert.That(ellipsis.Category, Is.EqualTo("Transit"));

            var sweat = EmoteContentRegistry.GetByKind(NpcEmoteKind.Sweat);
            Assert.That(sweat, Is.Not.Null);
            Assert.That(sweat.ProposedKey, Is.EqualTo("emote_sweat_drops"));

            var anger = EmoteContentRegistry.GetByKind(NpcEmoteKind.Anger);
            Assert.That(anger, Is.Not.Null);
            Assert.That(anger.ProposedKey, Is.EqualTo("emote_anger_vein"));

            Assert.That(EmoteContentRegistry.GetByKind(NpcEmoteKind.None), Is.Null);
        }

        [Test]
        public void Registry_LookupById_ReturnsExpectedRecord()
        {
            var record = EmoteContentRegistry.GetById("emote.bubble.ellipsis.v1");
            Assert.That(record, Is.Not.Null);
            Assert.That(record.EmoteKind, Is.EqualTo(NpcEmoteKind.Ellipsis));

            Assert.That(EmoteContentRegistry.GetById("nonexistent.id"), Is.Null);
        }
    }
}
