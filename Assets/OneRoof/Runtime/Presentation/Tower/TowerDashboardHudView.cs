using System;
using OneRoof.Application.Modes;
using OneRoof.UI;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Compact steward status and diagnostic entry points for the live tower.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TowerDashboardHudView : MonoBehaviour
    {
        private TowerPlayableController _controller;
        public bool IsCollapsed { get; private set; }
        public TowerPlayableController Controller { get => _controller; set => _controller = value; }
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

        private void OnGUI()
        {
            if (_controller == null) _controller = GetComponent<TowerPlayableController>();
            var sim = _controller?.SimulationSession;
            if (sim == null || _controller.ModeSession == null) return;

            var panel = new Rect(16, 16, 392, IsCollapsed ? 52 : 330);
            GUILayout.BeginArea(panel, StewardTheme.Panel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("ONE ROOF", StewardTheme.Label(17, StewardTheme.Text, true));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(IsCollapsed ? "OPEN  H" : "HIDE  H", StewardTheme.Button, GUILayout.Width(82), GUILayout.Height(26))) ToggleCollapsed();
            GUILayout.EndHorizontal();
            if (IsCollapsed) { GUILayout.EndArea(); return; }

            var phase = sim.DayPhase;
            GUILayout.Label($"THE STEWARD'S TOWER  /  DAY {phase.DayNumber}  /  {phase.ClockLabel}", StewardTheme.Label(10, StewardTheme.Muted, true));
            GUILayout.Space(5);
            StewardTheme.Rule(364);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            Metric("TREASURY", $"${sim.TreasuryBalance:N0}");
            Metric("RESIDENTS", sim.ResidentCount.ToString());
            Metric("FLOORS", sim.FloorCount.ToString());
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            StewardTheme.Rule(364);

            if (_controller.IsGroundStart && sim.ResidentCount == 0)
            {
                GUILayout.Label("OPENING DAY  /  MAKE ROOM FOR LIFE", StewardTheme.Label(12, StewardTheme.Amber, true));
                GUILayout.Label("Build homes and essential services. New residents will arrive as the tower grows.", StewardTheme.Label(11, StewardTheme.Muted));
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("PLACE HOMES", StewardTheme.ActiveButton, GUILayout.Height(30)))
                    _controller.ModeSession.SelectBuildTool("residential:apartment");
                if (GUILayout.Button("VIEW UTILITIES", StewardTheme.Button, GUILayout.Height(30)))
                    _controller.ShowUtilitiesOverlay();
                GUILayout.EndHorizontal();
            }
            else
            {
                var snapshot = sim.Projection();
                var severity = (_controller.OverlayPresenter?.CurrentOverlay ?? _controller.DataOverlays?.ElevatorWait)?.OverallSeverity.ToString() ?? "Optimal";
                var queueColor = snapshot.QueueLength > 0 ? StewardTheme.Amber : StewardTheme.Mint;
                GUILayout.Label($"TRANSIT   {snapshot.QueueLength} waiting  •  {snapshot.ArrivedCount} arrived  •  {snapshot.Elevators.Count} cars", StewardTheme.Label(12, queueColor, true));
                GUILayout.Label($"Average completed wait {snapshot.AverageWaitTicks:F1} ticks   /   {severity} pressure", StewardTheme.Label(11, StewardTheme.Muted));
                if (_controller.ModeSession.CurrentMode == InteractionMode.Data)
                {
                    var flow = sim.TreasuryFlow;
                    GUILayout.Space(5);
                    StewardTheme.Rule(364);
                    GUILayout.Label($"DAILY TREASURY  /  LAST SETTLED TICK {sim.LastSettlementTick:N0}", StewardTheme.Label(10, StewardTheme.Muted, true));
                    GUILayout.Label($"Rent +${flow.Rent:N0}   Tax +${flow.Tax:N0}   Net ${flow.Net:+#,0;-#,0;0}", StewardTheme.Label(11, StewardTheme.Text, true));
                    GUILayout.Label($"Upkeep -${flow.Upkeep:N0}   Subsidy -${flow.Subsidy:N0}   Construction -${flow.Construction:N0}   Salvage +${flow.ConstructionSalvage:N0}", StewardTheme.Label(10, StewardTheme.Muted));
                }
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("INSPECT CAUSE", StewardTheme.ActiveButton, GUILayout.Height(30))) _controller.InspectBottleneck();
                if (GUILayout.Button("SHOW FLOW", StewardTheme.Button, GUILayout.Height(30))) _controller.ToggleDataOverlay();
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(_controller.IsPaused ? "● PAUSED  /  SPACE TO RESUME" : "● LIVE  /  SPACE TO PAUSE", StewardTheme.Label(10, _controller.IsPaused ? StewardTheme.Amber : StewardTheme.Mint, true));
            GUILayout.EndArea();
        }

        private static void Metric(string label, string value)
        {
            GUILayout.BeginVertical(GUILayout.Width(116));
            GUILayout.Label(label, StewardTheme.Label(10, StewardTheme.Muted, true));
            GUILayout.Label(value, StewardTheme.Label(17, StewardTheme.Text, true));
            GUILayout.EndVertical();
        }
    }
}
