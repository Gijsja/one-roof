using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Editor.AssetLab;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneRoof.Editor.Tests.EditMode
{
    public sealed class ResidentAnimationPreviewWindowTests
    {
        [UnityTest]
        public IEnumerator OpeningScrubbingAndClosingPreviewPreservesActiveSceneAndReleasesRig()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Interactive preview requires graphical Unity.");
            var active = SceneManager.GetActiveScene();
            var roots = active.GetRootGameObjects().Select(go => go.GetInstanceID()).OrderBy(id => id).ToArray();
            var dirty = active.isDirty;
            var preview = ScriptableObject.CreateInstance<ResidentAnimationPreviewWindow>();
            Component rig = null;
            try
            {
                preview.Show();
                rig = (Component)Field("_rig").GetValue(preview);
                Assert.That(rig, Is.Not.Null);
                Assert.That(rig.gameObject.scene.handle, Is.Not.EqualTo(active.handle));
                Assert.That(rig.gameObject.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
                Field("_clip").SetValue(preview, NpcAnimationClip.Sleep);
                Field("_sampleTime").SetValue(preview, 2.75f);
                Field("_facingLeft").SetValue(preview, true);
                var sample = rig.GetType().GetProperty("LastAnimationSampleTime");
                for (int i = 0; i < 60 && (float)sample.GetValue(rig) != 2.75f; i++)
                {
                    preview.Repaint();
                    yield return null;
                }
                Assert.That((float)sample.GetValue(rig), Is.EqualTo(2.75f), "Window repaint must apply the scrubbed time.");
                Assert.That(rig.GetType().GetProperty("CurrentAnimation").GetValue(rig), Is.EqualTo(NpcAnimationClip.Sleep));
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(active.handle));
                Assert.That(active.GetRootGameObjects().Select(go => go.GetInstanceID()).OrderBy(id => id), Is.EqualTo(roots));
                Assert.That(active.isDirty, Is.EqualTo(dirty));
            }
            finally { preview.Close(); }
            yield return null;
            Assert.That(rig == null, Is.True, "Preview cleanup must destroy its isolated rig.");
            Assert.That(active.GetRootGameObjects().Select(go => go.GetInstanceID()).OrderBy(id => id), Is.EqualTo(roots));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
        private static FieldInfo Field(string name) => typeof(ResidentAnimationPreviewWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
