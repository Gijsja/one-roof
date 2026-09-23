using System;
using OneRoof.Application.Prediction;
using OneRoof.UI;
using UnityEngine;

namespace OneRoof.UI.Prediction
{
    /// <summary>
    /// UI component rendering cost, footprint validity, before/after wait estimates,
    /// and confidence labeling for placement previews (Docs/04_UX_CONTRACT.md).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlacementPreviewCardView : MonoBehaviour
    {
        private ElevatorPlacementPreviewProjection _currentProjection;
        private bool _isOpen;
        private Action _onConfirmPlacement;

        private GUIStyle _headerStyle;
        private GUIStyle _metricStyle;
        private GUIStyle _improvementStyle;
        private GUIStyle _confidenceStyle;
        private GUIStyle _confirmButtonStyle;

        public bool IsOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        public ElevatorPlacementPreviewProjection CurrentProjection => _currentProjection;

        public void SetPreview(ElevatorPlacementPreviewProjection projection, Action onConfirm = null)
        {
            _currentProjection = projection;
            _onConfirmPlacement = onConfirm;
            _isOpen = projection != null;
        }

        public void Close()
        {
            _isOpen = false;
            _onConfirmPlacement = null;
        }

        public void Confirm()
        {
            if (_isOpen && _currentProjection != null && _currentProjection.IsValid)
            {
                _onConfirmPlacement?.Invoke();
                Close();
            }
        }

        private void OnGUI()
        {
            if (!_isOpen || _currentProjection == null)
            {
                return;
            }

            EnsureStyles();

            var cardRect = new Rect(Screen.width - 376, Screen.height - 322, 360, 306);
            GUILayout.BeginArea(cardRect, StewardTheme.Panel);

            GUILayout.Label("BUILD  /  IMPACT PREVIEW", StewardTheme.Label(10, StewardTheme.Mint, true));
            GUILayout.Label(_currentProjection.ItemName, _headerStyle);
            GUILayout.Label($"Cost: ${_currentProjection.Cost}  •  Status: {(_currentProjection.IsValid ? "Valid" : "Blocked")}");
            GUILayout.Space(6);

            if (!_currentProjection.IsValid)
            {
                GUILayout.Label($"Reason: {_currentProjection.InvalidReason}", _metricStyle);
            }
            else
            {
                GUILayout.Label("ESTIMATED IMPACT:", _metricStyle);
                GUILayout.Label($"Elevator cars: {_currentProjection.CurrentCarCount}  ➜  {_currentProjection.PredictedCarCount}");
                GUILayout.Label($"Average wait: {_currentProjection.CurrentAverageWaitTicks:F1} ticks  ➜  {_currentProjection.PredictedAverageWaitTicks:F1} ticks");
                GUILayout.Label($"Max wait: {_currentProjection.CurrentMaxWaitTicks} ticks  ➜  {_currentProjection.PredictedMaxWaitTicks} ticks");
                GUILayout.Label($"Congestion: {_currentProjection.CurrentSeverity}  ➜  {_currentProjection.PredictedSeverity}");
                GUILayout.Label($"Estimated improvement: {_currentProjection.EstimatedImprovementPercentage:F1}%", _improvementStyle);
                GUILayout.Space(4);

                GUILayout.Label(_currentProjection.ConfidenceLabel, _confidenceStyle);
                GUILayout.Space(6);

                if (GUILayout.Button("CONFIRM BUILD", _confirmButtonStyle, GUILayout.Height(30)))
                {
                    Confirm();
                }
            }

            if (GUILayout.Button("CANCEL", StewardTheme.Button, GUILayout.Height(25)))
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

            _metricStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _improvementStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.95f, 0.55f) }
            };

            _confidenceStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.85f, 0.95f) }
            };

            _confirmButtonStyle = StewardTheme.ActiveButton;
        }
    }
}
