using OneRoof.Application.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    [DisallowMultipleComponent]
    public sealed class SatisfactionOverlayPresenter : MonoBehaviour
    {
        public SatisfactionOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public void UpdateOverlay(SatisfactionOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }
        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(500, 20, 260, 28), $"Tower satisfaction: {CurrentOverlay.TowerSatisfaction:P0}");
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++)
                GUI.Label(new Rect(500, 54 + i * 22, 300, 22), CurrentOverlay.Floors[i].AccessibilityLabel);
        }
    }
}
