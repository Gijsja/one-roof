using OneRoof.Application.Overlays;
using OneRoof.Domain.Infrastructure;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    [DisallowMultipleComponent]
    public sealed class UtilitiesOverlayPresenter : MonoBehaviour
    {
        public UtilitiesOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public event System.Action InspectionRequested;
        public event System.Action<UtilitiesNetworkLayerPresenter.NetworkKind> NetworkSelected;
        public UtilitiesNetworkLayerPresenter.NetworkKind SelectedNetwork { get; private set; }
        private Vector2 _floorScroll;
        public void UpdateOverlay(UtilitiesOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }
        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            var width = Mathf.Min(360f, Screen.width - 32f);
            var height = Mathf.Min(250f, Screen.height * 0.36f);
            var panelY = Mathf.Min(185f, Mathf.Max(12f, Screen.height - height - 20f));
            var panel = new Rect(Mathf.Max(16f, Screen.width - width - 20f), panelY, width, height);
            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Label($"UTILITIES  ·  {CurrentOverlay.FailedEquipmentCount} equipment failed");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Power", GUILayout.Height(28))) Select(UtilitiesNetworkLayerPresenter.NetworkKind.Power);
            if (GUILayout.Button("Water", GUILayout.Height(28))) Select(UtilitiesNetworkLayerPresenter.NetworkKind.Water);
            if (GUILayout.Button("Waste", GUILayout.Height(28))) Select(UtilitiesNetworkLayerPresenter.NetworkKind.Waste);
            if (GUILayout.Button("Inspect", GUILayout.Height(28))) InspectionRequested?.Invoke();
            GUILayout.EndHorizontal();
            GUILayout.Label($"{SelectedNetwork} by floor. Hover a row for the full cause.");
            _floorScroll = GUILayout.BeginScrollView(_floorScroll, GUILayout.Height(height - 122f));
            var undergroundSummary = FormatUndergroundSummary(CurrentOverlay, SelectedNetwork);
            if (!string.IsNullOrEmpty(undergroundSummary)) GUILayout.Label(undergroundSummary);
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++)
            {
                var floor = CurrentOverlay.Floors[i];
                GUILayout.Label(new GUIContent(FormatFloorSummary(floor, SelectedNetwork), floor.AccessibilityLabel),
                    GUILayout.Height(22f));
            }
            GUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var detailStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 10 };
                GUI.Label(new Rect(8f, height - 55f, width - 16f, 52f), GUI.tooltip, detailStyle);
            }
            GUILayout.EndArea();
        }

        public static string FormatFloorSummary(UtilitiesFloorProjection floor, UtilitiesNetworkLayerPresenter.NetworkKind network)
        {
            switch (network)
            {
                case UtilitiesNetworkLayerPresenter.NetworkKind.Water:
                    return $"Floor {floor.Floor}  ·  Water {Mathf.RoundToInt(floor.WaterPressure * 100f)}%  ·  {(floor.WaterCause == "None" ? "OK" : "Check")}";
                case UtilitiesNetworkLayerPresenter.NetworkKind.Waste:
                    return $"Floor {floor.Floor}  ·  Waste {(floor.WasteCause == "None" ? "OK" : "Check")}";
                default:
                    return $"Floor {floor.Floor}  ·  Power {Mathf.RoundToInt(floor.Voltage * 100f)}%  ·  {(floor.ElectricalCause == "None" ? "OK" : "Check")}";
            }
        }

        public static string FormatUndergroundSummary(UtilitiesOverlayProjection overlay,
            UtilitiesNetworkLayerPresenter.NetworkKind network)
        {
            if (network == UtilitiesNetworkLayerPresenter.NetworkKind.Waste || overlay?.UndergroundPaths == null)
                return string.Empty;
            var kind = network == UtilitiesNetworkLayerPresenter.NetworkKind.Power
                ? UndergroundUtilityKind.Power : UndergroundUtilityKind.Water;
            var total = 0;
            var connected = 0;
            var flowing = 0;
            foreach (var room in overlay.UndergroundPaths.RoomStatuses)
            {
                if (room.Kind != kind) continue;
                total++;
                if (room.IsConnected) connected++;
                if (room.IsFlowing) flowing++;
            }
            if (total == 0) return "Undercity · No rooms";
            if (connected == 0) return $"Undercity · No connected path to {total} rooms";
            return $"Undercity · {flowing}/{total} rooms flowing ({connected} connected)";
        }

        private void Select(UtilitiesNetworkLayerPresenter.NetworkKind network)
        {
            SelectedNetwork = network;
            NetworkSelected?.Invoke(network);
        }
    }
}
