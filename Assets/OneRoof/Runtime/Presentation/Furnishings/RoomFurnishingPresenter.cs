using System.Collections.Generic;
using OneRoof.Content;
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
                    if (Application.isPlaying)
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
