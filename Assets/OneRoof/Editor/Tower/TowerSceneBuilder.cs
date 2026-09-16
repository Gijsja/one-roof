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
    }
}
