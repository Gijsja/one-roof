using OneRoof.Application.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    [DisallowMultipleComponent]
    public sealed class FootTrafficOverlayPresenter : MonoBehaviour
    {
        public FootTrafficOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public void UpdateOverlay(FootTrafficOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }
        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(430, 20, 560, 28), "Foot traffic: residents in transit by floor");
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++) GUI.Label(new Rect(430, 54 + i * 26, 520, 22), CurrentOverlay.Floors[i].AccessibilityLabel);
        }
    }
}
