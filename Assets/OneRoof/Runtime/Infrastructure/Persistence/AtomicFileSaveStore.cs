using System;
using System.IO;

namespace OneRoof.Infrastructure.Persistence
{
    public interface IFileSaveStore
    {
        SaveResult Save(string filePath, string content);

        LoadResult<string> Load(string filePath);

        bool Delete(string filePath);
    }

    public sealed class AtomicFileSaveStore : IFileSaveStore
    {
        public SaveResult Save(string filePath, string content)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return SaveResult.Failure("Save path cannot be empty.");
            }

            var tempPath = filePath + ".tmp";

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write full content to temporary file first
                File.WriteAllText(tempPath, content ?? string.Empty);

                // Atomically replace target
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.Move(tempPath, filePath);
                return SaveResult.Success();
            }
            catch (Exception ex)
            {
                // Attempt cleanup of temp file if it was left behind
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Ignore secondary cleanup error
                }

                return SaveResult.Failure($"Failed to save to '{filePath}': {ex.Message}");
            }
        }

        public LoadResult<string> Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return LoadResult<string>.Failure(LoadErrorReason.FileNotFound, "Save path cannot be empty.");
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    return LoadResult<string>.Failure(LoadErrorReason.FileNotFound, $"Save file '{filePath}' does not exist.");
                }

                var content = File.ReadAllText(filePath);
                return LoadResult<string>.Success(content);
            }
            catch (Exception ex)
            {
                return LoadResult<string>.Failure(LoadErrorReason.IoError, $"IO error reading '{filePath}': {ex.Message}");
            }
        }

        public bool Delete(string filePath)
        {
            var deleted = false;
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    deleted = true;
                }

                var tempPath = filePath + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                return false;
            }

            return deleted;
        }
    }
}
