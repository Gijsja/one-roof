using System.Collections.Generic;
using OneRoof.Content;
using OneRoof.Domain.Topology;
using UnityEngine;

namespace OneRoof.Presentation.Furnishings
{
    /// <summary>
    /// Presentation component managing interior furniture and prop furnishings for a room.
    /// Places themed props anchored to the floorboard baseline with correct sorting and interaction anchors.
    /// </summary>
    public class RoomFurnishingPresenter : MonoBehaviour
    {
        private static readonly string[] SleepMatches = { "bed" };
        private static readonly string[] WorkMatches = { "desk", "bench", "monitor", "vanity" };
        private static readonly string[] SeatMatches = { "sofa", "booth", "chair", "conversation", "gametable", "table" };
        private static readonly string[] CookMatches = { "kitchenette" };
        private static readonly string[] BrowseMatches = { "coatrack", "shelf", "rack", "bookcase" };
        private static readonly string[] ServiceMatches = { "counter", "reception", "coatrack", "checkout" };
        private static readonly string[] RestMatches = { "sofa", "table", "coatrack" };
        private static readonly string[] DefaultMatches = { "booth", "table", "coatrack", "kitchenette", "desk" };

        private readonly List<GameObject> _placedProps = new List<GameObject>();
        private readonly List<SpriteRenderer> _placedRenderers = new List<SpriteRenderer>();
        private readonly List<string> _currentPropIds = new List<string>();
        private Material _sharedMaterial;

        private float _clutterAlpha = -1f;
        public float ClutterAlpha => Mathf.Max(0f, _clutterAlpha);
        public int PlacedCount => _placedProps.Count;

        public void SetClutterAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (Mathf.Approximately(_clutterAlpha, alpha)) return;
            _clutterAlpha = alpha;
            for (var i = 0; i < _placedRenderers.Count; i++)
            {
                var renderer = _placedRenderers[i];
                if (renderer == null) continue;
                var color = renderer.color;
                color.a = alpha;
                renderer.color = color;
                renderer.enabled = alpha > 0.001f;
            }
        }

        public IReadOnlyList<GameObject> PlacedProps => _placedProps;

        public bool TryGetDockPosition(InteractionPointKind kind, int slot, out Vector3 worldPosition)
        {
            var matches = kind switch
            {
                InteractionPointKind.Sleep => SleepMatches,
                InteractionPointKind.Work => WorkMatches,
                InteractionPointKind.Seat => SeatMatches,
                InteractionPointKind.Cook => CookMatches,
                InteractionPointKind.Browse => BrowseMatches,
                InteractionPointKind.Service => ServiceMatches,
                InteractionPointKind.Rest => RestMatches,
                _ => DefaultMatches
            };
            var matchCount = 0;
            for (var i = 0; i < _placedProps.Count; i++)
            {
                var prop = _placedProps[i];
                if (prop != null && NameContainsAny(prop.name, matches))
                {
                    matchCount++;
                }
            }
            if (matchCount > 0)
            {
                var s = Mathf.Abs(slot);
                var selected = matchCount > 1 && kind == InteractionPointKind.Sleep
                    ? (s == 2 ? 0 : (s % matchCount))
                    : (s % matchCount);

                for (var i = 0; i < _placedProps.Count; i++)
                {
                    var prop = _placedProps[i];
                    if (prop != null && NameContainsAny(prop.name, matches) && selected-- == 0)
                    {
                        float lateralOffset;
                        if (kind == InteractionPointKind.Sleep)
                        {
                            if (matchCount > 1)
                            {
                                lateralOffset = s switch
                                {
                                    0 => -0.16f,
                                    1 => 0f,
                                    2 => 0.16f,
                                    _ => (s % 2 == 0) ? -0.16f : 0.16f
                                };
                            }
                            else
                            {
                                lateralOffset = (s % 2 == 0) ? -0.16f : 0.16f;
                            }
                        }
                        else if (kind == InteractionPointKind.Seat)
                        {
                            lateralOffset = (s % 2 == 0) ? -0.16f : 0.16f;
                        }
                        else
                        {
                            lateralOffset = 0f;
                        }

                        worldPosition = prop.transform.position + new Vector3(lateralOffset, 0.06f, -0.2f);
                        return true;
                    }
                }
            }
            worldPosition = default;
            return false;
        }

        private static bool NameContainsAny(string name, string[] matches)
        {
            var lower = name.ToLowerInvariant();
            for (var i = 0; i < matches.Length; i++)
            {
                if (lower.Contains(matches[i])) return true;
            }
            return false;
        }

        public void FurnishRoom(string roomTheme, float width, float height, bool isWestSide, Material sharedMaterial = null)
        {
            _sharedMaterial = sharedMaterial;
            ClearProps();

            var lowerTheme = (roomTheme ?? "").ToLowerInvariant();
            var floorBaselineY = -height * 0.5f;

            if (lowerTheme.Contains("residential") || lowerTheme.Contains("apartment"))
            {
                FurnishResidential(width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("office") || lowerTheme.Contains("commercial:office"))
            {
                FurnishOffice(width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("diner") || lowerTheme.Contains("restaurant"))
            {
                FurnishDiner(width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("retail"))
            {
                FurnishRetail(width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("clinic"))
            {
                FurnishClinic(width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("maintenance"))
            {
                FurnishMaintenance(width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("security"))
            {
                FurnishSecurity(width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("utility"))
            {
                FurnishUtility(roomTheme, width, floorBaselineY, isWestSide);
            }
            else if (lowerTheme.Contains("lobby"))
            {
                FurnishLobby(width, floorBaselineY, isWestSide);
            }
        }

        private void FurnishResidential(float width, float baselineY, bool isWestSide)
        {
            if (width < 2.0f)
            {
                // 1x1 Micro Pod (1.5m wide): 1 resident
                var bedX = isWestSide ? (-width * 0.5f + 0.55f) : (width * 0.5f - 0.55f);
                SpawnProp("prop.furniture.bed.v1", new Vector3(bedX, baselineY, 0.40f));

                var planterX = isWestSide ? (-width * 0.5f + 0.16f) : (width * 0.5f - 0.16f);
                SpawnProp("prop.decor.planter.v1", new Vector3(planterX, baselineY, 0.42f));
            }
            else if (width < 4.0f)
            {
                // 2x2 Studio Apartment (3.0m wide): 2 residents
                var bedX = isWestSide ? (-width * 0.5f + 0.65f) : (width * 0.5f - 0.65f);
                SpawnProp("prop.furniture.bed.v1", new Vector3(bedX, baselineY, 0.40f));

                var planterX = isWestSide ? (-width * 0.5f + 0.20f) : (width * 0.5f - 0.20f);
                SpawnProp("prop.decor.planter.v1", new Vector3(planterX, baselineY, 0.42f));

                var sofaX = isWestSide ? (width * 0.5f - 0.95f) : (-width * 0.5f + 0.95f);
                SpawnProp("prop.furniture.sofa.v1", new Vector3(sofaX, baselineY, 0.35f));

                if (width >= 2.5f)
                {
                    var bookcaseX = isWestSide ? 0.42f : -0.42f;
                    SpawnProp("prop.furniture.bookcase.v1", new Vector3(bookcaseX, baselineY, 0.46f));
                }

                if (width >= 3.0f)
                {
                    var extraPropId = isWestSide ? "prop.furniture.conversationset.v1" : "prop.furniture.vanitydresser.v1";
                    var extraX = isWestSide ? (width * 0.5f - 1.45f) : (-width * 0.5f + 1.45f);
                    SpawnProp(extraPropId, new Vector3(extraX, baselineY, 0.38f));
                }
            }
            else
            {
                // 3x3 Grand Suite (4.5m wide): 3 residents
                // Master double bed (residents 0 & 2)
                var masterBedX = isWestSide ? (-width * 0.5f + 0.75f) : (width * 0.5f - 0.75f);
                SpawnProp("prop.furniture.bed.v1", new Vector3(masterBedX, baselineY, 0.40f));

                // Secondary twin bed (resident 1)
                var secondBedX = isWestSide ? (-width * 0.5f + 1.95f) : (width * 0.5f - 1.95f);
                SpawnProp("prop.furniture.bed.v1", new Vector3(secondBedX, baselineY, 0.40f));

                var planterX = isWestSide ? (-width * 0.5f + 0.18f) : (width * 0.5f - 0.18f);
                SpawnProp("prop.decor.planter.v1", new Vector3(planterX, baselineY, 0.42f));

                // Living lounge area
                var sofaX = isWestSide ? (width * 0.5f - 1.25f) : (-width * 0.5f + 1.25f);
                SpawnProp("prop.furniture.sofa.v1", new Vector3(sofaX, baselineY, 0.35f));

                var tableX = isWestSide ? (width * 0.5f - 1.25f) : (-width * 0.5f + 1.25f);
                SpawnProp("prop.furniture.table.v1", new Vector3(tableX, baselineY, 0.28f));

                var bookcaseX = isWestSide ? 0.35f : -0.35f;
                SpawnProp("prop.furniture.bookcase.v1", new Vector3(bookcaseX, baselineY, 0.46f));

                var extraPropId = isWestSide ? "prop.furniture.conversationset.v1" : "prop.furniture.vanitydresser.v1";
                var extraX = isWestSide ? (width * 0.5f - 1.85f) : (-width * 0.5f + 1.85f);
                SpawnProp(extraPropId, new Vector3(extraX, baselineY, 0.38f));
            }
        }

        private void FurnishOffice(float width, float baselineY, bool isWestSide)
        {
            // Left workstation desk and chair
            var deskLeftX = -width * 0.25f;
            SpawnProp("prop.workplace.desk.v1", new Vector3(deskLeftX, baselineY, 0.40f));
            SpawnProp("prop.workplace.chair.v1", new Vector3(deskLeftX - 0.15f, baselineY, 0.35f));

            // Right workstation desk and chair
            var deskRightX = width * 0.25f;
            SpawnProp("prop.workplace.desk.v1", new Vector3(deskRightX, baselineY, 0.40f));
            SpawnProp("prop.workplace.chair.v1", new Vector3(deskRightX + 0.15f, baselineY, 0.35f));

            // Filing cabinet near wall
            var fileX = isWestSide ? (-width * 0.5f + 0.25f) : (width * 0.5f - 0.25f);
            SpawnProp("prop.workplace.filing.v1", new Vector3(fileX, baselineY, 0.42f));
        }

        private void FurnishDiner(float width, float baselineY, bool isWestSide)
        {
            // Service counter
            var counterX = isWestSide ? 0.35f : -0.35f;
            SpawnProp("prop.commercial.counter.v1", new Vector3(counterX, baselineY, 0.38f));

            // Diner booth on outer side
            var boothX = isWestSide ? (-width * 0.5f + 0.60f) : (width * 0.5f - 0.60f);
            SpawnProp("prop.commercial.booth.v1", new Vector3(boothX, baselineY, 0.35f));

            // Game table in wide dining areas
            if (width >= 2.5f)
            {
                var tableX = isWestSide ? (width * 0.5f - 0.65f) : (-width * 0.5f + 0.65f);
                SpawnProp("prop.furniture.gametable.v1", new Vector3(tableX, baselineY, 0.36f));
            }
        }

        private void FurnishLobby(float width, float baselineY, bool isWestSide)
        {
            // Reception desk
            SpawnProp("prop.civic.reception.v1", new Vector3(0f, baselineY, 0.38f));

            // Coat rack
            var coatX = isWestSide ? (-width * 0.5f + 0.35f) : (width * 0.5f - 0.35f);
            SpawnProp("prop.civic.coatrack.v1", new Vector3(coatX, baselineY, 0.42f));

            // Monstera planter
            var plantX = isWestSide ? (width * 0.5f - 0.35f) : (-width * 0.5f + 0.35f);
            SpawnProp("prop.decor.planter.v1", new Vector3(plantX, baselineY, 0.42f));
        }

        private void FurnishRetail(float width, float baselineY, bool isWestSide)
        {
            // Display shelf on exterior side
            var shelfX = isWestSide ? (-width * 0.5f + 0.60f) : (width * 0.5f - 0.60f);
            SpawnProp("prop.commercial.shelf.v1", new Vector3(shelfX, baselineY, 0.35f));

            // Checkout counter near corridor
            var counterX = isWestSide ? (width * 0.5f - 0.85f) : (-width * 0.5f + 0.85f);
            SpawnProp("prop.commercial.checkout.v1", new Vector3(counterX, baselineY, 0.38f));

            // Garment rack in wider shops
            if (width >= 2.5f)
            {
                SpawnProp("prop.commercial.rack.v1", new Vector3(0f, baselineY, 0.42f));
            }
        }

        private void FurnishClinic(float width, float baselineY, bool isWestSide)
        {
            // Exam bed on exterior side
            var bedX = isWestSide ? (-width * 0.5f + 0.65f) : (width * 0.5f - 0.65f);
            SpawnProp("prop.service.exambed.v1", new Vector3(bedX, baselineY, 0.40f));

            // Supply cabinet near corridor
            var cabinetX = isWestSide ? (width * 0.5f - 0.35f) : (-width * 0.5f + 0.35f);
            SpawnProp("prop.service.pharmacabinet.v1", new Vector3(cabinetX, baselineY, 0.42f));

            // Privacy screen in wider clinics
            if (width >= 2.5f)
            {
                SpawnProp("prop.service.screen.v1", new Vector3(0f, baselineY, 0.45f));
            }
        }

        private void FurnishMaintenance(float width, float baselineY, bool isWestSide)
        {
            // Central workbench
            SpawnProp("prop.service.workbench.v1", new Vector3(0f, baselineY, 0.38f));

            // Tool cabinet against the wall
            var cabinetX = isWestSide ? (-width * 0.5f + 0.25f) : (width * 0.5f - 0.25f);
            SpawnProp("prop.service.toolcabinet.v1", new Vector3(cabinetX, baselineY, 0.42f));

            // Parts shelf in wider workshops
            if (width >= 3.0f)
            {
                var shelfX = isWestSide ? (width * 0.5f - 0.55f) : (-width * 0.5f + 0.55f);
                SpawnProp("prop.service.partsshelf.v1", new Vector3(shelfX, baselineY, 0.40f));
            }
        }

        private void FurnishSecurity(float width, float baselineY, bool isWestSide)
        {
            // Monitor desk in the middle
            SpawnProp("prop.service.securitydesk.v1", new Vector3(0f, baselineY, 0.40f));

            // Locker row against the wall
            var lockerX = isWestSide ? (-width * 0.5f + 0.55f) : (width * 0.5f - 0.55f);
            SpawnProp("prop.service.lockerrow.v1", new Vector3(lockerX, baselineY, 0.42f));
        }

        private void FurnishUtility(string roomTheme, float width, float baselineY, bool isWestSide)
        {
            var lower = (roomTheme ?? "").ToLowerInvariant();
            if (lower.Contains("electrical_substation") || lower.Contains("transformer"))
            {
                SpawnProp("prop.utility.substation.v1", new Vector3(0f, baselineY, 0.38f));
            }
            else if (lower.Contains("water_pump") || lower.Contains("water_booster"))
            {
                SpawnProp("prop.utility.pump.v1", new Vector3(0f, baselineY, 0.38f));
            }
            else if (lower.Contains("waste_collection"))
            {
                SpawnProp("prop.utility.wastehopper.v1", new Vector3(0f, baselineY, 0.38f));
            }
            else
            {
                // Risers and chutes are narrow vertical runs: dress with a pipe chase
                SpawnProp("prop.utility.pipechase.v1", new Vector3(0f, baselineY, 0.42f));
            }

            // Larger plant rooms get a pipe chase on the corridor edge; keep a clear walk lane
            if (width >= 1.5f && (lower.Contains("substation") || lower.Contains("water_pump") || lower.Contains("waste_collection")))
            {
                var chaseX = isWestSide ? (width * 0.5f - 0.25f) : (-width * 0.5f + 0.25f);
                SpawnProp("prop.utility.pipechase.v1", new Vector3(chaseX, baselineY, 0.42f));
            }
        }

        public bool NeedsFurnishingSync(IReadOnlyList<string> newPropIds)
        {
            if (newPropIds == null || newPropIds.Count == 0)
                return _currentPropIds.Count != 0;
            if (newPropIds.Count != _currentPropIds.Count)
                return true;
            for (var i = 0; i < newPropIds.Count; i++)
            {
                if (!string.Equals(newPropIds[i], _currentPropIds[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public void SyncFurnishings(IReadOnlyList<string> propContentIds, float width, float height, bool isWestSide, Material sharedMaterial = null)
        {
            if (sharedMaterial != null) _sharedMaterial = sharedMaterial;
            if (!NeedsFurnishingSync(propContentIds)) return;

            ClearProps();
            if (propContentIds == null || propContentIds.Count == 0) return;

            var baselineY = -height * 0.5f;
            for (var i = 0; i < propContentIds.Count; i++)
            {
                var id = propContentIds[i];
                if (string.Equals(id, "prop.civic.coatrack.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var coatX = isWestSide ? (width * 0.5f - 0.25f) : (-width * 0.5f + 0.25f);
                    SpawnProp(id, new Vector3(coatX, baselineY, 0.42f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.furniture.bed.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var bedX = isWestSide ? (-width * 0.5f + 0.65f) : (width * 0.5f - 0.65f);
                    SpawnProp(id, new Vector3(bedX, baselineY, 0.40f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.decor.planter.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var planterX = isWestSide ? (-width * 0.5f + 0.20f) : (width * 0.5f - 0.20f);
                    SpawnProp(id, new Vector3(planterX, baselineY, 0.42f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.furniture.sofa.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var sofaX = isWestSide ? (width * 0.5f - 0.95f) : (-width * 0.5f + 0.95f);
                    SpawnProp(id, new Vector3(sofaX, baselineY, 0.35f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.furniture.table.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var tableX = isWestSide ? (width * 0.5f - 0.95f) : (-width * 0.5f + 0.95f);
                    SpawnProp(id, new Vector3(tableX, baselineY, 0.28f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.workplace.desk.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var deskX = isWestSide ? -0.05f : 0.05f;
                    SpawnProp(id, new Vector3(deskX, baselineY, 0.42f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.furniture.bookcase.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var bookcaseX = isWestSide ? 0.42f : -0.42f;
                    SpawnProp(id, new Vector3(bookcaseX, baselineY, 0.46f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.domestic.kitchenette.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var kitchenX = isWestSide ? -0.15f : 0.15f;
                    SpawnProp(id, new Vector3(kitchenX, baselineY, 0.44f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.furniture.gametable.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var gameTableX = isWestSide ? 0.15f : -0.15f;
                    SpawnProp(id, new Vector3(gameTableX, baselineY, 0.36f));
                    _currentPropIds.Add(id);
                }
                else if (string.Equals(id, "prop.furniture.conversationset.v1", System.StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(id, "prop.furniture.vanitydresser.v1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var extraX = isWestSide ? (width * 0.5f - 1.45f) : (-width * 0.5f + 1.45f);
                    SpawnProp(id, new Vector3(extraX, baselineY, 0.38f));
                    _currentPropIds.Add(id);
                }
                else
                {
                    SpawnProp(id, new Vector3(0f, baselineY, 0.38f));
                    _currentPropIds.Add(id);
                }
            }
        }

        public GameObject SpawnProp(string contentId, Vector3 localPosition)
        {
            var propObj = new GameObject("Prop_" + contentId);
            propObj.transform.SetParent(transform, false);
            propObj.transform.localPosition = localPosition;

            var sr = propObj.AddComponent<SpriteRenderer>();
            sr.sprite = PropCatalog.GetPropSprite(contentId);
            sr.sortingOrder = -2;
            sr.sharedMaterial = _sharedMaterial ?? GetOrCreateDefaultPropMaterial();
            if (_clutterAlpha >= 0f)
            {
                var color = sr.color;
                color.a = _clutterAlpha;
                sr.color = color;
                sr.enabled = _clutterAlpha > 0.001f;
            }

            _placedProps.Add(propObj);
            _placedRenderers.Add(sr);
            return propObj;
        }

        private static Material s_defaultPropMaterial;

        public static Material GetOrCreateDefaultPropMaterial()
        {
            if (s_defaultPropMaterial != null) return s_defaultPropMaterial;

            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("OneRoof/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                var mat = new Material(shader) { name = "Prop_DefaultTransparentMaterial" };
                mat.SetOverrideTag("RenderType", "Transparent");
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                s_defaultPropMaterial = mat;
            }
            return s_defaultPropMaterial;
        }

        public void ClearProps()
        {
            for (var i = _placedProps.Count - 1; i >= 0; i--)
            {
                if (_placedProps[i] != null)
                {
                    if (UnityEngine.Application.isPlaying)
                    {
                        Destroy(_placedProps[i]);
                    }
                    else
                    {
                        DestroyImmediate(_placedProps[i]);
                    }
                }
            }
            _placedProps.Clear();
            _placedRenderers.Clear();
            _currentPropIds.Clear();
            _clutterAlpha = -1f;
        }

        private void OnDestroy()
        {
            ClearProps();
        }
    }
}
