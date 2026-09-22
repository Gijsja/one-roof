using System.IO;
using OneRoof.Presentation.Tower;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneRoof.Editor.Tower
{
    /// <summary>
    /// Builds and validates the main interactive Tower scene (Assets/Scenes/Tower.unity).
    /// </summary>
    [InitializeOnLoad]
    public static class TowerSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Tower.unity";
        public const string GroundStartScenePath = "Assets/Scenes/Tower_GroundStart.unity";

        static TowerSceneBuilder()
        {
            EditorApplication.delayCall += EnsureSceneExists;
        }

        private static void EnsureSceneExists()
        {
            if (!File.Exists(ScenePath))
            {
                CreateScene();
            }
        }

        [MenuItem("One Roof/Build Tower Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Tower World");
            root.AddComponent<TowerPlayableController>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Successfully created and saved {ScenePath}");
        }

        [MenuItem("One Roof/Build Ground Start Scene")]
        public static void CreateGroundStartScene()
        {
            // Rebuild this scene from an empty hierarchy. The old asset had
            // ExecuteAlways-generated objects from the five-floor fixture saved
            // into it, so reopening it could display rooms and residents that do
            // not exist in the ground-start simulation.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Tower World");
            root.SetActive(false);
            var controller = root.AddComponent<TowerPlayableController>();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_startMode").enumValueIndex = (int)TowerStartMode.GroundFloorStart;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            controller.Initialize();
            EditorSceneManager.SaveScene(scene, GroundStartScenePath);
            Debug.Log($"Successfully rebuilt and saved {GroundStartScenePath}");
        }
    }
}
