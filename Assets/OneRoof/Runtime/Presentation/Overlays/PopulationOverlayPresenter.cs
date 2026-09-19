using OneRoof.Application.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    /// <summary>Renders the population overlay with labels and bars so meaning never relies on colour.</summary>
    [DisallowMultipleComponent]
    public sealed class PopulationOverlayPresenter : MonoBehaviour
    {
        public PopulationOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public event System.Action<int> FloorInspectionRequested;

        public void UpdateOverlay(PopulationOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }

        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            GUI.Box(new Rect(430, 20, 560, 28), $"Population: {CurrentOverlay.ResidentCount} residents — density and demographics by floor");
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++)
            {
                var floor = CurrentOverlay.Floors[i];
                GUI.Label(new Rect(430, 54 + i * 38, 480, 36), floor.AccessibilityLabel);
                if (GUI.Button(new Rect(915, 58 + i * 38, 70, 26), "Inspect")) FloorInspectionRequested?.Invoke(floor.Floor);
            }
        }
    }
}
