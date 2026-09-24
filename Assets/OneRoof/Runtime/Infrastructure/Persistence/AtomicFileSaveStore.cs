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
        private static readonly object FileGate = new object();

        public SaveResult Save(string filePath, string content)
        {
            lock (FileGate) return SaveCore(filePath, content);
        }

        private static SaveResult SaveCore(string filePath, string content)
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
                var backupPath = tempPath + ".bak";
                const int maxAttempts = 5;
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        if (File.Exists(resolvedPath))
                        {
                            File.Replace(tempPath, resolvedPath, backupPath, ignoreMetadataErrors: true);
                            if (File.Exists(backupPath))
                            {
                                try { File.Delete(backupPath); } catch { }
                            }
                        }
                        else
                        {
                            File.Move(tempPath, resolvedPath);
                        }
                        return SaveResult.Success();
                    }
                    catch (FileNotFoundException) when (!File.Exists(resolvedPath))
                    {
                        File.Move(tempPath, resolvedPath);
                        return SaveResult.Success();
                    }
                    catch (IOException ex) when (attempt < maxAttempts && IsSharingViolation(ex))
                    {
                        System.Threading.Thread.Sleep(25 * (1 << (attempt - 1)));
                    }
                }

                return SaveResult.Failure($"Failed to save to '{Path.GetFileName(resolvedPath)}' after retries.");
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

                return SaveResult.Failure($"Failed to save '{Path.GetFileName(resolvedPath)}' ({ex.GetType().Name}).");
            }
        }

        public LoadResult<string> Load(string filePath)
        {
            lock (FileGate) return LoadCore(filePath);
        }

        private static LoadResult<string> LoadCore(string filePath)
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
                    return LoadResult<string>.Failure(LoadErrorReason.FileNotFound, $"Save file '{Path.GetFileName(resolvedPath)}' does not exist.");
                }

                var content = File.ReadAllText(resolvedPath);
                return LoadResult<string>.Success(content);
            }
            catch (Exception ex)
            {
                return LoadResult<string>.Failure(LoadErrorReason.IoError, $"Failed to read '{Path.GetFileName(resolvedPath)}' ({ex.GetType().Name}).");
            }
        }

        public bool Delete(string filePath)
        {
            lock (FileGate) return DeleteCore(filePath);
        }

        private static bool DeleteCore(string filePath)
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

                var backupPath = resolvedPath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                var directory = Path.GetDirectoryName(resolvedPath);
                var filename = Path.GetFileName(resolvedPath);
                if (Directory.Exists(directory))
                {
                    var pattern = filename + ".*.tmp";
                    foreach (var tmpFile in Directory.EnumerateFiles(directory, pattern))
                    {
                        try { File.Delete(tmpFile); } catch { }
                    }
                    foreach (var backupFile in Directory.EnumerateFiles(directory, pattern + ".bak"))
                    {
                        try { File.Delete(backupFile); } catch { }
                    }
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

        private static bool IsSharingViolation(IOException exception)
        {
            const int sharingViolation = unchecked((int)0x80070020);
            const int lockViolation = unchecked((int)0x80070021);
            return exception.HResult == sharingViolation || exception.HResult == lockViolation;
        }
    }
}
