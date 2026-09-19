using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneRoof.Editor.AssetLab
{
    /// <summary>Creates the editor-only AssetLab preview scene used by the content validation gate.</summary>
    [InitializeOnLoad]
    public static class AssetLabSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/AssetLab.unity";

        static AssetLabSceneBuilder() { EditorApplication.delayCall += EnsureSceneExists; }

        [MenuItem("One Roof/AssetLab/Create Preview Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("AssetLab (Editor Only)");
            root.AddComponent<AssetLabPreviewController>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureSceneExists() { if (!File.Exists(ScenePath)) CreateScene(); }
    }

    /// <summary>Visible scene marker; validation is intentionally invoked through the menu/CI method.</summary>
    public sealed class AssetLabPreviewController : MonoBehaviour { }
}
