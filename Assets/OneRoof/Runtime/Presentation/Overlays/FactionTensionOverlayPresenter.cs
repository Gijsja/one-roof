using System;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    /// <summary>Regional tension bands with text and pattern labels independent of colour.</summary>
    [DisallowMultipleComponent]
    public sealed class FactionTensionOverlayPresenter : MonoBehaviour
    {
        private Vector2 _scroll;
        private TowerTopologyProjection _topology;
        public FactionTensionOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public event Action<int> FloorInspectionRequested;

        public void UpdateOverlay(FactionTensionOverlayProjection overlay, TowerTopologyProjection topology = null)
        { CurrentOverlay = overlay; _topology = topology; }
        public void SetVisible(bool visible) => IsVisible = visible;

        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            var camera = Camera.main;
            if (camera != null && _topology != null)
            {
                foreach (var floor in CurrentOverlay.Floors)
                {
                    if (floor.SupporterCount == 0 || !_topology.TryGetFloorSlab(floor.Floor, out var slab)) continue;
                    var left = -2.4f + slab.MinX * .5f;
                    var right = -2.4f + (slab.MaxX + 1) * .5f;
                    var y = TowerStructurePresenter.FloorY(floor.Floor);
                    var lower = camera.WorldToScreenPoint(new Vector3(left, y - .7f, 0f));
                    var upper = camera.WorldToScreenPoint(new Vector3(right, y + .7f, 0f));
                    if (lower.z <= 0f || upper.z <= 0f) continue;
                    var rect = new Rect(lower.x, Screen.height - upper.y, upper.x - lower.x, upper.y - lower.y);
                    var oldColor = GUI.color;
                    GUI.color = floor.AveragePressure >= .7f ? new Color(1f, .35f, .3f, .18f)
                        : floor.AveragePressure >= .4f ? new Color(1f, .65f, .3f, .14f) : new Color(.5f, .9f, .75f, .1f);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                    GUI.color = new Color(1f, .8f, .5f, .65f);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
                    GUI.color = oldColor;
                }
            }
            GUI.Box(new Rect(430, 20, 650, 32), "Faction tension: regional support and grievances");
            var viewport = new Rect(430, 56, 650, Mathf.Max(100, Screen.height - 80));
            var content = new Rect(0, 0, 626, CurrentOverlay.Floors.Count * 58);
            _scroll = GUI.BeginScrollView(viewport, _scroll, content);
            for (var i = 0; i < CurrentOverlay.Floors.Count; i++)
            {
                var floor = CurrentOverlay.Floors[i];
                var y = i * 58;
                GUI.Label(new Rect(0, y, 550, 25), floor.AccessibilityLabel);
                var oldColor = GUI.color;
                GUI.color = floor.AveragePressure >= .7f ? new Color(.96f, .48f, .35f, .7f)
                    : floor.AveragePressure >= .4f ? new Color(.98f, .75f, .4f, .7f)
                    : new Color(.5f, .8f, .75f, .7f);
                GUI.Box(new Rect(4, y + 28, Mathf.Max(2f, floor.AveragePressure * 470f), 12), floor.Glyph);
                GUI.color = oldColor;
                if (floor.SupporterCount > 0 && GUI.Button(new Rect(550, y, 66, 26), "Inspect"))
                    FloorInspectionRequested?.Invoke(floor.Floor);
            }
            GUI.EndScrollView();
        }
    }
}
