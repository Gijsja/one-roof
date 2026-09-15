using System.Reflection;
using NUnit.Framework;
using OneRoof.Presentation.Transit;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TransitPrototypeSceneCompositionTests
    {
        [Test]
        public void TestbedTransitSceneContainsThePrototypeController()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Testbed_Transit.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            var root = GameObject.Find("Transit Prototype");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<TransitPrototypeController>(), Is.Not.Null);
        }

        [Test]
        public void PrototypeControllerBuildsFiveFloorsAndFiftyResidentViews()
        {
            var root = new GameObject("Presentation Test Root");
            var controller = root.AddComponent<TransitPrototypeController>();
            typeof(TransitPrototypeController)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);

            Assert.That(root.transform.childCount, Is.EqualTo(63));

            Object.DestroyImmediate(root);
            var cameraObject = GameObject.Find("Prototype Camera");
            if (cameraObject != null)
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
