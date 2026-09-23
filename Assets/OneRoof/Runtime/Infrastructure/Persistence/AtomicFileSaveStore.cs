using System;
using System.IO;
using System.Text;
using UnityEngine;

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

            if (!TryResolveAuthorizedPath(filePath, out var resolvedPath))
            {
                return SaveResult.Failure("Save path must be under Application.persistentDataPath.");
            }

            var tempPath = (string)null;

            try
            {
                var directory = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // CreateNew requests exclusive creation, so a pre-existing file or symlink at
                // this unpredictable path cannot be opened and overwritten.
                tempPath = Path.Combine(directory, Path.GetFileName(resolvedPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(content ?? string.Empty);
                }

                // Atomically replace target — File.Replace is an atomic rename on same-filesystem
                // paths (maps to rename(2) on Linux) and has no window where both files are absent.
                // If no prior save exists we fall back to a plain Move (nothing to lose).
                if (File.Exists(resolvedPath))
                {
                    File.Replace(tempPath, resolvedPath, null);
                }
                else
                {
                    File.Move(tempPath, resolvedPath);
                }
                return SaveResult.Success();
            }
            catch (Exception ex)
            {
                // Attempt cleanup of temp file if it was left behind
                try
                {
                    if (tempPath != null && File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Ignore secondary cleanup error
                }

                return SaveResult.Failure($"Failed to save to '{resolvedPath}': {ex.Message}");
            }
        }

        public LoadResult<string> Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return LoadResult<string>.Failure(LoadErrorReason.FileNotFound, "Save path cannot be empty.");
            }

            if (!TryResolveAuthorizedPath(filePath, out var resolvedPath))
            {
                return LoadResult<string>.Failure(LoadErrorReason.IoError, "Save path must be under Application.persistentDataPath.");
            }

            try
            {
                if (!File.Exists(resolvedPath))
                {
                    return LoadResult<string>.Failure(LoadErrorReason.FileNotFound, $"Save file '{resolvedPath}' does not exist.");
                }

                var content = File.ReadAllText(resolvedPath);
                return LoadResult<string>.Success(content);
            }
            catch (Exception ex)
            {
                return LoadResult<string>.Failure(LoadErrorReason.IoError, $"IO error reading '{resolvedPath}': {ex.Message}");
            }
        }

        public bool Delete(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !TryResolveAuthorizedPath(filePath, out var resolvedPath))
            {
                return false;
            }

            var deleted = false;
            try
            {
                if (File.Exists(resolvedPath))
                {
                    File.Delete(resolvedPath);
                    deleted = true;
                }

                // Clean up deterministic temporary files left by older versions.
                var tempPath = resolvedPath + ".tmp";
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

        private static bool TryResolveAuthorizedPath(string filePath, out string resolvedPath)
        {
            resolvedPath = null;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            try
            {
                var rootPath = Path.GetFullPath(UnityEngine.Application.persistentDataPath);
                var candidatePath = Path.GetFullPath(Path.IsPathRooted(filePath)
                    ? filePath
                    : Path.Combine(rootPath, filePath));
                var rootWithSeparator = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!candidatePath.StartsWith(rootWithSeparator, comparison))
                {
                    return false;
                }

                resolvedPath = candidatePath;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
