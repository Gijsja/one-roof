using System;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Infrastructure.Persistence;

namespace OneRoof.Infrastructure.Tests.EditMode
{
    public sealed class JsonSaveSerializerTests
    {
        [Serializable]
        public sealed class SampleSaveState
        {
            public string buildingName;
            public int totalFloors;
        }

        [Test]
        public void RoundTripSerializationPreservesMetadataAndState()
        {
            var serializer = new JsonSaveSerializer();
            var randomState = new RandomStreamState(999, 888, 42);
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                new Tick(120),
                randomState,
                "2026-09-15T15:30:00Z",
                "1.0.0");

            var state = new SampleSaveState
            {
                buildingName = "Skyline One",
                totalFloors = 5
            };

            var originalEnvelope = new SaveEnvelope<SampleSaveState>(metadata, state);
            var json = serializer.Serialize(originalEnvelope);

            Assert.That(json, Is.Not.Null.And.Not.Empty);

            var loadResult = serializer.Deserialize<SampleSaveState>(json, new SchemaVersion(1));

            Assert.That(loadResult.IsSuccess, Is.True);
            var deserialized = loadResult.Value;
            Assert.That(deserialized.Metadata.Version.Value, Is.EqualTo(1));
            Assert.That(deserialized.Metadata.SimulationTick.Value, Is.EqualTo(120));
            Assert.That(deserialized.Metadata.RandomState.Seed, Is.EqualTo(999));
            Assert.That(deserialized.Metadata.RandomState.Position, Is.EqualTo(42));
            Assert.That(deserialized.Metadata.CreatedAtUtc, Is.EqualTo("2026-09-15T15:30:00Z"));
            Assert.That(deserialized.StatePayload.buildingName, Is.EqualTo("Skyline One"));
            Assert.That(deserialized.StatePayload.totalFloors, Is.EqualTo(5));
        }

        [Test]
        public void CorruptedJsonFailsWithCorruptDataReason()
        {
            var serializer = new JsonSaveSerializer();

            var result = serializer.Deserialize<SampleSaveState>("not valid json {{{", new SchemaVersion(1));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorReason, Is.EqualTo(LoadErrorReason.CorruptData));

            var emptyResult = serializer.Deserialize<SampleSaveState>("", new SchemaVersion(1));
            Assert.That(emptyResult.IsSuccess, Is.False);
            Assert.That(emptyResult.ErrorReason, Is.EqualTo(LoadErrorReason.CorruptData));
        }

        [Test]
        public void NewerVersionFailsWithUnsupportedVersionReason()
        {
            var serializer = new JsonSaveSerializer();
            var randomState = new RandomStreamState(1, 1, 0);
            var metadata = new SaveEnvelopeMetadata(new SchemaVersion(5), new Tick(1), randomState, "now", "2.0.0");
            var envelope = new SaveEnvelope<SampleSaveState>(metadata, new SampleSaveState());

            var json = serializer.Serialize(envelope);
            var result = serializer.Deserialize<SampleSaveState>(json, new SchemaVersion(1));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorReason, Is.EqualTo(LoadErrorReason.UnsupportedVersion));
        }

        [Test]
        public void MigratorSuccessfullyUpgradesOlderSchemaVersion()
        {
            var serializer = new JsonSaveSerializer();
            var randomState = new RandomStreamState(1, 1, 0);
            var metadata = new SaveEnvelopeMetadata(new SchemaVersion(1), new Tick(1), randomState, "now", "1.0.0");
            var envelope = new SaveEnvelope<SampleSaveState>(metadata, new SampleSaveState { buildingName = "Old", totalFloors = 3 });

            var json = serializer.Serialize(envelope);

            var migrator = new DummyMigrator(new SchemaVersion(1), new SchemaVersion(2));
            var result = serializer.Deserialize<SampleSaveState>(json, new SchemaVersion(2), new[] { migrator });

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Metadata.Version.Value, Is.EqualTo(2));
            Assert.That(result.Value.StatePayload.buildingName, Is.EqualTo("Old_Migrated"));
        }

        private sealed class DummyMigrator : ISaveMigrator
        {
            public DummyMigrator(SchemaVersion sourceVersion, SchemaVersion targetVersion)
            {
                SourceVersion = sourceVersion;
                TargetVersion = targetVersion;
            }

            public SchemaVersion SourceVersion { get; }

            public SchemaVersion TargetVersion { get; }

            public string Migrate(string sourcePayloadJson)
            {
                return sourcePayloadJson.Replace("\"Old\"", "\"Old_Migrated\"");
            }
        }
    }
}
