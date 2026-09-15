using System;
using System.IO;
using NUnit.Framework;
using OneRoof.Infrastructure.Persistence;

namespace OneRoof.Infrastructure.Tests.EditMode
{
    public sealed class AtomicFileSaveStoreTests
    {
        private string _testDirectory;

        [SetUp]
        public void SetUp()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "OneRoofTests_" + Guid.NewGuid().ToString("N"));
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
    }
}
