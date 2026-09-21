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
        public void UpdateOverlay(UtilitiesOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }
        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(430, 20, 560, 28), $"Utilities: {CurrentOverlay.FailedEquipmentCount} failed equipment item(s) — power, water, and waste by floor");
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++) GUI.Label(new Rect(430, 54 + i * 30, 480, 28), CurrentOverlay.Floors[i].AccessibilityLabel);
            if (GUI.Button(new Rect(905, 55, 80, 26), "Inspect")) InspectionRequested?.Invoke();
        }
    }
}
