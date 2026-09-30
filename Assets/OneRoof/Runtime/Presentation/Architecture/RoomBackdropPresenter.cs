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

        private Material _sharedMaterial;
        private float _interiorAlpha = 1f;
        public float InteriorAlpha => _interiorAlpha;

        public void SetInteriorAlpha(float alpha, Material fadeMaterial)
        {
            _interiorAlpha = Mathf.Clamp01(alpha);
            ApplyInteriorAlpha(BackdropRenderer, fadeMaterial);
            ApplyInteriorAlpha(DoorRenderer, fadeMaterial);
            ApplyInteriorAlpha(WindowRenderer, fadeMaterial);
            ApplyInteriorAlpha(SconceRenderer, fadeMaterial);
        }

        private void ApplyInteriorAlpha(SpriteRenderer renderer, Material fadeMaterial)
        {
            if (renderer == null) return;
            var color = renderer.color; color.a = _interiorAlpha; renderer.color = color;
            renderer.enabled = _interiorAlpha > 0.001f;
            if (_sharedMaterial != null) renderer.sharedMaterial = _interiorAlpha < 1f && fadeMaterial != null ? fadeMaterial : _sharedMaterial;
        }
        private Color _baseBackdropColor = Color.white;

        public void SetHousingCondition(OneRoof.Domain.Population.HousingConditionStage condition)
        {
            if (BackdropRenderer == null) return;
            var tint = condition switch
            {
                OneRoof.Domain.Population.HousingConditionStage.Worn => new Color(1f, .91f, .78f),
                OneRoof.Domain.Population.HousingConditionStage.Degraded => new Color(.78f, .72f, .67f),
                _ => Color.white
            };
            BackdropRenderer.color = _baseBackdropColor * tint;
            var color = BackdropRenderer.color; color.a = _interiorAlpha; BackdropRenderer.color = color;
        }

        public void Setup(string roomTheme, float width, float height, float worldLeft, float worldRight, float centerY, bool isWestSide, Material sharedMaterial = null)
        {
            _sharedMaterial = sharedMaterial;
            RoomTheme = roomTheme;
            Width = width;
            Height = height;

            // 1. Setup 9-sliced Backdrop
            var backdropObj = new GameObject("Backdrop");
            backdropObj.transform.SetParent(transform, false);
            backdropObj.transform.localPosition = new Vector3(0f, 0f, 0.7f);

            BackdropRenderer = backdropObj.AddComponent<SpriteRenderer>();
            _baseBackdropColor = BackdropRenderer.color;
            BackdropRenderer.sprite = ArchitecturalFixtureCatalog.GetRoomBackdrop(roomTheme);
            BackdropRenderer.drawMode = SpriteDrawMode.Sliced;
            BackdropRenderer.size = new Vector2(Mathf.Max(0.5f, width - 0.04f), height);
            BackdropRenderer.sortingOrder = -10;
            if (_sharedMaterial != null) BackdropRenderer.sharedMaterial = _sharedMaterial;

            var lowerTheme = (roomTheme ?? "").ToLowerInvariant();
            var isResidential = lowerTheme.Contains("residential") || lowerTheme.Contains("apartment");
            var isOffice = lowerTheme.Contains("office") || lowerTheme.Contains("commercial:office");
            var isRetail = lowerTheme.Contains("retail");
            var isClinic = lowerTheme.Contains("clinic");
            var isMaintenance = lowerTheme.Contains("maintenance");
            var isSecurity = lowerTheme.Contains("security");
            var isUtilityBigRoom = lowerTheme.Contains("electrical_substation") ||
                                   lowerTheme.Contains("water_pump") ||
                                   lowerTheme.Contains("waste_collection");

            var floorBaselineY = -height * 0.5f;

            // 2. Door fixture (interior corridor side, facing elevator shaft)
            if (isResidential || isOffice)
            {
                AddDoor(ArchitecturalFixtureCatalog.ApartmentDoor, width, floorBaselineY, isWestSide, Vector3.one);
            }
            else if (isRetail || isClinic || isMaintenance || isSecurity)
            {
                AddDoor(ArchitecturalFixtureCatalog.ServiceDoor, width, floorBaselineY, isWestSide, Vector3.one);
            }
            else if (isUtilityBigRoom)
            {
                AddDoor(ArchitecturalFixtureCatalog.UtilityRollerDoor, width, floorBaselineY, isWestSide, new Vector3(2.2f, 1.9f, 1f));
            }

            // 3. Window fixtures (exterior wall side)
            if (isResidential && width >= 1.5f)
            {
                AddWindow(ArchitecturalFixtureCatalog.ResidentialWindow, width, isWestSide, Vector3.one);
            }
            else if (isRetail && width >= 2.0f)
            {
                AddWindow(ArchitecturalFixtureCatalog.StorefrontWindow, width, isWestSide, new Vector3(2f, 2f, 1f));
            }
            else if (isClinic && width >= 1.5f)
            {
                AddWindow(ArchitecturalFixtureCatalog.ResidentialWindow, width, isWestSide, Vector3.one);
            }

            // 4. Wall Sconce fixture
            if (isResidential || isOffice || isRetail || isClinic || isMaintenance || isSecurity)
            {
                var sconceObj = new GameObject("WallSconce");
                sconceObj.transform.SetParent(transform, false);
                var sconceLocalX = isWestSide ? (width * 0.5f - 0.70f) : (-width * 0.5f + 0.70f);
                sconceObj.transform.localPosition = new Vector3(sconceLocalX, 0.20f, 0.48f);

                SconceRenderer = sconceObj.AddComponent<SpriteRenderer>();
                SconceRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.WallSconce);
                SconceRenderer.sortingOrder = -4;
                if (_sharedMaterial != null) SconceRenderer.sharedMaterial = _sharedMaterial;
            }
        }

        private void AddDoor(string fixtureKey, float width, float floorBaselineY, bool isWestSide, Vector3 scale)
        {
            var doorObj = new GameObject("EntranceDoor");
            doorObj.transform.SetParent(transform, false);

            // If room is west of shaft, corridor is on east (right) edge; if east of shaft, corridor is on west (left) edge
            var doorLocalX = isWestSide ? (width * 0.5f - 0.35f) : (-width * 0.5f + 0.35f);
            doorObj.transform.localPosition = new Vector3(doorLocalX, floorBaselineY, 0.45f);
            doorObj.transform.localScale = scale;

            DoorRenderer = doorObj.AddComponent<SpriteRenderer>();
            DoorRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(fixtureKey);
            DoorRenderer.sortingOrder = -5;
            if (_sharedMaterial != null) DoorRenderer.sharedMaterial = _sharedMaterial;
        }

        private void AddWindow(string fixtureKey, float width, bool isWestSide, Vector3 scale)
        {
            var windowObj = new GameObject("Window");
            windowObj.transform.SetParent(transform, false);

            // Window sits on exterior side opposite the door
            var windowLocalX = isWestSide ? (-width * 0.5f + 0.55f) : (width * 0.5f - 0.55f);
            windowObj.transform.localPosition = new Vector3(windowLocalX, 0.08f, 0.5f);
            windowObj.transform.localScale = scale;

            WindowRenderer = windowObj.AddComponent<SpriteRenderer>();
            WindowRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(fixtureKey);
            WindowRenderer.sortingOrder = -6;
            if (_sharedMaterial != null) WindowRenderer.sharedMaterial = _sharedMaterial;
        }
    }
}
