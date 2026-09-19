using OneRoof.Application.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    [DisallowMultipleComponent]
    public sealed class ScrutinyOverlayPresenter : MonoBehaviour
    {
        public ScrutinyOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public event System.Action InspectionRequested;

        public void UpdateOverlay(ScrutinyOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }

        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(430, 20, 560, 28), CurrentOverlay.AccessibilityLabel);
            for (var i = 0; i < CurrentOverlay.ContributingFactors.Count; i++)
                GUI.Label(new Rect(430, 54 + i * 24, 470, 22), $"• {CurrentOverlay.ContributingFactors[i]}");
            if (GUI.Button(new Rect(905, 55, 80, 26), "Inspect")) InspectionRequested?.Invoke();
        }
    }
}
