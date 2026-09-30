using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using OneRoof.Editor.AssetLab;
using UnityEngine;

namespace OneRoof.Editor.Tests.EditMode
{
    public sealed class ResidentArtworkArchiveGuardTests
    {
        private string root;
        private string archive;
        private const string Guid = "0123456789abcdef0123456789abcdef";
        [SetUp] public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "one-roof-archive-" + System.Guid.NewGuid().ToString("N"));
            archive = Path.Combine(root, "Archive").Replace('\\', '/');
            Directory.CreateDirectory(archive);
            File.WriteAllBytes(Path.Combine(archive, "body.png"), new byte[] { 1, 2, 3, 4 });
            File.WriteAllText(Path.Combine(archive, "body.png.meta"), "guid: " + Guid);
            WriteManifest();
        }
        [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }
        private void WriteManifest()
        {
            var files = Directory.GetFiles(archive).Where(p => !p.EndsWith("manifest.json"));
            var entries = files.Select(p => "{\"archived\":\"" + p.Replace('\\', '/') + "\",\"sha256\":\"" + Hash(p) + "\"}");
            File.WriteAllText(Path.Combine(archive, "manifest.json"), "[" + string.Join(",", entries) + "]");
        }
        private string Asset(string relative, string text)
        {
            string path = Path.Combine(root, "Assets", relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
            return path;
        }
        [Test] public void CurrentArchiveHasNoActiveReferencesOrReintroducedArtwork()
        { Assert.That(ResidentArtworkArchiveGuard.Validate(), Is.Empty); }
        [Test] public void ResourcesResidentImageIsRejectedEvenWithNewBytesAndGuid()
        {
            var path = Asset("Resources/Residents/new.png", "new artwork");
            Assert.That(ResidentArtworkArchiveGuard.ValidateFiles(archive, new[] { path }), Has.Some.Contains("Resources representation"));
        }
        [Test] public void RenamedArchivedImageWithNewGuidIsRejectedOutsideResources()
        {
            var path = Asset("Modular/renamed.png", "");
            File.Copy(Path.Combine(archive, "body.png"), path, true);
            Assert.That(ResidentArtworkArchiveGuard.ValidateFiles(archive, new[] { path }), Has.Some.Contains("reintroduced"));
        }
        [TestCase("Tower.unity", "m_Sprite: {fileID: 21300000, guid: ")]
        [TestCase("Resident.prefab", "m_Sprite: {fileID: 21300000, guid: ")]
        [TestCase("AddressableAssetsData/Group.asset", "m_GUID: ")]
        public void UnresolvedSerializedAndAddressableRegistrationsAreRejected(string path, string prefix)
        {
            var asset = Asset(path, prefix + Guid + "}");
            Assert.That(ResidentArtworkArchiveGuard.ValidateFiles(archive, new[] { asset }), Has.Some.Contains("GUID reference or registration"));
        }
        [Test] public void ChangedArchiveAndMissingMetadataFailClosed()
        {
            File.WriteAllBytes(Path.Combine(archive, "body.png"), new byte[] { 9 });
            File.Delete(Path.Combine(archive, "body.png.meta"));
            var errors = ResidentArtworkArchiveGuard.ValidateFiles(archive, Array.Empty<string>());
            Assert.That(errors, Has.Some.Contains("checksum mismatch"));
            Assert.That(errors, Has.Some.Contains("metadata"));
        }
        [Test] public void MissingManifestFailsClosed()
        {
            File.Delete(Path.Combine(archive, "manifest.json"));
            Assert.That(ResidentArtworkArchiveGuard.ValidateFiles(archive, Array.Empty<string>()), Has.Some.Contains("manifest is missing"));
        }
        [Test] public void NewModularContentAndEmotesRemainEligibleForExplicitRegistration()
        {
            var image = Asset("Modular/attachment.png", "new modular artwork");
            var emote = Asset("Resources/Emotes/happy.png", "emote artwork");
            var settings = Asset("AddressableAssetsData/Group.asset", "m_GUID: fedcba9876543210fedcba9876543210");
            Assert.That(ResidentArtworkArchiveGuard.ValidateFiles(archive, new[] { image, emote, settings }), Is.Empty);
        }
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
