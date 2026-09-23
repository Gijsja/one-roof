using System;
using System.IO;
using NUnit.Framework;
using OneRoof.Infrastructure.Persistence;
using UnityEngine;

namespace OneRoof.Infrastructure.Tests.EditMode
{
    public sealed class AtomicFileSaveStoreTests
    {
        private string _testDirectory;

        [SetUp]
        public void SetUp()
        {
            _testDirectory = Path.Combine(UnityEngine.Application.persistentDataPath, "OneRoofTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Test]
        public void SaveAndLoadRoundTripsFileContentAtomically()
        {
            var store = new AtomicFileSaveStore();
            var filePath = Path.Combine(_testDirectory, "slot1.save");
            var content = "{\"test\":\"atomic_content\"}";

            var saveResult = store.Save(filePath, content);
            Assert.That(saveResult.IsSuccess, Is.True);
            Assert.That(File.Exists(filePath), Is.True);
            Assert.That(File.Exists(filePath + ".tmp"), Is.False);
            Assert.That(Directory.GetFiles(_testDirectory, "slot1.save.*.tmp"), Is.Empty);

            var loadResult = store.Load(filePath);
            Assert.That(loadResult.IsSuccess, Is.True);
            Assert.That(loadResult.Value, Is.EqualTo(content));
        }

        [Test]
        public void LoadNonExistentFileReturnsFileNotFound()
        {
            var store = new AtomicFileSaveStore();
            var filePath = Path.Combine(_testDirectory, "nonexistent.save");

            var loadResult = store.Load(filePath);
            Assert.That(loadResult.IsSuccess, Is.False);
            Assert.That(loadResult.ErrorReason, Is.EqualTo(LoadErrorReason.FileNotFound));
        }

        [Test]
        public void DeleteRemovesFileAndAnyStaleTmpFile()
        {
            var store = new AtomicFileSaveStore();
            var filePath = Path.Combine(_testDirectory, "delete_me.save");
            store.Save(filePath, "data");

            File.WriteAllText(filePath + ".tmp", "stale_tmp");

            Assert.That(store.Delete(filePath), Is.True);
            Assert.That(File.Exists(filePath), Is.False);
            Assert.That(File.Exists(filePath + ".tmp"), Is.False);
        }

        [Test]
        public void SaveOverExistingFileReplacesContentWithoutGap()
        {
            var store = new AtomicFileSaveStore();
            var filePath = Path.Combine(_testDirectory, "overwrite.save");
            var originalContent = "{\"version\":1}";
            var updatedContent = "{\"version\":2}";

            store.Save(filePath, originalContent);
            Assert.That(File.ReadAllText(filePath), Is.EqualTo(originalContent));

            var result = store.Save(filePath, updatedContent);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(File.ReadAllText(filePath), Is.EqualTo(updatedContent));
            Assert.That(File.Exists(filePath + ".tmp"), Is.False, "Temp file must be cleaned up after atomic replace");
        }

        [Test]
        public void DeleteWhenFileAbsentReturnsFalse()
        {
            var store = new AtomicFileSaveStore();
            var filePath = Path.Combine(_testDirectory, "never_existed.save");

            Assert.That(store.Delete(filePath), Is.False);
        }

        [Test]
        public void SaveLoadAndDeleteRejectPathsOutsidePersistentDataPath()
        {
            var store = new AtomicFileSaveStore();
            var outsidePath = Path.Combine(Path.GetTempPath(), "OneRoof_outside_" + Guid.NewGuid().ToString("N") + ".save");

            Assert.That(store.Save(outsidePath, "should not be written").IsSuccess, Is.False);
            Assert.That(File.Exists(outsidePath), Is.False);
            Assert.That(store.Load(outsidePath).IsSuccess, Is.False);
            Assert.That(store.Delete(outsidePath), Is.False);
        }

        [Test]
        public void SaveLoadAndDeleteRejectDirectoryTraversal()
        {
            var store = new AtomicFileSaveStore();
            var fileName = "OneRoof_traversal_" + Guid.NewGuid().ToString("N") + ".save";
            var traversalPath = Path.Combine("..", fileName);

            Assert.That(store.Save(traversalPath, "should not be written").IsSuccess, Is.False);
            Assert.That(store.Load(traversalPath).IsSuccess, Is.False);
            Assert.That(store.Delete(traversalPath), Is.False);
            Assert.That(File.Exists(Path.GetFullPath(Path.Combine(UnityEngine.Application.persistentDataPath, traversalPath))), Is.False);
        }
    }
}
