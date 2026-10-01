using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEditor;
using UnityEngine;

namespace OneRoof.Editor.AssetLab
{
    /// <summary>Isolated, scrub-able preview of the same procedural pose evaluator used at runtime.</summary>
    public sealed class ResidentAnimationPreviewWindow : EditorWindow
    {
        private PreviewRenderUtility _preview;
        private NpcSkeletalHierarchy _rig;
        private NpcAnimationClip _clip;
        private int _variant;
        private float _sampleTime;
        private bool _facingLeft;
        private Vector2 _boneScroll;
        private bool _isPlaying;
        private float _playbackSpeed = 1f;
        private double _lastTime;

        [MenuItem("One Roof/Residents/Animation Preview")]
        public static void Open() => GetWindow<ResidentAnimationPreviewWindow>("Resident Animation");

        private void OnEnable()
        {
            _preview = new PreviewRenderUtility();
            _preview.camera.nearClipPlane = .01f;
            _preview.camera.farClipPlane = 100f;
            _preview.camera.orthographic = true;
            _preview.camera.orthographicSize = 0.7f;
            _preview.camera.transform.position = new Vector3(0f, 0.35f, -10f);
            _preview.camera.clearFlags = CameraClearFlags.SolidColor;
            _preview.camera.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
            var root = new GameObject("Isolated Resident Preview") { hideFlags = HideFlags.HideAndDontSave };
            _rig = root.AddComponent<NpcSkeletalHierarchy>();
            _rig.Initialize(_variant);
            _preview.AddSingleGO(root);
            _lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _preview?.Cleanup();
            _preview = null;
            _rig = null;
        }

        private void OnEditorUpdate()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = (float)(now - _lastTime);
            _lastTime = now;
            if (_isPlaying)
            {
                _sampleTime = (_sampleTime + dt * _playbackSpeed) % 10f;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (_preview == null || _rig == null) return;
            EditorGUILayout.HelpBox("Source: NpcSkeletalHierarchy.ApplyProceduralAnimation. Close views use explicit bone transforms; distant views use a generated neutral proxy. Preview objects are isolated from the Tower scene.", MessageType.Info);
            if (GUILayout.Button("Open pose evaluator source"))
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs"));
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<NpcSkeletalHierarchy>() : null;
            if (selected != null)
            {
                EditorGUILayout.LabelField("Selected resident", selected.ResidentIndex.ToString());
                EditorGUILayout.LabelField("Selected pose", selected.CurrentAnimation.ToString());
                EditorGUILayout.LabelField("Selection reason", selected.AnimationSelectionReason);
                EditorGUILayout.LabelField("Last sampled time", selected.LastAnimationSampleTime.ToString("F3"));
            }
            EditorGUI.BeginChangeCheck();
            _clip = (NpcAnimationClip)EditorGUILayout.EnumPopup("Pose", _clip);
            _variant = EditorGUILayout.IntSlider("Palette / proportions", _variant, 0, NpcWardrobeVariantCatalog.Count - 1);
            _facingLeft = EditorGUILayout.Toggle("Face left", _facingLeft);
            _sampleTime = EditorGUILayout.Slider("Sample time (seconds)", _sampleTime, 0f, 10f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_isPlaying ? "Pause" : "Play", GUILayout.Width(70)))
            {
                _isPlaying = !_isPlaying;
                _lastTime = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("Reset", GUILayout.Width(55)))
            {
                _sampleTime = 0f;
                Repaint();
            }
            _playbackSpeed = EditorGUILayout.Slider("Speed", _playbackSpeed, 0.1f, 2.0f);
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck()) _rig.Initialize(_variant);
            _rig.SetFacing(_facingLeft ? -1f : 1f);
            _rig.SetAnimationClip(_clip, "Isolated editor preview");
            _rig.SetBubbleVisible(false);
            _rig.ApplyProceduralAnimation(_sampleTime);
            EditorGUILayout.LabelField("Effective time", (_rig.LastAnimationSampleTime + _rig.AnimationPhaseOffset).ToString("F3") + " s (sample + resident phase)");
            var rect = GUILayoutUtility.GetRect(200f, 250f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                var bounds = _rig.MainRenderer.bounds;
                bool hasBounds = false;
                foreach (var renderer in _rig.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                _preview.camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                _preview.camera.orthographicSize = Mathf.Max(bounds.size.y * .5f, bounds.size.x * .5f / (rect.width / rect.height)) * 1.15f;
                _preview.BeginPreview(rect, GUIStyle.none);
                _preview.Render(true);
                GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.ScaleToFit, false);
            }
            EditorGUILayout.LabelField("Evaluated bone transforms", EditorStyles.boldLabel);
            _boneScroll = EditorGUILayout.BeginScrollView(_boneScroll);
            foreach (var pair in _rig.Bones)
            {
                EditorGUILayout.LabelField(pair.Key, "position " + pair.Value.localPosition.ToString("F3") + " / angle " + Mathf.DeltaAngle(0f, pair.Value.localEulerAngles.z).ToString("F1"));
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
