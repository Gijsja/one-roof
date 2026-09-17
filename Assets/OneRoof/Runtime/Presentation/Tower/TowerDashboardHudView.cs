using System;
using OneRoof.Application.Modes;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Top-left dashboard HUD displaying simulation metrics, quick inspection/overlay toggles,
    /// and build tool shortcuts.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TowerDashboardHudView : MonoBehaviour
    {
        private TowerPlayableController _controller;
        private GUIStyle _hudHeaderStyle;
        private GUIStyle _hudMetricStyle;
        private GUIStyle _hudButtonStyle;
        private GUIStyle _hudHelpStyle;

        public TowerPlayableController Controller
        {
            get => _controller;
            set => _controller = value;
        }

        private void OnGUI()
        {
            if (_controller == null)
            {
                _controller = GetComponent<TowerPlayableController>();
            }

            if (_controller == null) return;

            var sim = _controller.SimulationSession;
            var mode = _controller.ModeSession;
            if (sim == null || mode == null) return;

            EnsureStyles();

            var snapshot = sim.Projection();
            var congestion = sim.CongestionProjection();
            var totalRes = Math.Max(1, sim.ResidentCount);

            var hudHeight = mode.CurrentMode == InteractionMode.Build ? 260 : 220;
            var hudRect = new Rect(20, 20, 380, hudHeight);
            GUILayout.BeginArea(hudRect, GUI.skin.box);

            GUILayout.Label("ONE ROOF — FIRST PLAYABLE SLICE", _hudHeaderStyle);
            GUILayout.Label($"Sim Tick: {snapshot.Tick}  •  Status: {(_controller.IsPaused ? "[PAUSED]" : "[RUNNING]")}", _hudMetricStyle);
            GUILayout.Label($"Treasury: ${sim.Economy.CashBalance:N0}  •  Residents: {sim.ResidentCount}  •  Floors: {sim.FloorCount}", _hudMetricStyle);
            GUILayout.Space(4);

            var overlay = _controller.OverlayPresenter?.CurrentOverlay ?? _controller.OverlayService?.CreateOverlay(congestion);
            var severityBadge = overlay?.OverallSeverity.ToString().ToUpperInvariant() ?? "OPTIMAL";

            GUILayout.Label($"Lobby Queue: {snapshot.QueueLength}/{totalRes} waiting  [{severityBadge}]", _hudMetricStyle);
            GUILayout.Label($"Delivered: {snapshot.ArrivedCount}/{totalRes} arrived", _hudMetricStyle);
            GUILayout.Label($"Average Completed Wait: {snapshot.AverageWaitTicks:F1} ticks", _hudMetricStyle);
            GUILayout.Label($"Elevator Bank: {snapshot.Elevators.Count} active car(s)", _hudMetricStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Inspect Bottleneck [I]", _hudButtonStyle, GUILayout.Height(28))) _controller.InspectBottleneck();
            if (GUILayout.Button("Flow Overlay [D]", _hudButtonStyle, GUILayout.Height(28))) _controller.ToggleDataOverlay();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Car [B]", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowPlacementPreview();
            if (GUILayout.Button("+ Add Car Now", _hudButtonStyle, GUILayout.Height(28))) _controller.OnConfirmElevatorPlacement();
            if (GUILayout.Button("Reset [R]", _hudButtonStyle, GUILayout.Height(28))) _controller.ResetCommuteSimulation();
            GUILayout.EndHorizontal();

            if (mode.CurrentMode == InteractionMode.Build)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Apt", _hudButtonStyle, GUILayout.Height(24))) mode.SelectBuildTool("residential:apartment");
                if (GUILayout.Button("+ Diner", _hudButtonStyle, GUILayout.Height(24))) mode.SelectBuildTool("commercial:diner");
                if (GUILayout.Button("+ Shaft", _hudButtonStyle, GUILayout.Height(24))) mode.SelectBuildTool("transit:elevator_shaft");
                if (GUILayout.Button("+ Slab", _hudButtonStyle, GUILayout.Height(24))) mode.SelectBuildTool("floor:slab");
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);
            GUILayout.Label("Shortcuts: [Space] Pause  [1] Build  [2] Inspect  [3] Data  [R-Click / Esc] Cancel", _hudHelpStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_hudHeaderStyle != null) return;

            _hudHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.25f, 0.88f, 1f) }
            };

            _hudMetricStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _hudButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _hudHelpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
            };
        }
    }
}
