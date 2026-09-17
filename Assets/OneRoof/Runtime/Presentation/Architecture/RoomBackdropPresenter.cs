using UnityEngine;

namespace OneRoof.Presentation.Architecture
{
    /// <summary>
    /// Presentation component managing 9-sliced room backdrops and architectural fixtures
    /// (doors, windows, trim) for an individual room cutaway.
    /// </summary>
    public class RoomBackdropPresenter : MonoBehaviour
    {
        public SpriteRenderer BackdropRenderer { get; private set; }
        public SpriteRenderer DoorRenderer { get; private set; }
        public SpriteRenderer WindowRenderer { get; private set; }
        public SpriteRenderer SconceRenderer { get; private set; }

        public string RoomTheme { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }

        public void Setup(string roomTheme, float width, float height, float worldLeft, float worldRight, float centerY, bool isWestSide)
        {
            RoomTheme = roomTheme;
            Width = width;
            Height = height;

            // 1. Setup 9-sliced Backdrop
            var backdropObj = new GameObject("Backdrop");
            backdropObj.transform.SetParent(transform, false);
            backdropObj.transform.localPosition = new Vector3(0f, 0f, 0.7f);

            BackdropRenderer = backdropObj.AddComponent<SpriteRenderer>();
            BackdropRenderer.sprite = ArchitecturalFixtureCatalog.GetRoomBackdrop(roomTheme);
            BackdropRenderer.drawMode = SpriteDrawMode.Sliced;
            BackdropRenderer.size = new Vector2(Mathf.Max(0.5f, width - 0.04f), height);
            BackdropRenderer.sortingOrder = -10;

            var lowerTheme = (roomTheme ?? "").ToLowerInvariant();
            var isResidential = lowerTheme.Contains("residential") || lowerTheme.Contains("apartment");
            var isOffice = lowerTheme.Contains("office") || lowerTheme.Contains("commercial:office");

            var floorBaselineY = -height * 0.5f;

            // 2. Door fixture (interior corridor side, facing elevator shaft)
            if (isResidential || isOffice)
            {
                var doorObj = new GameObject("EntranceDoor");
                doorObj.transform.SetParent(transform, false);

                // If room is west of shaft, corridor is on east (right) edge; if east of shaft, corridor is on west (left) edge
                var doorLocalX = isWestSide ? (width * 0.5f - 0.35f) : (-width * 0.5f + 0.35f);
                doorObj.transform.localPosition = new Vector3(doorLocalX, floorBaselineY, 0.45f);

                DoorRenderer = doorObj.AddComponent<SpriteRenderer>();
                DoorRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.ApartmentDoor);
                DoorRenderer.sortingOrder = -5;
            }

            // 3. Window fixture (exterior wall side)
            if (isResidential && width >= 1.5f)
            {
                var windowObj = new GameObject("Window");
                windowObj.transform.SetParent(transform, false);

                // Window sits on exterior side opposite the door
                var windowLocalX = isWestSide ? (-width * 0.5f + 0.55f) : (width * 0.5f - 0.55f);
                windowObj.transform.localPosition = new Vector3(windowLocalX, 0.08f, 0.5f);

                WindowRenderer = windowObj.AddComponent<SpriteRenderer>();
                WindowRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.ResidentialWindow);
                WindowRenderer.sortingOrder = -6;
            }

            // 4. Wall Sconce fixture
            if (isResidential || isOffice)
            {
                var sconceObj = new GameObject("WallSconce");
                sconceObj.transform.SetParent(transform, false);
                var sconceLocalX = isWestSide ? (width * 0.5f - 0.70f) : (-width * 0.5f + 0.70f);
                sconceObj.transform.localPosition = new Vector3(sconceLocalX, 0.20f, 0.48f);

                SconceRenderer = sconceObj.AddComponent<SpriteRenderer>();
                SconceRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.WallSconce);
                SconceRenderer.sortingOrder = -4;
            }
        }
    }
}
