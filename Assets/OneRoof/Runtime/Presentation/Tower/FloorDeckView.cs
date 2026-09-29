using System;
using System.Collections.Generic;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Architecture;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Dedicated presentation component representing the physical floor deck between levels.
    /// Manages the walking tread surface, structural core depth, ceiling soffit, and elevator shaft cutout.
    /// Supports dynamic theme swapping and procedural fallbacks.
    /// </summary>
    public sealed class FloorDeckView : MonoBehaviour
    {
        public const float ShaftLeft = -2.40f;
        public const float ShaftRight = -1.40f;
        public const float SillWidth = 0.06f;

        public const float TreadZ = 0.05f;
        public const float CoreZ = 0.08f;
        public const float SoffitZ = 0.12f;
        public const float SillZ = 0.04f;
        public const float FasciaZ = 0.06f;

        public const int SoffitSortingOrder = -3;
        public const int CoreSortingOrder = -2;
        public const int TreadSortingOrder = -1;
        public const int SillSortingOrder = 0;

        private int _floorLevel;
        private CellBounds _slabBounds;
        private FloorTheme _theme;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        // Wing roots
        private GameObject _leftWing;
        private GameObject _rightWing;
        private GameObject _continuousCenter;
        private MeshRenderer _continuousTread;
        private MeshRenderer _continuousCore;
        private MeshRenderer _continuousSoffit;

        // Left Wing Renderers
        private MeshRenderer _leftTread;
        private MeshRenderer _leftCore;
        private MeshRenderer _leftSoffit;
        private MeshRenderer _leftSill;
        private MeshRenderer _leftFascia;

        // Right Wing Renderers
        private MeshRenderer _rightTread;
        private MeshRenderer _rightCore;
        private MeshRenderer _rightSoffit;
        private MeshRenderer _rightSill;
        private MeshRenderer _rightFascia;

        public int FloorLevel => _floorLevel;
        public CellBounds SlabBounds => _slabBounds;
        public FloorTheme CurrentTheme => _theme;
        public GameObject LeftWing => _leftWing;
        public GameObject RightWing => _rightWing;

        public MeshRenderer LeftTread => _leftTread;
        public MeshRenderer LeftCore => _leftCore;
        public MeshRenderer LeftSoffit => _leftSoffit;
        public MeshRenderer RightTread => _rightTread;
        public MeshRenderer RightCore => _rightCore;
        public MeshRenderer RightSoffit => _rightSoffit;

        public void Initialize(int floorLevel, Material worldMaterial, MaterialPropertyBlock colorBlock, FloorTheme initialTheme = null)
        {
            _floorLevel = floorLevel;
            _worldMaterial = worldMaterial;
            _colorBlock = colorBlock ?? new MaterialPropertyBlock();
            _theme = initialTheme ?? FloorThemeCatalog.GetTheme(FloorThemeCatalog.ConcreteSlab);
        }

        public void ApplyTheme(FloorTheme theme)
        {
            if (theme == null) return;
            _theme = theme;
            RefreshAppearance();
        }

        public void UpdateGeometry(CellBounds slab, bool hasShaftCutout = true)
        {
            _slabBounds = slab;
            if (_theme == null) _theme = FloorThemeCatalog.GetTheme(FloorThemeCatalog.ConcreteSlab);

            var worldLeft = -2.40f + slab.MinX * 0.5f;
            var worldRight = -2.40f + (slab.MaxX + 1) * 0.5f;
            var floorY = TowerStructurePresenter.FloorY(_floorLevel);
            var baselineY = floorY - 0.74f;

            var treadH = _theme.TreadThickness;
            var coreH = _theme.CoreThickness;
            var soffitH = _theme.SoffitThickness;

            var treadY = baselineY - treadH * 0.5f;
            var coreY = baselineY - treadH - coreH * 0.5f;
            var soffitY = baselineY - treadH - coreH - soffitH * 0.5f;
            var totalH = treadH + coreH + soffitH;
            var fullDeckCenterY = baselineY - totalH * 0.5f;

            if (hasShaftCutout)
            {
                if (_continuousCenter != null) _continuousCenter.SetActive(false);

                // --- Left Wing (West of Shaft) ---
                if (ShaftLeft > worldLeft)
                {
                    EnsureLeftWing();
                    _leftWing.SetActive(true);
                    var width = ShaftLeft - worldLeft;
                    var centerX = (worldLeft + ShaftLeft) * 0.5f;

                    UpdateSegment(_leftTread, new Vector3(centerX, treadY, TreadZ), new Vector2(width, treadH), Color.white, TreadSortingOrder, GetSurfaceTexture());
                    UpdateSegment(_leftCore, new Vector3(centerX, coreY, CoreZ), new Vector2(width, coreH), _theme.CoreColor, CoreSortingOrder);
                    UpdateSegment(_leftSoffit, new Vector3(centerX, soffitY, SoffitZ), new Vector2(width, soffitH), _theme.SoffitColor, SoffitSortingOrder);

                    // Threshold sill at shaft edge
                    var sillCenterX = ShaftLeft - SillWidth * 0.5f;
                    UpdateSegment(_leftSill, new Vector3(sillCenterX, baselineY - treadH * 0.5f, SillZ), new Vector2(SillWidth, treadH * 1.2f), _theme.ThresholdTrimColor, SillSortingOrder);

                    // Exterior fascia at outer edge
                    var fasciaCenterX = worldLeft + SillWidth * 0.5f;
                    UpdateSegment(_leftFascia, new Vector3(fasciaCenterX, fullDeckCenterY, FasciaZ), new Vector2(SillWidth, totalH), _theme.ExteriorFasciaColor, SillSortingOrder);
                }
                else if (_leftWing != null)
                {
                    _leftWing.SetActive(false);
                }

                // --- Right Wing (East of Shaft) ---
                if (worldRight > ShaftRight)
                {
                    EnsureRightWing();
                    _rightWing.SetActive(true);
                    var width = worldRight - ShaftRight;
                    var centerX = (ShaftRight + worldRight) * 0.5f;

                    UpdateSegment(_rightTread, new Vector3(centerX, treadY, TreadZ), new Vector2(width, treadH), Color.white, TreadSortingOrder, GetSurfaceTexture());
                    UpdateSegment(_rightCore, new Vector3(centerX, coreY, CoreZ), new Vector2(width, coreH), _theme.CoreColor, CoreSortingOrder);
                    UpdateSegment(_rightSoffit, new Vector3(centerX, soffitY, SoffitZ), new Vector2(width, soffitH), _theme.SoffitColor, SoffitSortingOrder);

                    // Threshold sill at shaft edge
                    var sillCenterX = ShaftRight + SillWidth * 0.5f;
                    UpdateSegment(_rightSill, new Vector3(sillCenterX, baselineY - treadH * 0.5f, SillZ), new Vector2(SillWidth, treadH * 1.2f), _theme.ThresholdTrimColor, SillSortingOrder);

                    // Exterior fascia at outer edge
                    var fasciaCenterX = worldRight - SillWidth * 0.5f;
                    UpdateSegment(_rightFascia, new Vector3(fasciaCenterX, fullDeckCenterY, FasciaZ), new Vector2(SillWidth, totalH), _theme.ExteriorFasciaColor, SillSortingOrder);
                }
                else if (_rightWing != null)
                {
                    _rightWing.SetActive(false);
                }
            }
            else
            {
                // Continuous deck across the whole width
                if (_leftWing != null) _leftWing.SetActive(false);
                if (_rightWing != null) _rightWing.SetActive(false);
                EnsureContinuousDeck();
                _continuousCenter.SetActive(true);

                var width = worldRight - worldLeft;
                var centerX = (worldLeft + worldRight) * 0.5f;
                UpdateSegment(_continuousTread, new Vector3(centerX, treadY, TreadZ), new Vector2(width, treadH), Color.white, TreadSortingOrder, GetSurfaceTexture());
                UpdateSegment(_continuousCore, new Vector3(centerX, coreY, CoreZ), new Vector2(width, coreH), _theme.CoreColor, CoreSortingOrder);
                UpdateSegment(_continuousSoffit, new Vector3(centerX, soffitY, SoffitZ), new Vector2(width, soffitH), _theme.SoffitColor, SoffitSortingOrder);
            }
        }

        public void RefreshAppearance()
        {
            if (_theme == null) return;

            SetRendererColor(_leftTread, Color.white, GetSurfaceTexture());
            SetRendererColor(_leftCore, _theme.CoreColor);
            SetRendererColor(_leftSoffit, _theme.SoffitColor);
            SetRendererColor(_leftSill, _theme.ThresholdTrimColor);
            SetRendererColor(_leftFascia, _theme.ExteriorFasciaColor);

            SetRendererColor(_rightTread, Color.white, GetSurfaceTexture());
            SetRendererColor(_rightCore, _theme.CoreColor);
            SetRendererColor(_rightSoffit, _theme.SoffitColor);
            SetRendererColor(_rightSill, _theme.ThresholdTrimColor);
            SetRendererColor(_rightFascia, _theme.ExteriorFasciaColor);

            SetRendererColor(_continuousTread, Color.white, GetSurfaceTexture());
            SetRendererColor(_continuousCore, _theme.CoreColor);
            SetRendererColor(_continuousSoffit, _theme.SoffitColor);
        }

        private void EnsureLeftWing()
        {
            if (_leftWing != null) return;
            _leftWing = new GameObject("LeftWing");
            _leftWing.transform.SetParent(transform, false);

            _leftTread = CreateSubQuad("Tread", _leftWing.transform);
            _leftCore = CreateSubQuad("Core", _leftWing.transform);
            _leftSoffit = CreateSubQuad("Soffit", _leftWing.transform);
            _leftSill = CreateSubQuad("ShaftSill", _leftWing.transform);
            _leftFascia = CreateSubQuad("ExteriorFascia", _leftWing.transform);
        }

        private void EnsureRightWing()
        {
            if (_rightWing != null) return;
            _rightWing = new GameObject("RightWing");
            _rightWing.transform.SetParent(transform, false);

            _rightTread = CreateSubQuad("Tread", _rightWing.transform);
            _rightCore = CreateSubQuad("Core", _rightWing.transform);
            _rightSoffit = CreateSubQuad("Soffit", _rightWing.transform);
            _rightSill = CreateSubQuad("ShaftSill", _rightWing.transform);
            _rightFascia = CreateSubQuad("ExteriorFascia", _rightWing.transform);
        }

        private void EnsureContinuousDeck()
        {
            if (_continuousCenter != null) return;
            _continuousCenter = new GameObject("ContinuousDeck");
            _continuousCenter.transform.SetParent(transform, false);

            _continuousTread = CreateSubQuad("Tread", _continuousCenter.transform);
            _continuousCore = CreateSubQuad("Core", _continuousCenter.transform);
            _continuousSoffit = CreateSubQuad("Soffit", _continuousCenter.transform);
        }

        private MeshRenderer CreateSubQuad(string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _worldMaterial;
            return renderer;
        }

        private Texture GetSurfaceTexture()
        {
            return _theme != null ? FloorThemeCatalog.GetOrLoadSurfaceSprite(_theme)?.texture : null;
        }

        private void UpdateSegment(MeshRenderer renderer, Vector3 localPos, Vector2 size, Color color, int sortingOrder, Texture texture = null)
        {
            if (renderer == null) return;
            var t = renderer.transform;
            t.localPosition = localPos;
            t.localScale = new Vector3(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y), 1f);
            renderer.sortingOrder = sortingOrder;
            SetRendererColor(renderer, color, texture);
        }

        private void SetRendererColor(MeshRenderer renderer, Color color, Texture texture = null)
        {
            if (renderer == null || _worldMaterial == null) return;
            renderer.sharedMaterial = _worldMaterial;

            if (_colorBlock == null) _colorBlock = new MaterialPropertyBlock();
            _colorBlock.Clear();
            _colorBlock.SetColor("_BaseColor", color);
            _colorBlock.SetColor("_Color", color);
            var surfaceTexture = texture != null ? texture : Texture2D.whiteTexture;
            _colorBlock.SetTexture("_BaseMap", surfaceTexture);
            _colorBlock.SetTexture("_MainTex", surfaceTexture);
            renderer.SetPropertyBlock(_colorBlock);
        }
    }
}
