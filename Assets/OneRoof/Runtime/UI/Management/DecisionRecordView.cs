using System;
using OneRoof.Application.Decisions;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.UI;
using UnityEngine;

namespace OneRoof.UI.Management
{
    /// <summary>Read-only decision timeline with stable-ID inspector routes.</summary>
    [DisallowMultipleComponent]
    public sealed class DecisionRecordView : MonoBehaviour
    {
        private static readonly string[] FactionNames = { "Tenant Union", "Corporate Coalition", "Merchant Guild", "Civic & Eco Council" };
        private TowerSimulationSession _simulation;
        private ModeShellSession _modes;
        private Vector2 _scroll;
        private long _selectedId;
        private long _cachedVersion = -1;
        private DecisionRecordProjection _cachedHistory;
        public event Action<int> ResidentInspectionRequested;
        public event Action<int> BusinessInspectionRequested;
        public event Action<int> FloorInspectionRequested;

        public static Rect PanelRect(int screenWidth, int screenHeight)
        {
            var width = Mathf.Min(540, screenWidth - 668);
            return width < 240 ? new Rect(0, 0, 0, 0) : new Rect(652, 112, width, Mathf.Max(300, screenHeight - 330));
        }

        public void Bind(TowerSimulationSession simulation, ModeShellSession modes)
        { _simulation = simulation; _modes = modes; _cachedVersion = -1; _cachedHistory = null; }

        private void OnGUI()
        {
            if (_simulation == null || _modes?.CurrentMode != InteractionMode.Manage) return;
            var rect = PanelRect(Screen.width, Screen.height);
            if (rect.width <= 0) return;
            if (_cachedHistory == null || _cachedVersion != _simulation.Version)
            { _cachedHistory = _simulation.DecisionHistory(); _cachedVersion = _simulation.Version; }
            var entries = _cachedHistory.Entries;
            GUILayout.BeginArea(rect, StewardTheme.Panel);
            GUILayout.Label("DECISIONS & CONSEQUENCES", StewardTheme.Label(15, StewardTheme.Text, true));
            GUILayout.Label("Observed changes follow daily settlement; they may have other causes.", StewardTheme.Label(11, StewardTheme.Muted));
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (entries.Count == 0) GUILayout.Label("No decrees or civil actions recorded yet.");
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (GUILayout.Button($"Day {entry.Day}: {entry.Title} ({entry.Phase})")) _selectedId = entry.Id;
                if (_selectedId != entry.Id) continue;
                GUILayout.Label(entry.Cause);
                if (!string.IsNullOrEmpty(entry.OldSetting)) GUILayout.Label(entry.OldSetting + " → " + entry.NewSetting);
                GUILayout.Label(entry.ImmediateEffect);
                if (entry.Observations.Count >= 2)
                {
                    var first = entry.Observations[0];
                    var last = entry.Observations[entry.Observations.Count - 1];
                    GUILayout.Label($"Observed: treasury {last.Treasury - first.Treasury:+#;-#;0}; household cash {last.HouseholdCash - first.HouseholdCash:+#;-#;0}; business cash {last.BusinessCash - first.BusinessCash:+#;-#;0}.");
                    GUILayout.Label($"Satisfaction {last.Satisfaction - first.Satisfaction:+0.00;-0.00;0}; strain {last.Strain - first.Strain:+0.00;-0.00;0}; scrutiny {last.Scrutiny - first.Scrutiny:+0.00;-0.00;0}.");
                    for (var factionIndex = 0; factionIndex < FactionNames.Length && factionIndex < first.FactionPressures.Count && factionIndex < last.FactionPressures.Count; factionIndex++)
                        GUILayout.Label($"{FactionNames[factionIndex]} pressure: {last.FactionPressures[factionIndex] - first.FactionPressures[factionIndex]:+0.00;-0.00;0}.");
                    if (entry.HouseholdIds.Count > 0 && first.HouseholdBalances.Count > 0 && last.HouseholdBalances.Count > 0)
                        GUILayout.Label(first.HouseholdBalances[0] == long.MinValue || last.HouseholdBalances[0] == long.MinValue
                            ? $"Household #{entry.HouseholdIds[0]}: historical balance unavailable after departure."
                            : $"Household #{entry.HouseholdIds[0]} cash: {last.HouseholdBalances[0] - first.HouseholdBalances[0]:+#;-#;0} since decision.");
                    if (entry.BusinessIds.Count > 0 && first.BusinessBalances.Count > 0 && last.BusinessBalances.Count > 0)
                        GUILayout.Label(first.BusinessBalances[0] == long.MinValue || last.BusinessBalances[0] == long.MinValue
                            ? $"Business #{entry.BusinessIds[0]}: historical balance unavailable after closure."
                            : $"Business #{entry.BusinessIds[0]} cash: {last.BusinessBalances[0] - first.BusinessBalances[0]:+#;-#;0} since decision.");
                }
                if (entry.ResidentIds.Count > 0 && GUILayout.Button($"Inspect resident #{entry.ResidentIds[0]}"))
                    ResidentInspectionRequested?.Invoke(entry.ResidentIds[0]);
                if (entry.BusinessIds.Count > 0 && GUILayout.Button($"Inspect business #{entry.BusinessIds[0]}"))
                    BusinessInspectionRequested?.Invoke(entry.BusinessIds[0]);
                if (entry.FloorIds.Count > 0 && GUILayout.Button($"Inspect floor {entry.FloorIds[0]}"))
                    FloorInspectionRequested?.Invoke(entry.FloorIds[0]);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
