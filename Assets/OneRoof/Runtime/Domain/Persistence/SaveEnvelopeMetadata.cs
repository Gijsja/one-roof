using System;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Persistence
{
    [Serializable]
    public sealed class SaveEnvelopeMetadata
    {
        public SaveEnvelopeMetadata(
            SchemaVersion version,
            Tick simulationTick,
            RandomStreamState randomState,
            string createdAtUtc,
            string gameVersion)
        {
            version.EnsureValid();

            if (string.IsNullOrWhiteSpace(createdAtUtc))
            {
                throw new ArgumentException("Save creation timestamp cannot be empty.", nameof(createdAtUtc));
            }

            Version = version;
            SimulationTick = simulationTick;
            RandomState = randomState;
            CreatedAtUtc = createdAtUtc;
            GameVersion = gameVersion ?? string.Empty;
        }

        public SchemaVersion Version { get; }

        public Tick SimulationTick { get; }

        public RandomStreamState RandomState { get; }

        public string CreatedAtUtc { get; }

        public string GameVersion { get; }
    }
}
