using OneRoof.Application.Inspectors;
using OneRoof.UI;
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
            GUILayout.BeginArea(new Rect(Screen.width - 376, 16, 360, 408), StewardTheme.Panel);
            GUILayout.Label("INSPECT  /  CAUSE CHAIN", StewardTheme.Label(10, StewardTheme.Mint, true));
            GUILayout.Label(_currentProjection.Title, _headerStyle);
            GUILayout.Space(4);
            GUILayout.Label("CURRENT STATE", StewardTheme.Label(10, StewardTheme.Muted, true));
            GUILayout.Label(_currentProjection.Symptom, _labelStyle);
            GUILayout.Space(5);
            StewardTheme.Rule(332);
            GUILayout.Label("CONTRIBUTING FACTORS", StewardTheme.Label(10, StewardTheme.Muted, true));
            foreach (var detail in _currentProjection.Details) GUILayout.Label($"• {detail}", _labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("SYSTEM RESPONSE", StewardTheme.Label(10, StewardTheme.Mint, true));
            GUILayout.Label(_currentProjection.SuggestedResponse, _labelStyle);
            if (GUILayout.Button("CLOSE", StewardTheme.Button, GUILayout.Height(28))) Close();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_headerStyle != null) return;
            _headerStyle = StewardTheme.Label(16, StewardTheme.Text, true);
            _labelStyle = StewardTheme.Label(12, StewardTheme.Text);
        }
    }
}
