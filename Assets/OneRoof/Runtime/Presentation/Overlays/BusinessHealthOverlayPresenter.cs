using OneRoof.Application.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    [DisallowMultipleComponent]
    public sealed class BusinessHealthOverlayPresenter : MonoBehaviour
    {
        private Vector2 _tenantScrollPosition;
        public BusinessHealthOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public void UpdateOverlay(BusinessHealthOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }
        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(430, 20, 560, 28), "Business health: tenant solvency by floor");
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++) GUI.Label(new Rect(430, 54 + i * 26, 520, 22), CurrentOverlay.Floors[i].AccessibilityLabel);
            GUI.Box(new Rect(1000, 20, 620, 28), "Tenant margins: revenue, wages, rent, tax, cash, and arrears");
            var scrollRect = new Rect(1000, 54, 620, Mathf.Max(100, Screen.height - 74));
            var contentRect = new Rect(0, 0, scrollRect.width - 24, CurrentOverlay.Tenants.Count * 26);
            _tenantScrollPosition = GUI.BeginScrollView(scrollRect, _tenantScrollPosition, contentRect);
            for (var i = 0; i < CurrentOverlay.Tenants.Count; i++)
                GUI.Label(new Rect(4, i * 26, contentRect.width - 8, 24), CurrentOverlay.Tenants[i].AccessibilityLabel);
            GUI.EndScrollView();
        }
    }
}
