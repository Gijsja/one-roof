using System.Collections.Generic;
using OneRoof.Content;
using OneRoof.Presentation.Architecture;
using OneRoof.Presentation.Furnishings;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.AssetLab
{
    /// <summary>
    /// Standalone presentation testbed & benchmark HUD for 2D asset validation.
    /// Provides real-time metrics (FPS, frame time, active sprite renderers)
    /// and interactive inspection controls (clutter alpha, animation scrubbing)
    /// without requiring the Tower simulation or domain economic dependencies.
    /// </summary>
    public sealed class AssetLabBenchmarkDisplay : MonoBehaviour
    {
        private float _deltaTime;
        private float _clutterAlpha = 1.0f;
        private int _selectedClipIndex = 0;
        private readonly List<RoomFurnishingPresenter> _furnishings = new List<RoomFurnishingPresenter>();
        private readonly List<RoomBackdropPresenter> _backdrops = new List<RoomBackdropPresenter>();
        private readonly List<NpcSkeletalHierarchy> _rigs = new List<NpcSkeletalHierarchy>();

        private static readonly string[] PoseNames = {
            "Idle", "Walk", "Run", "Crawl", "Sit", "Sleep", "WorkDesk", "QueueWait", "Ride"
        };

        private static readonly string[] ConditionNames = { "Pristine", "Worn", "Degraded" };
        private int _selectedConditionIndex = 0;
        private float _interactiveWidth = 2.5f;
        private bool _isTiledMode = true;

        private void Start()
        {
            RefreshSceneComponents();
        }

        public void RefreshSceneComponents()
        {
            _furnishings.Clear();
            _furnishings.AddRange(FindObjectsByType<RoomFurnishingPresenter>(FindObjectsSortMode.None));

            _backdrops.Clear();
            _backdrops.AddRange(FindObjectsByType<RoomBackdropPresenter>(FindObjectsSortMode.None));

            _rigs.Clear();
            _rigs.AddRange(FindObjectsByType<NpcSkeletalHierarchy>(FindObjectsSortMode.None));
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            const float panelWidth = 340f;
            const float panelHeight = 390f;

            GUILayout.BeginArea(new Rect(16, 16, panelWidth, panelHeight), GUI.skin.box);
            GUILayout.Label("<b>One Roof - AssetLab Benchmark Matrix</b>", GUI.skin.label);

            var msec = _deltaTime * 1000.0f;
            var fps = 1.0f / Mathf.Max(0.0001f, _deltaTime);
            var fpsColor = fps >= 55f ? "#40d9ff" : (fps >= 30f ? "#ffd700" : "#ff4040");

            GUILayout.Label($"Performance: <color={fpsColor}>{fps:0.} FPS ({msec:0.0} ms)</color>", GetRichTextStyle());

            var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            GUILayout.Label($"Active SpriteRenderers: <b>{renderers.Length}</b>");
            GUILayout.Label($"Rooms: <b>{_backdrops.Count}</b> | Test Rigs: <b>{_rigs.Count}</b>");

            GUILayout.Space(6);
            GUILayout.Label($"Room Width (Cells): {Mathf.RoundToInt(_interactiveWidth / 0.5f)} ({_interactiveWidth:F1}m)");
            var newWidth = GUILayout.HorizontalSlider(_interactiveWidth, 0.5f, 4.0f);
            if (!Mathf.Approximately(newWidth, _interactiveWidth))
            {
                _interactiveWidth = newWidth;
                for (var i = 0; i < _backdrops.Count; i++)
                {
                    _backdrops[i]?.SetWidth(_interactiveWidth);
                }
            }

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Wall Mode:", GUILayout.Width(80));
            if (GUILayout.Button(_isTiledMode ? "Tiled (0.5m Pitch)" : "Sliced (Stretched)"))
            {
                _isTiledMode = !_isTiledMode;
                var mode = _isTiledMode ? SpriteDrawMode.Tiled : SpriteDrawMode.Sliced;
                for (var i = 0; i < _backdrops.Count; i++)
                {
                    _backdrops[i]?.SetBackdropDrawMode(mode);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Wear Stage:", GUILayout.Width(80));
            if (GUILayout.Button(ConditionNames[_selectedConditionIndex]))
            {
                _selectedConditionIndex = (_selectedConditionIndex + 1) % ConditionNames.Length;
                var stage = (OneRoof.Domain.Population.HousingConditionStage)_selectedConditionIndex;
                for (var i = 0; i < _backdrops.Count; i++)
                {
                    _backdrops[i]?.SetHousingCondition(stage);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"Prop Clutter Alpha: {_clutterAlpha:F2}");
            var newAlpha = GUILayout.HorizontalSlider(_clutterAlpha, 0.0f, 1.0f);
            if (!Mathf.Approximately(newAlpha, _clutterAlpha))
            {
                _clutterAlpha = newAlpha;
                for (var i = 0; i < _furnishings.Count; i++)
                {
                    _furnishings[i]?.SetClutterAlpha(_clutterAlpha);
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Rig Pose Evaluation:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◄", GUILayout.Width(30)))
            {
                _selectedClipIndex = (_selectedClipIndex - 1 + PoseNames.Length) % PoseNames.Length;
                ApplyPoseToRigs();
            }
            GUILayout.Label(PoseNames[_selectedClipIndex], GUILayout.ExpandWidth(true));
            if (GUILayout.Button("►", GUILayout.Width(30)))
            {
                _selectedClipIndex = (_selectedClipIndex + 1) % PoseNames.Length;
                ApplyPoseToRigs();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void ApplyPoseToRigs()
        {
            var clip = (NpcAnimationClip)_selectedClipIndex;
            for (var i = 0; i < _rigs.Count; i++)
            {
                if (_rigs[i] == null) continue;
                _rigs[i].SetAnimationClip(clip, "AssetLab Benchmark Scrubber");
                _rigs[i].ApplyProceduralAnimation(Time.time);
            }
        }

        private GUIStyle GetRichTextStyle()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                richText = true
            };
            return style;
        }
    }
}
