using System.IO;
using OneRoof.Content;
using OneRoof.Presentation.Architecture;
using OneRoof.Presentation.AssetLab;
using OneRoof.Presentation.Furnishings;
using OneRoof.Presentation.Population;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneRoof.Editor.AssetLab
{
    /// <summary>
    /// Builds the standalone 2D asset preview and benchmark scene (AssetLab_ModularTest.unity).
    /// Allows testing multi-cell room backdrops, floor-baseline prop docking, and
    /// procedural resident rigs without mutating or touching the main Tower simulation.
    /// </summary>
    public static class AssetLabModularTestBuilder
    {
        public const string ScenePath = "Assets/Scenes/AssetLab_ModularTest.unity";

        [MenuItem("One Roof/AssetLab/Build Modular Test Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Orthographic 2.5D Camera
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.10f); // Midnight navy core #0a0f18
            camObj.transform.position = new Vector3(5.5f, 1.2f, -10f);

            // 2. Benchmark Controller HUD
            var benchmarkObj = new GameObject("AssetLab Benchmark Controller");
            benchmarkObj.AddComponent<AssetLabBenchmarkDisplay>();

            // 3. Testbed Root
            var testbedRoot = new GameObject("AssetLab Testbed Root");

            // Build 4 Demo Rooms: 1-cell (0.5m), 2-cell (1.0m), 4-cell (2.0m), and 6-cell (3.0m)
            CreateDemoRoom(testbedRoot.transform, "residential", 0f, 1.0f, 0.5f, 1.42f, 0, NpcAnimationClip.Idle);
            CreateDemoRoom(testbedRoot.transform, "residential", 1.8f, 1.0f, 1.0f, 1.42f, 1, NpcAnimationClip.Sit);
            CreateDemoRoom(testbedRoot.transform, "commercial:office", 4.3f, 1.0f, 2.0f, 1.42f, 2, NpcAnimationClip.WorkDesk);
            CreateDemoRoom(testbedRoot.transform, "lobby", 7.8f, 1.0f, 3.0f, 1.42f, 3, NpcAnimationClip.Walk);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[AssetLab] Modular test scene created at: {ScenePath}");
        }

        private static void CreateDemoRoom(
            Transform parent,
            string roomTheme,
            float posX,
            float posY,
            float width,
            float height,
            int residentVariant,
            NpcAnimationClip clip)
        {
            var roomObj = new GameObject($"Room_{roomTheme.Replace(':', '_')}");
            roomObj.transform.SetParent(parent, false);
            roomObj.transform.position = new Vector3(posX, posY, 0f);

            // 1. Room Backdrop & Fixtures
            var backdropPresenter = roomObj.AddComponent<RoomBackdropPresenter>();
            backdropPresenter.Setup(
                roomTheme,
                width,
                height,
                posX - width * 0.5f,
                posX + width * 0.5f,
                posY,
                isWestSide: false);

            // 2. Room Props & Baseline Furnishings
            var furnishingPresenter = roomObj.AddComponent<RoomFurnishingPresenter>();
            furnishingPresenter.FurnishRoom(roomTheme, width, height, isWestSide: false);

            // 3. Resident Rig
            var residentObj = new GameObject($"Resident_Rig_{residentVariant}");
            residentObj.transform.SetParent(roomObj.transform, false);
            residentObj.transform.localPosition = new Vector3(0f, -height * 0.5f, -0.05f); // Anchor on floor baseline

            var rig = residentObj.AddComponent<NpcSkeletalHierarchy>();
            rig.Initialize(residentVariant);
            rig.SetAnimationClip(clip, "AssetLab Testbed Demonstration");
            rig.ApplyProceduralAnimation(0.5f);
        }
    }
}
