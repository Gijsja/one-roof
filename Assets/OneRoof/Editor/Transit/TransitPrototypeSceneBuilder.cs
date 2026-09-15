using OneRoof.Presentation.Transit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneRoof.Editor.Transit
{
    public static class TransitPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Testbed_Transit.unity";

        [MenuItem("One Roof/Build Transit Prototype Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Transit Prototype");
            root.AddComponent<TransitPrototypeController>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Created {ScenePath}");
        }
    }
}
