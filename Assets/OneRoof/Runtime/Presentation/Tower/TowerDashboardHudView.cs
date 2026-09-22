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

        public bool IsCollapsed { get; private set; }

        public void SetCollapsed(bool collapsed) => IsCollapsed = collapsed;

        public void ToggleCollapsed() => IsCollapsed = !IsCollapsed;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame) ToggleCollapsed();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.H)) ToggleCollapsed();
#endif
        }

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

            // Collapsed to a single chip so the top tower floors stay readable.
            if (IsCollapsed)
            {
                var chipRect = new Rect(20, 20, 210, 48);
                GUILayout.BeginArea(chipRect, GUI.skin.box);
                if (GUILayout.Button("Show HUD [H]", _hudButtonStyle, GUILayout.Height(28))) ToggleCollapsed();
                GUILayout.EndArea();
                return;
            }

            var snapshot = sim.Projection();
            var congestion = sim.CongestionProjection();
            var totalRes = Math.Max(1, sim.ResidentCount);
            var groundStart = _controller.IsGroundStart;

            var hudRect = groundStart ? new Rect(20, 20, 700, 560) : new Rect(20, 20, 700, 352);
            GUILayout.BeginArea(hudRect, GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label(groundStart ? "ONE ROOF — GROUND-FLOOR START" : "ONE ROOF — FIRST PLAYABLE SLICE", _hudHeaderStyle);
            if (GUILayout.Button("Hide [H]", _hudButtonStyle, GUILayout.Height(24), GUILayout.Width(90))) ToggleCollapsed();
            GUILayout.EndHorizontal();
            var phase = sim.DayPhase;
            GUILayout.Label($"Day {phase.DayNumber} — {phase.ClockLabel} {(phase.IsNight ? "Night" : "Day")}  •  Tick: {snapshot.Tick}  •  Status: {(_controller.IsPaused ? "[PAUSED]" : "[RUNNING]")}", _hudMetricStyle);
            GUILayout.Label($"Treasury: ${sim.TreasuryBalance:N0}  •  Residents: {sim.ResidentCount}  •  Floors: {sim.FloorCount}", _hudMetricStyle);
            GUILayout.Space(4);

            var overlay = _controller.OverlayPresenter?.CurrentOverlay ?? _controller.DataOverlays?.ElevatorWait;
            var severityBadge = overlay?.OverallSeverity.ToString().ToUpperInvariant() ?? "OPTIMAL";

            GUILayout.Label($"Lobby Queue: {snapshot.QueueLength}/{totalRes} waiting  [{severityBadge}]", _hudMetricStyle);
            GUILayout.Label($"Delivered: {snapshot.ArrivedCount}/{totalRes} arrived", _hudMetricStyle);
            GUILayout.Label($"Average Completed Wait: {snapshot.AverageWaitTicks:F1} ticks", _hudMetricStyle);
            GUILayout.Label($"Elevator Bank: {snapshot.Elevators.Count} active car(s)", _hudMetricStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Inspect Bottleneck [I]", _hudButtonStyle, GUILayout.Height(28))) _controller.InspectBottleneck();
            if (GUILayout.Button("Flow Overlay [D]", _hudButtonStyle, GUILayout.Height(28))) _controller.ToggleDataOverlay();
            if (GUILayout.Button("Satisfaction", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowSatisfactionOverlay();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Population", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowPopulationOverlay();
            if (GUILayout.Button("Scrutiny", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowScrutinyOverlay();
            if (GUILayout.Button("Foot Traffic", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowFootTrafficOverlay();
            if (GUILayout.Button("Business Health", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowBusinessHealthOverlay();
            if (GUILayout.Button("Utilities", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowUtilitiesOverlay();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Car [B]", _hudButtonStyle, GUILayout.Height(28))) _controller.ShowPlacementPreview();
            if (GUILayout.Button("+ Add Car Now", _hudButtonStyle, GUILayout.Height(28))) _controller.OnConfirmElevatorPlacement();
            if (GUILayout.Button("Reset [R]", _hudButtonStyle, GUILayout.Height(28))) _controller.ResetCommuteSimulation();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("Shortcuts: [Space] Pause  [1/B] Build  [2/I] Inspect  [3/D] Data  [4/M] Manage  [H] HUD  [Esc] Cancel", _hudHelpStyle);
            if (groundStart) DrawGroundStartChecklist(sim);
            GUILayout.EndArea();
        }

        /// <summary>
        /// From-scratch progress checklist: each of the five focus systems shows a
        /// live, read-only milestone so economy, utilities, commute, routines, and
        /// expansion stay legible from the first slab. Presentation only — no domain writes.
        /// </summary>
        private void DrawGroundStartChecklist(Application.Tower.TowerSimulationSession sim)
        {
            GUILayout.Space(6);
            GUILayout.Label("FROM-SCRATCH CHECKLIST", _hudHeaderStyle);

            var rentDone = sim.TotalRevenue > 0;
            GUILayout.Label($"{Mark(rentDone)} Economy — Treasury ${sim.TreasuryBalance:N0} {(rentDone ? $"• rent collected ${sim.TotalRevenue:N0}" : "• build homes + workplaces to earn rent")}", _hudMetricStyle);

            var power = sim.ElectricalGridProjection();
            var powerOk = power.SubstationCapacity > 0f;
            GUILayout.Label($"{Mark(powerOk)} Utility — Power {(powerOk ? $"substation {power.SubstationCapacity:F0} vs demand {power.TotalDemand:F0}" : "no substation: build utility:electrical_substation")}", _hudMetricStyle);

            var water = sim.WaterWasteNetworkProjection();
            var waterOk = water.PumpCapacity > 0f;
            GUILayout.Label($"{Mark(waterOk)} Utility — Water {(waterOk ? $"pumps {water.PumpCapacity:F0} vs demand {water.TotalDemand:F0}" : "no pumps: build utility:water_pump")}", _hudMetricStyle);

            var congestion = sim.CongestionProjection();
            var commuteOk = sim.FloorCount > 1 && sim.ResidentCount > 0;
            GUILayout.Label($"{Mark(commuteOk)} Commute — {sim.ElevatorCarCount} car(s), {congestion.TotalQueued} queued, avg wait {congestion.AverageWaitTicks:F1} ticks", _hudMetricStyle);

            var routineOk = sim.ResidentCount > 0;
            GUILayout.Label($"{Mark(routineOk)} Routine — {sim.ResidentCount} resident(s), {sim.ActiveTripCount} trip(s) in transit", _hudMetricStyle);

            var expandOk = sim.FloorCount > 1;
            GUILayout.Label($"{Mark(expandOk)} Expansion — {sim.FloorCount} floor(s), {sim.RoomCount} room(s)", _hudMetricStyle);
        }

        private static string Mark(bool done) => done ? "[✔]" : "[○]";

        private void EnsureStyles()
        {
            if (_hudHeaderStyle != null) return;

            _hudHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.25f, 0.88f, 1f) }
            };

            _hudMetricStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _hudButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            _hudHelpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
            };
        }
    }
}
