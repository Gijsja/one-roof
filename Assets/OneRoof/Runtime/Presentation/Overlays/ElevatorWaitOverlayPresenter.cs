using System.Collections.Generic;
using OneRoof.Application.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    /// <summary>
    /// Renders floor congestion overlays, animated flow direction, and non-color accessibility badges
    /// (Docs/04_UX_CONTRACT.md).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElevatorWaitOverlayPresenter : MonoBehaviour
    {
        private ElevatorWaitOverlayProjection _currentOverlay;
        private bool _isVisible;
        private GUIStyle _badgeStyle;
        private GUIStyle _bottleneckStyle;
        private GUIStyle _flowStyle;

        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        public ElevatorWaitOverlayProjection CurrentOverlay => _currentOverlay;

        public void UpdateOverlay(ElevatorWaitOverlayProjection overlay)
        {
            _currentOverlay = overlay;
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
        }

        private void OnGUI()
        {
            if (!_isVisible || _currentOverlay == null)
            {
                return;
            }

            EnsureStyles();

            for (var i = 0; i < _currentOverlay.FloorFlows.Count; i++)
            {
                var flow = _currentOverlay.FloorFlows[i];
                DrawFloorOverlay(flow);
            }
        }

        private void DrawFloorOverlay(in FloorWaitFlowProjection flow)
        {
            var screenY = FloorScreenY(flow.FloorLevel);
            var rect = new Rect(20, screenY, 320, 26);

            var style = flow.IsBottleneck ? _bottleneckStyle : _badgeStyle;
            GUI.Label(rect, flow.NonColorBadge, style);

            if (flow.QueuedCount > 0)
            {
                var arrowCount = Mathf.Clamp(Mathf.CeilToInt(flow.FlowIntensity * 5), 1, 5);
                var arrowStr = new string('◀', arrowCount) + " LOBBY FLOW";
                var flowRect = new Rect(350, screenY, 140, 26);
                GUI.Label(flowRect, arrowStr, _flowStyle);
            }
        }

        private static float FloorScreenY(int floor)
        {
            // Map floor index (0 to 4) to vertical screen coordinates
            var normalized = (4 - floor) / 4.5f;
            return 80 + normalized * (Screen.height - 240);
        }

        private void EnsureStyles()
        {
            if (_badgeStyle != null)
            {
                return;
            }

            _badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            _bottleneckStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.4f, 0.3f) }
            };

            _flowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
        }
    }
}
