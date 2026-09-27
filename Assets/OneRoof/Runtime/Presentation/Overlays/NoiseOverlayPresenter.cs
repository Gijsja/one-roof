using OneRoof.Application.Overlays;
using OneRoof.Presentation.Tower;
using EntityId = OneRoof.Domain.Identity.EntityId;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    /// <summary>Text and glyph contour for room-scale acoustic estimates.</summary>
    [DisallowMultipleComponent]
    public sealed class NoiseOverlayPresenter : MonoBehaviour
    {
        public NoiseOverlayProjection CurrentOverlay { get; private set; }
        public bool IsVisible { get; private set; }
        public event System.Action<EntityId> RoomInspectionRequested;
        public void UpdateOverlay(NoiseOverlayProjection overlay) { CurrentOverlay = overlay; }
        public void SetVisible(bool visible) { IsVisible = visible; }

        private void OnGUI()
        {
            if (!IsVisible || CurrentOverlay == null) return;
            var camera = Camera.main;
            if (camera != null)
            {
                for (var i = 0; i < CurrentOverlay.Rooms.Count; i++)
                {
                    var room = CurrentOverlay.Rooms[i];
                    if (room.Intensity <= 0f) continue;
                    var left = -2.4f + room.MinX * .5f;
                    var right = -2.4f + (room.MaxX + 1) * .5f;
                    var y = TowerStructurePresenter.FloorY(room.Floor);
                    var lower = camera.WorldToScreenPoint(new Vector3(left, y - .65f, 0f));
                    var upper = camera.WorldToScreenPoint(new Vector3(right, y + .65f, 0f));
                    if (lower.z <= 0f || upper.z <= 0f) continue;
                    var rect = new Rect(lower.x, Screen.height - upper.y, upper.x - lower.x, upper.y - lower.y);
                    var oldColor = GUI.color;
                    GUI.color = room.Intensity >= 2.5f ? new Color(1f, .45f, .25f, .18f) : new Color(1f, .8f, .3f, .12f);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                    GUI.color = new Color(1f, .7f, .35f, .65f);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
                    GUI.color = oldColor;
                }
            }
            GUI.Box(new Rect(430, 20, 650, 50), "Noise: " + CurrentOverlay.ModelLabel + "\n" + CurrentOverlay.QuietHoursLabel);
            for (var i = 0; i < CurrentOverlay.Rooms.Count; i++)
            {
                var room = CurrentOverlay.Rooms[i];
                var y = 76 + i * 25;
                GUI.Label(new Rect(430, y, 570, 24), room.AccessibilityLabel);
                if (GUI.Button(new Rect(1005, y, 70, 24), "Inspect"))
                    RoomInspectionRequested?.Invoke(room.StrongestSourceRoomId.IsValid ? room.StrongestSourceRoomId : room.RoomId);
            }
        }
    }
}
