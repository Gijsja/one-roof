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
        private readonly List<GameObject> _placedProps = new List<GameObject>();

        public IReadOnlyList<GameObject> PlacedProps => _placedProps;

        public bool TryGetDockPosition(InteractionPointKind kind, int slot, out Vector3 worldPosition)
        {
            var matches = kind == InteractionPointKind.Sleep ? new[] { "bed" } :
                kind == InteractionPointKind.Work ? new[] { "desk", "bench", "monitor" } :
                kind == InteractionPointKind.Seat ? new[] { "sofa" } : new[] { "booth" };
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
                var selected = Mathf.Abs(slot) % matchCount;
                for (var i = 0; i < _placedProps.Count; i++)
                {
                    var prop = _placedProps[i];
                    if (prop != null && NameContainsAny(prop.name, matches) && selected-- == 0)
                    {
                        worldPosition = prop.transform.position + new Vector3(0f, 0.06f, -0.2f);
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

        public void FurnishRoom(string roomTheme, float width, float height, bool isWestSide)
        {
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
            // Bed on exterior side
            var bedX = isWestSide ? (-width * 0.5f + 0.65f) : (width * 0.5f - 0.65f);
            SpawnProp("prop.furniture.bed.v1", new Vector3(bedX, baselineY, 0.40f));

            // Planter near bed
            var planterX = isWestSide ? (-width * 0.5f + 0.20f) : (width * 0.5f - 0.20f);
            SpawnProp("prop.decor.planter.v1", new Vector3(planterX, baselineY, 0.42f));

            // Sofa in center-living area
            if (width >= 2.0f)
            {
                var sofaX = isWestSide ? (width * 0.5f - 0.95f) : (-width * 0.5f + 0.95f);
                SpawnProp("prop.furniture.sofa.v1", new Vector3(sofaX, baselineY, 0.35f));
            }

            // Bookcase near wall
            if (width >= 2.5f)
            {
                var bookcaseX = 0f;
                SpawnProp("prop.furniture.bookcase.v1", new Vector3(bookcaseX, baselineY, 0.45f));
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

        private GameObject SpawnProp(string contentId, Vector3 localPosition)
        {
            var propObj = new GameObject("Prop_" + contentId);
            propObj.transform.SetParent(transform, false);
            propObj.transform.localPosition = localPosition;

            var sr = propObj.AddComponent<SpriteRenderer>();
            sr.sprite = PropCatalog.GetPropSprite(contentId);
            sr.sortingOrder = -2;

            _placedProps.Add(propObj);
            return propObj;
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
        }

        private void OnDestroy()
        {
            ClearProps();
        }
    }
}
