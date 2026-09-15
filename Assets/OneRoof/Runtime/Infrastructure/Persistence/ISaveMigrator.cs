using OneRoof.Domain.Identity;

namespace OneRoof.Infrastructure.Persistence
{
    public interface ISaveMigrator
    {
        SchemaVersion SourceVersion { get; }

        SchemaVersion TargetVersion { get; }

        string Migrate(string sourcePayloadJson);
    }
}
