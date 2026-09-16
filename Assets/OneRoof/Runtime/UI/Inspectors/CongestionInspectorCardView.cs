using System;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.Application.Modes.Commands;
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

            var cardRect = new Rect(Screen.width - 360, 80, 340, 310);
            GUILayout.BeginArea(cardRect, GUI.skin.window);

            GUILayout.Label(_currentProjection.Title, _headerStyle);
            GUILayout.Space(6);

            GUILayout.Label("SYMPTOM:", _symptomStyle);
            GUILayout.Label(_currentProjection.SymptomDescription);
            GUILayout.Space(6);

            GUILayout.Label("CONTRIBUTING CAUSES:", _symptomStyle);
            for (var i = 0; i < _currentProjection.ContributingCauses.Count; i++)
            {
                GUILayout.Label($"• {_currentProjection.ContributingCauses[i]}", _causeStyle);
            }
            GUILayout.Space(8);

            GUILayout.Label("RECOMMENDED RESPONSE:", _symptomStyle);
            GUILayout.Label(_currentProjection.SuggestedResponseAction);
            GUILayout.Space(8);

            if (_currentProjection.CanDirectRouteToBuild)
            {
                if (GUILayout.Button("Open Build Mode (Add Capacity)", _actionButtonStyle, GUILayout.Height(34)))
                {
                    ExecuteDirectResponse();
                }
            }

            if (GUILayout.Button("Close", GUILayout.Height(24)))
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

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.45f, 0.35f) }
            };

            _symptomStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            _causeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) }
            };

            _actionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.9f, 0.6f) }
            };
        }
    }
}
