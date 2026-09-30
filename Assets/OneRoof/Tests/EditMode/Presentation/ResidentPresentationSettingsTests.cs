using System;
using NUnit.Framework;
using OneRoof.Presentation.Tower;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ResidentPresentationSettingsTests
    {
        [TestCase(0, 60)]
        [TestCase(-1, 60)]
        [TestCase(60, -1)]
        public void InvalidBudgets_AreRejected(int capacity, int prewarm)
        {
            var settings = new ResidentPresentationSettings { SkeletalCapacity = capacity, PrewarmCount = prewarm };
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
        }

        [TestCase(8f, 8f, 16f, 18f)]
        [TestCase(9f, 8f, 16f, 18f)]
        [TestCase(6f, 17f, 16f, 18f)]
        [TestCase(6f, 8f, 18f, 18f)]
        [TestCase(float.NaN, 8f, 16f, 18f)]
        [TestCase(6f, 8f, 16f, float.PositiveInfinity)]
        public void InvalidFadeRanges_AreRejected(float start, float end, float macroStart, float macroEnd)
        {
            var settings = new ResidentPresentationSettings { RigFadeStart = start, RigFadeEnd = end,
                MacroFadeStart = macroStart, MacroFadeEnd = macroEnd };
            Assert.Throws<ArgumentException>(() => settings.Validate());
        }

        [TestCase(-0.1f, 0.8f)]
        [TestCase(float.NaN, 0.8f)]
        [TestCase(0.05f, 0f)]
        [TestCase(0.05f, 1.1f)]
        [TestCase(0.05f, float.PositiveInfinity)]
        public void InvalidVisibilitySettings_AreRejected(float margin, float preference)
        {
            var settings = new ResidentPresentationSettings { ViewportMargin = margin, RetainedRigPreference = preference };
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
        }

        [TestCase(80, 80, 50)]
        [TestCase(3, 60, 3)]
        [TestCase(80, 0, 0)]
        public void Controller_PrewarmsConfiguredInactiveViewsBeforeRendering(int capacity, int prewarm, int expected)
        {
            var root = new GameObject("Configured Tower");
            root.SetActive(false);
            try
            {
                var controller = root.AddComponent<TowerPlayableController>();
                controller.ResidentPresentation.SkeletalCapacity = capacity;
                controller.ResidentPresentation.PrewarmCount = prewarm;
                controller.Initialize();
                var pool = root.GetComponent<NpcViewPool>();
                Assert.That(pool.MaxCapacity, Is.EqualTo(capacity));
                Assert.That(pool.TotalInstantiatedCount, Is.EqualTo(expected));
                Assert.That(pool.ActiveCount, Is.Zero);
                foreach (var view in pool.AllViews) Assert.That(view.gameObject.activeSelf, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void Settings_InspectorSerializationRoundTrips()
        {
            var authored = new ResidentPresentationSettings { SkeletalCapacity = 80, PrewarmCount = 12,
                RigFadeStart = 3f, RigFadeEnd = 4f, MacroFadeStart = 9f, MacroFadeEnd = 10f,
                ViewportMargin = 0.2f, RetainedRigPreference = 0.5f };
            var restored = JsonUtility.FromJson<ResidentPresentationSettings>(JsonUtility.ToJson(authored));
            restored.Validate();
            Assert.That(restored.SkeletalCapacity, Is.EqualTo(80));
            Assert.That(restored.PrewarmCount, Is.EqualTo(12));
            Assert.That(restored.RigFadeEnd, Is.EqualTo(4f));
            Assert.That(restored.MacroFadeEnd, Is.EqualTo(10f));
            Assert.That(restored.ViewportMargin, Is.EqualTo(0.2f));
            Assert.That(restored.RetainedRigPreference, Is.EqualTo(0.5f));
        }
    }
}
