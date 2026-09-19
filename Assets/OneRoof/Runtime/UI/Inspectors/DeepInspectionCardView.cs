using OneRoof.Application.Inspectors;
using UnityEngine;

namespace OneRoof.UI.Inspectors
{
    /// <summary>Renders resident, room, and elevator-bank drill-down projections.</summary>
    [DisallowMultipleComponent]
    public sealed class DeepInspectionCardView : MonoBehaviour
    {
        private InspectorDetailProjection _currentProjection;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;

        public bool IsOpen { get; private set; }
        public InspectorDetailProjection CurrentProjection => _currentProjection;

        public void Inspect(InspectorDetailProjection projection)
        {
            _currentProjection = projection;
            IsOpen = projection != null;
        }

        public void Close() => IsOpen = false;

        private void OnGUI()
        {
            if (!IsOpen || _currentProjection == null) return;
            EnsureStyles();
            GUILayout.BeginArea(new Rect(Screen.width - 360, 80, 340, 360), GUI.skin.window);
            GUILayout.Label(_currentProjection.Title, _headerStyle);
            GUILayout.Space(6);
            GUILayout.Label("CURRENT STATE", _labelStyle);
            GUILayout.Label(_currentProjection.Symptom, _labelStyle);
            GUILayout.Space(6);
            GUILayout.Label("DETAILS", _labelStyle);
            foreach (var detail in _currentProjection.Details) GUILayout.Label($"• {detail}", _labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(_currentProjection.SuggestedResponse, _labelStyle);
            if (GUILayout.Button("Close", GUILayout.Height(24))) Close();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_headerStyle != null) return;
            _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, .85f, .2f) } };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, normal = { textColor = new Color(.88f, .92f, .98f) } };
        }
    }
}
