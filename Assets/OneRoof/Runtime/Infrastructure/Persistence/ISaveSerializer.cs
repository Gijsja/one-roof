using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;

namespace OneRoof.Infrastructure.Persistence
{
    public interface ISaveSerializer
    {
        string Serialize<TState>(SaveEnvelope<TState> envelope);

        LoadResult<SaveEnvelope<TState>> Deserialize<TState>(
            string json,
            SchemaVersion expectedVersion,
            IEnumerable<ISaveMigrator> migrators = null);
    }
}
