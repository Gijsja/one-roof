using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEditor;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ResidentVisualCaptureTests
    {
        [Test]
        public void CaptureArticulatedPosesAndUprightEmotionBubbles()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Resident visual capture requires graphical Unity.");
            var preview = new PreviewRenderUtility();
            Texture2D image = null;
            try
            {
                Debug.Log($"Preview default clipping: near={preview.camera.nearClipPlane}, far={preview.camera.farClipPlane}");
                preview.camera.nearClipPlane = .01f;
                preview.camera.farClipPlane = 100f;
                preview.camera.orthographic = true;
                preview.camera.orthographicSize = 1.15f;
                preview.camera.transform.position = new Vector3(0f, 0.8f, -10f);
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
                var bodies = new List<Bounds>();
                var visibleBounds = new Bounds();
                bool hasBounds = false;
                var poses = new[] { NpcAnimationClip.Idle, NpcAnimationClip.Walk, NpcAnimationClip.QueueWait,
                    NpcAnimationClip.Ride, NpcAnimationClip.Sit, NpcAnimationClip.Sleep };
                var captions = new[] { "At home", "A meaningful conversation", "Lift's late · F3",
                    "Thinking of that flirt", "That was awkward", "Sleeping" };
                var emotions = new[] { ResidentBubbleEmotion.Neutral, ResidentBubbleEmotion.Connected,
                    ResidentBubbleEmotion.Impatient, ResidentBubbleEmotion.Flirty, ResidentBubbleEmotion.Embarrassed,
                    ResidentBubbleEmotion.Neutral };
                for (int row = 0; row < 2; row++)
                for (int column = 0; column < poses.Length; column++)
                {
                    var root = new GameObject(poses[column] + (row == 0 ? " right" : " left")) { hideFlags = HideFlags.HideAndDontSave };
                    var rig = root.AddComponent<NpcSkeletalHierarchy>();
                    rig.Initialize(column);
                    preview.AddSingleGO(root);
                    root.transform.position = new Vector3((column - 2.5f) * 1.35f, row == 0 ? 1.15f : -0.1f, 0f);
                    rig.SetFacing(row == 0 ? 1f : -1f);
                    rig.SetAnimationClip(poses[column], "Visual validation fixture");
                    rig.SetBubble(new ResidentBubbleCue(captions[column], column == 0 || column == 5 ? ResidentBubbleKind.Action : ResidentBubbleKind.Thought, emotions[column]));
                    rig.ApplyProceduralAnimation(1.25f);
                    rig.RefreshBubblePose();
                    Assert.That(Quaternion.Angle(rig.EmoteAnchor.rotation, Quaternion.identity), Is.LessThan(0.01f));
                    var body = new Bounds();
                    bool hasBody = false;
                    foreach (var limb in rig.LimbRenderers.Values)
                    {
                        if (!limb.enabled || !limb.gameObject.activeInHierarchy) continue;
                        if (!hasBody) { body = limb.bounds; hasBody = true; }
                        else body.Encapsulate(limb.bounds);
                    }
                    Assert.That(hasBody, Is.True, "Fixture must contain visible articulated anatomy.");
                    bodies.Add(body);
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        if (!hasBounds) { visibleBounds = renderer.bounds; hasBounds = true; }
                        else visibleBounds.Encapsulate(renderer.bounds);
                    }
                }
                preview.camera.transform.position = new Vector3(visibleBounds.center.x, visibleBounds.center.y, -10f);
                preview.camera.aspect = 1800f / 700f;
                preview.camera.orthographicSize = Mathf.Max(visibleBounds.size.y * .5f, visibleBounds.size.x * .5f / preview.camera.aspect) * 1.12f;
                preview.BeginStaticPreview(new Rect(0f, 0f, 1800f, 700f));
                preview.Render(true);
                image = preview.EndStaticPreview();
                Assert.That(image, Is.Not.Null);
                var path = Path.Combine(Path.GetTempPath(), "one-roof-resident-review.png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                foreach (var body in bodies) AssertBodyVisible(image, preview.camera, body);
                Debug.Log("Resident visual review captured: " + path);
            }
            finally
            {
                if (image != null) Object.DestroyImmediate(image);
                preview.Cleanup();
            }
        }

        [Test]
        public void PoolReusesOneSpriteTintMaterialAcrossResidentsAndRecycle()
        {
            var root = new GameObject("Resident material pool test");
            try
            {
                var pool = root.AddComponent<NpcViewPool>();
                var first = pool.Acquire(1).SkeletalHierarchy;
                var second = pool.Acquire(2).SkeletalHierarchy;
                var material = first.LimbRenderers["torso"].sharedMaterial;
                Assert.That(material.shader, Is.EqualTo(NpcSkeletalHierarchy.GetOrCreateDefaultSharedMaterial().shader));
                Assert.That(material.shader.name, Does.Contain("Sprite"));
                Assert.That(second.LimbRenderers["torso"].sharedMaterial, Is.SameAs(material));
                pool.Release(1);
                var recycledView = pool.Acquire(3);
                var recycled = recycledView.SkeletalHierarchy;
                recycled.Initialize(2);
                Assert.That(recycled.LimbRenderers["torso"].sharedMaterial, Is.SameAs(material));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AssertBodyVisible(Texture2D image, Camera camera, Bounds body)
        {
            var min = camera.WorldToViewportPoint(body.min);
            var max = camera.WorldToViewportPoint(body.max);
            Assert.That(min.x, Is.InRange(0.01f, .99f));
            Assert.That(min.y, Is.InRange(0.01f, .99f));
            Assert.That(max.x, Is.InRange(0.01f, .99f));
            Assert.That(max.y, Is.InRange(0.01f, .99f));
            var background = image.GetPixel(0, 0);
            int visible = 0;
            int palettePixels = 0;
            for (int y = Mathf.FloorToInt(min.y * image.height); y < Mathf.CeilToInt(max.y * image.height); y++)
            for (int x = Mathf.FloorToInt(min.x * image.width); x < Mathf.CeilToInt(max.x * image.width); x++)
            {
                var pixel = image.GetPixel(x, y);
                if (Mathf.Abs(pixel.r - background.r) + Mathf.Abs(pixel.g - background.g) + Mathf.Abs(pixel.b - background.b) <= .15f) continue;
                visible++;
                if (Mathf.Max(pixel.r, pixel.g, pixel.b) - Mathf.Min(pixel.r, pixel.g, pixel.b) > .12f) palettePixels++;
            }
            Assert.That(visible, Is.GreaterThan(50), "Articulated anatomy must render inside every body region; bubbles alone cannot pass.");
            Assert.That(palettePixels, Is.GreaterThan(20), "Skin/clothing palette must render inside every body region; white silhouettes cannot pass.");
        }
    }
}
