using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using UnityEngine;

namespace OneRoof.Infrastructure.Persistence
{
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        [Serializable]
        internal sealed class EnvelopeDto
        {
            public int schemaVersion;
            public long simulationTick;
            public ulong randomSeed;
            public ulong randomState;
            public long randomPosition;
            public string createdAtUtc;
            public string gameVersion;
            public string payloadJson;
        }

        public string Serialize<TState>(SaveEnvelope<TState> envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var payloadJson = JsonUtility.ToJson(envelope.StatePayload);
            var dto = new EnvelopeDto
            {
                schemaVersion = envelope.Metadata.Version.Value,
                simulationTick = envelope.Metadata.SimulationTick.Value,
                randomSeed = envelope.Metadata.RandomState.Seed,
                randomState = envelope.Metadata.RandomState.State,
                randomPosition = envelope.Metadata.RandomState.Position,
                createdAtUtc = envelope.Metadata.CreatedAtUtc,
                gameVersion = envelope.Metadata.GameVersion,
                payloadJson = payloadJson
            };

            return JsonUtility.ToJson(dto, true);
        }

        public LoadResult<SaveEnvelope<TState>> Deserialize<TState>(
            string json,
            SchemaVersion expectedVersion,
            IEnumerable<ISaveMigrator> migrators = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return LoadResult<SaveEnvelope<TState>>.Failure(LoadErrorReason.CorruptData, "Save data is empty or whitespace.");
            }

            EnvelopeDto dto;
            try
            {
                dto = JsonUtility.FromJson<EnvelopeDto>(json);
            }
            catch (Exception ex)
            {
                return LoadResult<SaveEnvelope<TState>>.Failure(LoadErrorReason.CorruptData, $"Malformed JSON envelope: {ex.Message}");
            }

            if (dto == null || dto.schemaVersion <= 0 || string.IsNullOrWhiteSpace(dto.payloadJson))
            {
                return LoadResult<SaveEnvelope<TState>>.Failure(LoadErrorReason.CorruptData, "Invalid envelope schema or missing payload.");
            }

            var currentVersion = new SchemaVersion(dto.schemaVersion);
            var payload = dto.payloadJson;

            if (currentVersion < expectedVersion)
            {
                var migrationChain = BuildMigrationChain(currentVersion, expectedVersion, migrators);
                if (migrationChain == null)
                {
                    return LoadResult<SaveEnvelope<TState>>.Failure(
                        LoadErrorReason.UnsupportedVersion,
                        $"No migration path from schema version {currentVersion.Value} to {expectedVersion.Value}.");
                }

                foreach (var migrator in migrationChain)
                {
                    try
                    {
                        payload = migrator.Migrate(payload);
                        currentVersion = migrator.TargetVersion;
                    }
                    catch (Exception ex)
                    {
                        return LoadResult<SaveEnvelope<TState>>.Failure(
                            LoadErrorReason.MigrationFailed,
                            $"Migration from version {migrator.SourceVersion.Value} to {migrator.TargetVersion.Value} failed: {ex.Message}");
                    }
                }
            }
            else if (currentVersion > expectedVersion)
            {
                return LoadResult<SaveEnvelope<TState>>.Failure(
                    LoadErrorReason.UnsupportedVersion,
                    $"Save version {currentVersion.Value} is newer than supported version {expectedVersion.Value}.");
            }

            TState state;
            try
            {
                state = JsonUtility.FromJson<TState>(payload);
            }
            catch (Exception ex)
            {
                return LoadResult<SaveEnvelope<TState>>.Failure(
                    LoadErrorReason.CorruptData,
                    $"Failed to deserialize state payload: {ex.Message}");
            }

            if (state == null)
            {
                return LoadResult<SaveEnvelope<TState>>.Failure(
                    LoadErrorReason.CorruptData,
                    "Deserialized state payload was null or invalid.");
            }

            SaveEnvelope<TState> envelope;
            try
            {
                var actualState = dto.randomState != 0 ? dto.randomState : (dto.randomSeed != 0 ? dto.randomSeed : 1UL);
                var randomState = new RandomStreamState(dto.randomSeed, actualState, dto.randomPosition);
                var metadata = new SaveEnvelopeMetadata(
                    currentVersion,
                    new Tick(dto.simulationTick),
                    randomState,
                    dto.createdAtUtc,
                    dto.gameVersion);
                envelope = new SaveEnvelope<TState>(metadata, state);
            }
            catch (Exception ex)
            {
                return LoadResult<SaveEnvelope<TState>>.Failure(
                    LoadErrorReason.CorruptData,
                    $"Corrupt save metadata (timestamp, tick, or random state): {ex.Message}");
            }

            return LoadResult<SaveEnvelope<TState>>.Success(envelope);
        }

        private static List<ISaveMigrator> BuildMigrationChain(
            SchemaVersion start,
            SchemaVersion target,
            IEnumerable<ISaveMigrator> migrators)
        {
            if (migrators == null)
            {
                return null;
            }

            var migratorList = new List<ISaveMigrator>(migrators);
            var chain = new List<ISaveMigrator>();
            var current = start;

            while (current < target)
            {
                var nextMigrator = migratorList.Find(m => m.SourceVersion == current);
                if (nextMigrator == null || nextMigrator.TargetVersion <= current)
                {
                    return null;
                }

                chain.Add(nextMigrator);
                current = nextMigrator.TargetVersion;
            }

            return current == target ? chain : null;
        }
    }
}
