using OneRoof.Application.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    [DisallowMultipleComponent]
    public sealed class BusinessHealthOverlayPresenter : MonoBehaviour
    {
        public BusinessHealthOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public void UpdateOverlay(BusinessHealthOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }
        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(430, 20, 560, 28), "Business health: tenant solvency by floor");
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++) GUI.Label(new Rect(430, 54 + i * 26, 520, 22), CurrentOverlay.Floors[i].AccessibilityLabel);
        }
    }
}
