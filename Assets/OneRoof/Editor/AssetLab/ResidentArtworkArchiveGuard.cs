using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OneRoof.Editor.AssetLab
{
    /// <summary>Rejects legacy artwork even when copied under a new name or GUID.</summary>
    public sealed class ResidentArtworkArchiveGuard : IPreprocessBuildWithReport
    {
        public const string ArchivePath = "Art/Archive/ResidentRepresentations/2026-09-30";
        private static readonly Regex GuidPattern = new Regex(@"\b(?:guid|m_GUID):\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);
        [Serializable] private sealed class Entry { public string archived; public string sha256; }
        [Serializable] private sealed class Manifest { public Entry[] entries; }
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report) => RequireValid();

        public static void RequireValid()
        {
            var errors = Validate();
            if (errors.Count > 0) throw new BuildFailedException(string.Join("\n", errors));
        }

        public static IReadOnlyList<string> Validate() => ValidateFiles(ArchivePath, Directory.GetFiles("Assets", "*", SearchOption.AllDirectories));

        // Filesystem validation deliberately includes unresolved serialized references and
        // Addressables settings: AssetDatabase dependencies omit dangling GUIDs.
        public static IReadOnlyList<string> ValidateFiles(string archivePath, IEnumerable<string> assetPaths)
        {
            var errors = new List<string>();
            var guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string manifestPath = Path.Combine(archivePath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                errors.Add("Resident artwork archive manifest is missing: " + manifestPath);
                return errors;
            }
            Manifest manifest;
            try { manifest = JsonUtility.FromJson<Manifest>("{\"entries\":" + File.ReadAllText(manifestPath) + "}"); }
            catch (Exception e) { errors.Add("Invalid resident artwork archive manifest: " + e.Message); return errors; }
            if (manifest?.entries == null || manifest.entries.Length == 0)
            { errors.Add("Resident artwork archive manifest is empty."); return errors; }
            foreach (var entry in manifest.entries)
            {
                // Stored paths are project-relative. Resolve fixtures using their archive root too.
                if (entry == null) { errors.Add("Null resident archive manifest entry."); continue; }
                var relative = entry.archived?.Replace('\\', '/');
                var prefix = archivePath.Replace('\\', '/').TrimEnd('/') + "/";
                if (relative == null || !relative.StartsWith(prefix, StringComparison.Ordinal) || relative.Contains("../"))
                { errors.Add("Invalid resident archive entry path: " + relative); continue; }
                if (!File.Exists(entry.archived)) { errors.Add("Missing resident archive file: " + entry.archived); continue; }
                if (!string.Equals(Hash(entry.archived), entry.sha256, StringComparison.OrdinalIgnoreCase))
                    errors.Add("Resident archive checksum mismatch: " + entry.archived);
                if (!IsImage(entry.archived)) continue;
                hashes.Add(entry.sha256 ?? "");
                var meta = entry.archived + ".meta";
                if (!File.Exists(meta)) { errors.Add("Missing resident archive metadata: " + meta); continue; }
                foreach (Match match in GuidPattern.Matches(File.ReadAllText(meta))) guids.Add(match.Groups[1].Value);
            }
            foreach (var path in assetPaths)
            {
                if (!File.Exists(path)) continue;
                var normalized = path.Replace('\\', '/');
                if (IsImage(path))
                {
                    if (normalized.IndexOf("/Resources/Residents/", StringComparison.OrdinalIgnoreCase) >= 0)
                        errors.Add("Unapproved resident Resources representation: " + path);
                    if (hashes.Contains(Hash(path))) errors.Add("Archived resident artwork reintroduced: " + path);
                }
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".meta" && extension != ".asset" && extension != ".prefab" && extension != ".unity" && extension != ".mat" && extension != ".anim" && extension != ".controller" && extension != ".overridecontroller") continue;
                foreach (Match match in GuidPattern.Matches(File.ReadAllText(path)))
                    if (guids.Contains(match.Groups[1].Value))
                    { errors.Add("Archived resident GUID reference or registration: " + path + " (" + match.Groups[1].Value + ")"); break; }
            }
            return errors;
        }

        private static bool IsImage(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            { case ".png": case ".jpg": case ".jpeg": case ".tga": case ".psd": case ".tif": case ".tiff": case ".bmp": return true; default: return false; }
        }
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
