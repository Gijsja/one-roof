using System;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.Application.Modes.Commands;
using OneRoof.UI;
using UnityEngine;

namespace OneRoof.UI.Inspectors
{
    /// <summary>
    /// UI inspector view implementing the full explanation chain (Docs/04_UX_CONTRACT.md):
    /// symptom -> overlay -> inspector cause -> direct build/policy response route.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CongestionInspectorCardView : MonoBehaviour
    {
        private ElevatorCongestionInspectorProjection _currentProjection;
        private ModeShellSession _session;
        private bool _isOpen;
        private GUIStyle _headerStyle;
        private GUIStyle _symptomStyle;
        private GUIStyle _causeStyle;
        private GUIStyle _actionButtonStyle;

        public bool IsOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        public ModeShellSession Session
        {
            get => _session;
            set => _session = value;
        }

        public ElevatorCongestionInspectorProjection CurrentProjection => _currentProjection;

        public void Inspect(ElevatorCongestionInspectorProjection projection)
        {
            _currentProjection = projection;
            _isOpen = projection != null;
        }

        public void Close()
        {
            _isOpen = false;
        }

        public void ExecuteDirectResponse()
        {
            if (_session != null && _currentProjection != null && _currentProjection.CanDirectRouteToBuild)
            {
                _session.ExecuteCommand(new SetInteractionModeCommand(
                    InteractionMode.Build,
                    toolId: _currentProjection.TargetBuildTool,
                    targetFloor: _currentProjection.FloorLevel));
                _isOpen = false;
            }
        }

        private void OnGUI()
        {
            if (!_isOpen || _currentProjection == null)
            {
                return;
            }

            EnsureStyles();

            var cardRect = new Rect(Screen.width - 376, 16, 360, 370);
            GUILayout.BeginArea(cardRect, StewardTheme.Panel);

            GUILayout.Label("INSPECT  /  TRANSIT", StewardTheme.Label(10, StewardTheme.Mint, true));

            GUILayout.Label(_currentProjection.Title, _headerStyle);
            GUILayout.Space(6);

            GUILayout.Label("SYMPTOM", _symptomStyle);
            GUILayout.Label(_currentProjection.SymptomDescription, _causeStyle);
            GUILayout.Space(6);

            GUILayout.Label("CONTRIBUTING CAUSES", _symptomStyle);
            for (var i = 0; i < _currentProjection.ContributingCauses.Count; i++)
            {
                GUILayout.Label($"• {_currentProjection.ContributingCauses[i]}", _causeStyle);
            }
            GUILayout.Space(8);

            GUILayout.Label("SYSTEM RESPONSE", _symptomStyle);
            GUILayout.Label(_currentProjection.SuggestedResponseAction, _causeStyle);
            GUILayout.Space(8);

            if (_currentProjection.CanDirectRouteToBuild)
            {
                if (GUILayout.Button("BUILD MORE CAPACITY", _actionButtonStyle, GUILayout.Height(34)))
                {
                    ExecuteDirectResponse();
                }
            }

            if (GUILayout.Button("CLOSE", StewardTheme.Button, GUILayout.Height(28)))
            {
                Close();
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_headerStyle != null)
            {
                return;
            }

            _headerStyle = StewardTheme.Label(16, StewardTheme.Text, true);
            _symptomStyle = StewardTheme.Label(10, StewardTheme.Muted, true);
            _causeStyle = StewardTheme.Label(12, StewardTheme.Text);
            _actionButtonStyle = StewardTheme.ActiveButton;
        }
    }
}
