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
        public SpriteRenderer BathroomDoorRenderer { get; private set; }
        public SpriteRenderer WindowRenderer { get; private set; }
        public SpriteRenderer WindowSecondaryRenderer { get; private set; }
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
            ApplyInteriorAlpha(BathroomDoorRenderer, fadeMaterial);
            ApplyInteriorAlpha(WindowRenderer, fadeMaterial);
            ApplyInteriorAlpha(WindowSecondaryRenderer, fadeMaterial);
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

        public void SetBackdropDrawMode(SpriteDrawMode mode)
        {
            if (BackdropRenderer != null)
            {
                BackdropRenderer.drawMode = mode;
            }
        }

        public void SetWidth(float newWidth)
        {
            Width = newWidth;
            if (BackdropRenderer != null)
            {
                BackdropRenderer.size = new Vector2(Mathf.Max(0.5f, newWidth - 0.04f), Height);
            }
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

            if (isResidential)
            {
                // Harmonious palette tints so adjoining apartments don't look like an identical cloned grid
                var paletteIndex = Mathf.Abs((int)(worldLeft * 3.7f + centerY * 5.3f)) % 6;
                _baseBackdropColor = paletteIndex switch
                {
                    0 => new Color(0.96f, 0.89f, 0.82f), // Warm Terracotta Peach
                    1 => new Color(0.86f, 0.94f, 0.89f), // Soft Sage Leaf
                    2 => new Color(0.97f, 0.95f, 0.87f), // Honey Amber Warmth
                    3 => new Color(0.89f, 0.92f, 0.98f), // Slate Blue Grey
                    4 => new Color(0.95f, 0.89f, 0.93f), // Dusty Rose Linen
                    _ => new Color(0.97f, 0.96f, 0.93f)  // Neutral Cream Canvas
                };
            }
            else
            {
                _baseBackdropColor = Color.white;
            }

            BackdropRenderer.color = _baseBackdropColor;
            BackdropRenderer.sprite = ArchitecturalFixtureCatalog.GetRoomBackdrop(roomTheme);
            BackdropRenderer.drawMode = SpriteDrawMode.Sliced;
            BackdropRenderer.size = new Vector2(Mathf.Max(0.5f, width - 0.04f), height);
            BackdropRenderer.sortingOrder = -10;
            if (_sharedMaterial != null) BackdropRenderer.sharedMaterial = _sharedMaterial;

            var floorBaselineY = -height * 0.5f;

            // 2. Door fixtures
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

            // Loose interior bathroom door for residential units
            if (isResidential)
            {
                AddBathroomDoor(width, floorBaselineY, isWestSide);
            }

            // 3. Window fixtures (exterior wall side)
            if (isResidential && width >= 1.4f)
            {
                var winScale = width < 2.0f ? new Vector3(0.85f, 0.85f, 1f) : Vector3.one;
                AddWindow(ArchitecturalFixtureCatalog.ResidentialWindow, width, isWestSide, winScale);

                // Grand 3x3 suites (width >= 4.0m) get a secondary window for the living lounge
                if (width >= 4.0f)
                {
                    AddSecondaryWindow(ArchitecturalFixtureCatalog.ResidentialWindow, width, isWestSide, Vector3.one);
                }
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

        private void AddBathroomDoor(float width, float floorBaselineY, bool isWestSide)
        {
            var doorObj = new GameObject("BathroomDoor");
            doorObj.transform.SetParent(transform, false);

            // Position bathroom door on back wall inside the room
            // In 1x1: placed between sleeping and corridor entry
            // In 2x2: placed comfortably between living zone and corridor
            // In 3x3: placed near corridor/service zone
            var bathLocalX = width < 2.0f
                ? (isWestSide ? (width * 0.5f - 0.72f) : (-width * 0.5f + 0.72f))
                : (width < 4.0f
                    ? (isWestSide ? (width * 0.5f - 0.95f) : (-width * 0.5f + 0.95f))
                    : (isWestSide ? (width * 0.5f - 1.25f) : (-width * 0.5f + 1.25f)));

            doorObj.transform.localPosition = new Vector3(bathLocalX, floorBaselineY, 0.46f);
            doorObj.transform.localScale = new Vector3(0.88f, 0.92f, 1f);

            BathroomDoorRenderer = doorObj.AddComponent<SpriteRenderer>();
            BathroomDoorRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.ApartmentDoor);
            BathroomDoorRenderer.color = new Color(0.82f, 0.80f, 0.76f, _interiorAlpha);
            BathroomDoorRenderer.sortingOrder = -5;
            if (_sharedMaterial != null) BathroomDoorRenderer.sharedMaterial = _sharedMaterial;
        }

        private void AddWindow(string fixtureKey, float width, bool isWestSide, Vector3 scale)
        {
            var windowObj = new GameObject("Window");
            windowObj.transform.SetParent(transform, false);

            // Window sits on exterior side opposite the door
            var offset = width < 2.0f ? 0.45f : 0.60f;
            var windowLocalX = isWestSide ? (-width * 0.5f + offset) : (width * 0.5f - offset);
            windowObj.transform.localPosition = new Vector3(windowLocalX, 0.12f, 0.5f);
            windowObj.transform.localScale = scale;

            WindowRenderer = windowObj.AddComponent<SpriteRenderer>();
            WindowRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(fixtureKey);
            WindowRenderer.sortingOrder = -6;
            if (_sharedMaterial != null) WindowRenderer.sharedMaterial = _sharedMaterial;
        }

        private void AddSecondaryWindow(string fixtureKey, float width, bool isWestSide, Vector3 scale)
        {
            var windowObj = new GameObject("WindowSecondary");
            windowObj.transform.SetParent(transform, false);

            var windowLocalX = isWestSide ? (-width * 0.5f + 2.0f) : (width * 0.5f - 2.0f);
            windowObj.transform.localPosition = new Vector3(windowLocalX, 0.12f, 0.5f);
            windowObj.transform.localScale = scale;

            WindowSecondaryRenderer = windowObj.AddComponent<SpriteRenderer>();
            WindowSecondaryRenderer.sprite = ArchitecturalFixtureCatalog.GetFixture(fixtureKey);
            WindowSecondaryRenderer.sortingOrder = -6;
            if (_sharedMaterial != null) WindowSecondaryRenderer.sharedMaterial = _sharedMaterial;
        }
    }
}
