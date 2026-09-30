using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ResidentBubbleViewTests
    {
        [TestCase(NpcAnimationClip.Idle)]
        [TestCase(NpcAnimationClip.Walk)]
        [TestCase(NpcAnimationClip.QueueWait)]
        [TestCase(NpcAnimationClip.Ride)]
        [TestCase(NpcAnimationClip.Sit)]
        [TestCase(NpcAnimationClip.Sleep)]
        public void ScrubbingBackToSampleReplaysPoseAfterDifferentAnimation(NpcAnimationClip clip)
        {
            var go = new GameObject("Replay resident");
            try
            {
                var rig = go.AddComponent<NpcSkeletalHierarchy>();
                rig.Initialize(3);
                rig.SetFacing(-1f);
                rig.SetAnimationClip(clip);
                rig.ApplyProceduralAnimation(1.25f);
                var positions = new System.Collections.Generic.Dictionary<string, Vector3>();
                var rotations = new System.Collections.Generic.Dictionary<string, Quaternion>();
                var scales = new System.Collections.Generic.Dictionary<string, Vector3>();
                foreach (var pair in rig.Bones)
                {
                    positions.Add(pair.Key, pair.Value.localPosition);
                    rotations.Add(pair.Key, pair.Value.localRotation);
                    scales.Add(pair.Key, pair.Value.localScale);
                }
                rig.SetAnimationClip(clip == NpcAnimationClip.Sleep ? NpcAnimationClip.Walk : NpcAnimationClip.Sleep);
                rig.ApplyProceduralAnimation(8f);
                rig.SetAnimationClip(clip);
                rig.ApplyProceduralAnimation(1.25f);
                foreach (var pair in rig.Bones)
                {
                    Assert.That(Vector3.Distance(pair.Value.localPosition, positions[pair.Key]), Is.LessThan(0.0001f), pair.Key);
                    Assert.That(Quaternion.Angle(pair.Value.localRotation, rotations[pair.Key]), Is.LessThan(0.01f), pair.Key);
                    Assert.That(Vector3.Distance(pair.Value.localScale, scales[pair.Key]), Is.LessThan(0.0001f), pair.Key);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void BubbleKeepsTransparentSpriteMaterialWhenBodyMaterialIsApplied()
        {
            var go = new GameObject("Bubble resident");
            try
            {
                var rig = go.AddComponent<NpcSkeletalHierarchy>();
                rig.Initialize(0);
                rig.SetBubble(new ResidentBubbleCue("That was awkward", ResidentBubbleKind.Thought,
                    ResidentBubbleEmotion.Embarrassed));
                rig.ApplySharedMaterial(NpcSkeletalHierarchy.GetOrCreateDefaultSharedMaterial());
                foreach (var sprite in rig.EmoteAnchor.GetComponentsInChildren<SpriteRenderer>())
                    Assert.That(sprite.sharedMaterial, Is.Not.SameAs(NpcSkeletalHierarchy.GetOrCreateDefaultSharedMaterial()));
                Assert.That(rig.MainRenderer.sharedMaterial,
                    Is.SameAs(NpcSkeletalHierarchy.GetOrCreateDefaultSharedMaterial()));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void BubbleRemainsUprightAndConstantSizeWhileSleepingAndFacingLeft()
        {
            var go = new GameObject("Bubble resident");
            try
            {
                var rig = go.AddComponent<NpcSkeletalHierarchy>();
                rig.Initialize(0);
                rig.SetBubble(new ResidentBubbleCue("Sleeping"));
                rig.RefreshBubblePose();
                var size = rig.BubbleBounds.size;
                rig.SetFacing(-1f);
                rig.SetAnimationClip(NpcAnimationClip.Sleep);
                rig.ApplyProceduralAnimation(1f);
                rig.RefreshBubblePose();
                Assert.That(Quaternion.Angle(rig.EmoteAnchor.rotation, Quaternion.identity), Is.LessThan(0.01f));
                Assert.That(rig.BubbleBounds.size.x, Is.EqualTo(size.x).Within(0.001f));
                Assert.That(rig.BubbleBounds.size.y, Is.EqualTo(size.y).Within(0.001f));
                Assert.That(rig.ActivityCaption.transform.right.x, Is.GreaterThan(0f));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void LongCaptionStaysInsideBubbleAndHidingSurvivesAnimation()
        {
            var go = new GameObject("Bubble resident");
            try
            {
                var rig = go.AddComponent<NpcSkeletalHierarchy>();
                rig.Initialize(0);
                rig.SetBubble(new ResidentBubbleCue("Still thinking about that long conversation with a friend",
                    ResidentBubbleKind.Thought, ResidentBubbleEmotion.Connected));
                var text = rig.ActivityCaption.GetComponent<MeshRenderer>().bounds;
                Assert.That(text.size.x, Is.LessThanOrEqualTo(rig.BubbleBounds.size.x));
                Assert.That(text.size.y, Is.LessThanOrEqualTo(rig.BubbleBounds.size.y));
                rig.SetBubbleVisible(false);
                rig.ApplyProceduralAnimation(1f);
                Assert.That(rig.ActivityCaption.gameObject.activeSelf, Is.False);
                Assert.That(rig.EmoteRenderer.enabled, Is.False);
                rig.Initialize(1);
                Assert.That(rig.ActivityCaption.gameObject.activeSelf, Is.False);
                Assert.That(rig.CurrentEmote, Is.EqualTo(NpcEmoteKind.None));
                rig.SetBubble(new ResidentBubbleCue("At home"));
                Assert.That(rig.ActivityCaption.gameObject.activeSelf, Is.True);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
