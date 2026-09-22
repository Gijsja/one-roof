using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class WardrobeVariantDiversityTests
    {
        [Test]
        public void Hierarchy_TwelveResidentsResolveTwelveDistinctVariants()
        {
            var seen = new HashSet<string>();
            for (var i = 0; i < 12; i++)
            {
                var go = new GameObject($"VariantResident_{i}");
                try
                {
                    var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
                    skeletal.Initialize(i);
                    Assert.That(skeletal.WardrobeVariantKey, Is.Not.Null);
                    seen.Add(skeletal.WardrobeVariantKey);
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
            Assert.That(seen.Count, Is.EqualTo(12));
        }

        [Test]
        public void Hierarchy_VariantTintsAnatomyAndScalesStature()
        {
            var goA = new GameObject("VariantA");
            var goB = new GameObject("VariantB");
            try
            {
                var a = goA.AddComponent<NpcSkeletalHierarchy>();
                var b = goB.AddComponent<NpcSkeletalHierarchy>();
                a.Initialize(0); // firefighter
                b.Initialize(2); // chef

                var upperA = a.WardrobeSlots[NpcLayerKind.UpperClothing];
                var upperB = b.WardrobeSlots[NpcLayerKind.UpperClothing];
                Assert.That(upperA.sprite, Is.Not.Null);
                Assert.That(upperB.sprite, Is.Not.Null);
                Assert.That(upperA.sprite.name, Is.Not.EqualTo(upperB.sprite.name));
                Assert.That(upperA.color, Is.EqualTo(Color.white));

                Assert.That(a.transform.localScale.x, Is.InRange(0.9f, 1.1f));
                Assert.That(a.LimbRenderers["head"].color, Is.Not.EqualTo(Color.white));

                // Re-initialize is idempotent (absolute scale, no compounding).
                var scaleBefore = a.transform.localScale;
                a.Initialize(0);
                Assert.That(a.transform.localScale, Is.EqualTo(scaleBefore));
            }
            finally
            {
                Object.DestroyImmediate(goA);
                Object.DestroyImmediate(goB);
            }
        }
    }
}
