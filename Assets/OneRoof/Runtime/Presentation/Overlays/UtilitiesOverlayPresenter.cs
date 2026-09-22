using OneRoof.Application.Overlays;
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
        public void UpdateOverlay(UtilitiesOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }
        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(430, 20, 560, 28), $"Utilities: {CurrentOverlay.FailedEquipmentCount} failed equipment item(s) — select a network to view its riser");
            if (GUI.Button(new Rect(430, 54, 105, 28), "Power")) Select(UtilitiesNetworkLayerPresenter.NetworkKind.Power);
            if (GUI.Button(new Rect(540, 54, 105, 28), "Water")) Select(UtilitiesNetworkLayerPresenter.NetworkKind.Water);
            if (GUI.Button(new Rect(650, 54, 105, 28), "Waste")) Select(UtilitiesNetworkLayerPresenter.NetworkKind.Waste);
            if (GUI.Button(new Rect(905, 54, 80, 28), "Inspect")) InspectionRequested?.Invoke();
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++) GUI.Label(new Rect(430, 88 + i * 26, 520, 24), CurrentOverlay.Floors[i].AccessibilityLabel);
        }

        private void Select(UtilitiesNetworkLayerPresenter.NetworkKind network)
        {
            SelectedNetwork = network;
            NetworkSelected?.Invoke(network);
        }
    }
}
