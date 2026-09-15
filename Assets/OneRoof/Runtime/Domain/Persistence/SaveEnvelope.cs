using System;

namespace OneRoof.Domain.Persistence
{
    [Serializable]
    public sealed class SaveEnvelope<TState>
    {
        public SaveEnvelope(SaveEnvelopeMetadata metadata, TState statePayload)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            StatePayload = statePayload;
        }

        public SaveEnvelopeMetadata Metadata { get; }

        public TState StatePayload { get; }
    }
}
