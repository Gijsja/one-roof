namespace OneRoof.Infrastructure.Persistence
{
    public enum LoadErrorReason
    {
        None,
        FileNotFound,
        CorruptData,
        UnsupportedVersion,
        MigrationFailed,
        IoError
    }
}
