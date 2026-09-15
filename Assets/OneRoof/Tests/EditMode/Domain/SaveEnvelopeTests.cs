using System;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class SaveEnvelopeTests
    {
        [Test]
        public void SaveEnvelopeMetadataRequiresPositiveVersionAndValidTimestamp()
        {
            var randomState = new RandomStreamState(42, 42, 0);

            Assert.Throws<ArgumentException>(() =>
                new SaveEnvelopeMetadata(new SchemaVersion(1), new Tick(10), randomState, "", "1.0.0"));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SaveEnvelopeMetadata(new SchemaVersion(0), new Tick(10), randomState, "2026-09-15T00:00:00Z", "1.0.0"));
        }

        [Test]
        public void SaveEnvelopeHoldsMetadataAndStatePayload()
        {
            var randomState = new RandomStreamState(12345, 67890, 5);
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                new Tick(50),
                randomState,
                "2026-09-15T12:00:00Z",
                "0.1.0");

            var payload = new DummyState { Value = "Test Payload" };
            var envelope = new SaveEnvelope<DummyState>(metadata, payload);

            Assert.That(envelope.Metadata.Version.Value, Is.EqualTo(1));
            Assert.That(envelope.Metadata.SimulationTick.Value, Is.EqualTo(50));
            Assert.That(envelope.Metadata.RandomState.Seed, Is.EqualTo(12345));
            Assert.That(envelope.StatePayload.Value, Is.EqualTo("Test Payload"));
        }

        [Serializable]
        private sealed class DummyState
        {
            public string Value;
        }
    }
}
