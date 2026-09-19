using System;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Presentation component responsible for rendering pixel-perfect outline and soft glow silhouettes
    /// (using AllIn1SpriteShader with OUTBASE_ON and GLOW_ON) when hovering or selecting rooms, elevator shafts,
    /// and residents in Inspect mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InspectOutlinePresenter : MonoBehaviour
    {
        public static readonly Color HoverOutlineColor = new Color(0.35f, 0.85f, 1.0f, 0.95f);
        public static readonly Color HoverGlowColor = new Color(0.20f, 0.65f, 1.0f, 0.85f);

        public static readonly Color SelectionOutlineColor = new Color(1.0f, 0.85f, 0.20f, 1.0f);
        public static readonly Color SelectionGlowColor = new Color(1.0f, 0.65f, 0.10f, 0.95f);

        private Material _hoverBoxMaterial;
        private Material _hoverSpriteMaterial;
        private Material _selectionBoxMaterial;
        private Material _selectionSpriteMaterial;
        private Texture2D _borderTexture;
        private bool _isAllIn1Shader;

        private GameObject _hoverBox;
        private MeshRenderer _hoverBoxRenderer;
        private GameObject _hoverSpriteObj;
        private SpriteRenderer _hoverSpriteRenderer;
        private Transform _hoverFollowTarget;

        private GameObject _selectionBox;
        private MeshRenderer _selectionBoxRenderer;
        private GameObject _selectionSpriteObj;
        private SpriteRenderer _selectionSpriteRenderer;
        private Transform _selectionFollowTarget;

        public bool IsAllIn1Shader => _isAllIn1Shader;
        public Material HoverMaterial => _hoverBoxMaterial;
        public Material SelectionMaterial => _selectionBoxMaterial;

        public bool HasHoverTarget { get; private set; }
        public bool HasSelectionTarget { get; private set; }
        public Bounds HoverBounds { get; private set; }
        public Bounds SelectionBounds { get; private set; }

        private void Awake()
        {
            EnsureMaterials();
            EnsureGameObjects();
            ClearAll();
        }

        private void LateUpdate()
        {
            // Keep sprite silhouettes locked to moving targets (e.g. walking residents)
            if (HasHoverTarget && _hoverFollowTarget != null && _hoverSpriteObj != null && _hoverSpriteObj.activeSelf)
            {
                _hoverSpriteObj.transform.position = _hoverFollowTarget.position + new Vector3(0f, 0f, -0.05f);
                _hoverSpriteObj.transform.localScale = _hoverFollowTarget.localScale;
            }

            if (HasSelectionTarget && _selectionFollowTarget != null && _selectionSpriteObj != null && _selectionSpriteObj.activeSelf)
            {
                _selectionSpriteObj.transform.position = _selectionFollowTarget.position + new Vector3(0f, 0f, -0.05f);
                _selectionSpriteObj.transform.localScale = _selectionFollowTarget.localScale;
            }
        }

        public void HighlightHoverBox(Bounds bounds)
        {
            EnsureMaterials();
            EnsureGameObjects();

            HasHoverTarget = true;
            HoverBounds = bounds;
            _hoverFollowTarget = null;

            if (_hoverSpriteObj != null) _hoverSpriteObj.SetActive(false);

            if (_hoverBox != null)
            {
                _hoverBox.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.25f);
                _hoverBox.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
                _hoverBox.SetActive(true);
            }
        }

        public void HighlightHoverSprite(Sprite sprite, Transform sourceTransform, Bounds bounds)
        {
            EnsureMaterials();
            EnsureGameObjects();

            HasHoverTarget = true;
            HoverBounds = bounds;
            _hoverFollowTarget = sourceTransform;

            if (_hoverBox != null) _hoverBox.SetActive(false);

            if (_hoverSpriteObj != null && _hoverSpriteRenderer != null)
            {
                _hoverSpriteRenderer.sprite = sprite;
                if (sourceTransform != null)
                {
                    _hoverSpriteObj.transform.position = sourceTransform.position + new Vector3(0f, 0f, -0.05f);
                    _hoverSpriteObj.transform.localScale = sourceTransform.localScale;
                }
                else
                {
                    _hoverSpriteObj.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.25f);
                    _hoverSpriteObj.transform.localScale = Vector3.one;
                }
                _hoverSpriteObj.SetActive(true);
            }
        }

        public void HighlightSelectBox(Bounds bounds)
        {
            EnsureMaterials();
            EnsureGameObjects();

            HasSelectionTarget = true;
            SelectionBounds = bounds;
            _selectionFollowTarget = null;

            if (_selectionSpriteObj != null) _selectionSpriteObj.SetActive(false);

            if (_selectionBox != null)
            {
                _selectionBox.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.28f);
                _selectionBox.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
                _selectionBox.SetActive(true);
            }
        }

        public void HighlightSelectSprite(Sprite sprite, Transform sourceTransform, Bounds bounds)
        {
            EnsureMaterials();
            EnsureGameObjects();

            HasSelectionTarget = true;
            SelectionBounds = bounds;
            _selectionFollowTarget = sourceTransform;

            if (_selectionBox != null) _selectionBox.SetActive(false);

            if (_selectionSpriteObj != null && _selectionSpriteRenderer != null)
            {
                _selectionSpriteRenderer.sprite = sprite;
                if (sourceTransform != null)
                {
                    _selectionSpriteObj.transform.position = sourceTransform.position + new Vector3(0f, 0f, -0.05f);
                    _selectionSpriteObj.transform.localScale = sourceTransform.localScale;
                }
                else
                {
                    _selectionSpriteObj.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.28f);
                    _selectionSpriteObj.transform.localScale = Vector3.one;
                }
                _selectionSpriteObj.SetActive(true);
            }
        }

        public void ClearHover()
        {
            HasHoverTarget = false;
            HoverBounds = default;
            _hoverFollowTarget = null;
            if (_hoverBox != null) _hoverBox.SetActive(false);
            if (_hoverSpriteObj != null) _hoverSpriteObj.SetActive(false);
        }

        public void ClearSelection()
        {
            HasSelectionTarget = false;
            SelectionBounds = default;
            _selectionFollowTarget = null;
            if (_selectionBox != null) _selectionBox.SetActive(false);
            if (_selectionSpriteObj != null) _selectionSpriteObj.SetActive(false);
        }

        public void ClearAll()
        {
            ClearHover();
            ClearSelection();
        }

        public void EnsureMaterials()
        {
            if (_hoverBoxMaterial != null && _selectionBoxMaterial != null) return;

            if (_borderTexture == null)
            {
                _borderTexture = CreateBorderTexture();
            }

            _hoverBoxMaterial = CreateBoxMaterial(HoverOutlineColor, HoverGlowColor, _borderTexture, out _isAllIn1Shader);
            _hoverBoxMaterial.name = "InspectHoverOutline_Mat";

            _hoverSpriteMaterial = CreateSpriteMaterial(HoverOutlineColor, HoverGlowColor, out _);
            _hoverSpriteMaterial.name = "InspectHoverSpriteOutline_Mat";

            _selectionBoxMaterial = CreateBoxMaterial(SelectionOutlineColor, SelectionGlowColor, _borderTexture, out _);
            _selectionBoxMaterial.name = "InspectSelectionOutline_Mat";

            _selectionSpriteMaterial = CreateSpriteMaterial(SelectionOutlineColor, SelectionGlowColor, out _);
            _selectionSpriteMaterial.name = "InspectSelectionSpriteOutline_Mat";
        }

        private void EnsureGameObjects()
        {
            if (_hoverBox == null)
            {
                _hoverBox = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _hoverBox.name = "Inspect_HoverBoxSilhouette";
                _hoverBox.transform.SetParent(transform, false);
                StripCollider(_hoverBox);
                _hoverBoxRenderer = _hoverBox.GetComponent<MeshRenderer>();
                _hoverBoxRenderer.sharedMaterial = _hoverBoxMaterial;
                _hoverBoxRenderer.sortingOrder = 20;
                _hoverBox.SetActive(false);
            }

            if (_hoverSpriteObj == null)
            {
                _hoverSpriteObj = new GameObject("Inspect_HoverSpriteSilhouette");
                _hoverSpriteObj.transform.SetParent(transform, false);
                _hoverSpriteRenderer = _hoverSpriteObj.AddComponent<SpriteRenderer>();
                _hoverSpriteRenderer.sharedMaterial = _hoverSpriteMaterial;
                _hoverSpriteRenderer.sortingOrder = 20;
                _hoverSpriteObj.SetActive(false);
            }

            if (_selectionBox == null)
            {
                _selectionBox = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _selectionBox.name = "Inspect_SelectionBoxSilhouette";
                _selectionBox.transform.SetParent(transform, false);
                StripCollider(_selectionBox);
                _selectionBoxRenderer = _selectionBox.GetComponent<MeshRenderer>();
                _selectionBoxRenderer.sharedMaterial = _selectionBoxMaterial;
                _selectionBoxRenderer.sortingOrder = 22;
                _selectionBox.SetActive(false);
            }

            if (_selectionSpriteObj == null)
            {
                _selectionSpriteObj = new GameObject("Inspect_SelectionSpriteSilhouette");
                _selectionSpriteObj.transform.SetParent(transform, false);
                _selectionSpriteRenderer = _selectionSpriteObj.AddComponent<SpriteRenderer>();
                _selectionSpriteRenderer.sharedMaterial = _selectionSpriteMaterial;
                _selectionSpriteRenderer.sortingOrder = 22;
                _selectionSpriteObj.SetActive(false);
            }
        }

        private static Material CreateBoxMaterial(Color outlineColor, Color glowColor, Texture2D texture, out bool isAllIn1)
        {
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            if (shader == null) throw new MissingReferenceException("No unlit shader available for InspectOutlinePresenter.");

            var mat = new Material(shader);
            isAllIn1 = shader.name.IndexOf("AllIn1SpriteShader", StringComparison.OrdinalIgnoreCase) >= 0;

            mat.mainTexture = texture;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", outlineColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", outlineColor);

            if (isAllIn1)
            {
                mat.EnableKeyword("GLOW_ON");
                mat.SetColor("_GlowColor", glowColor);
                mat.SetFloat("_Glow", 1.8f);
            }
            else
            {
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent in URP
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return mat;
        }

        private static Material CreateSpriteMaterial(Color outlineColor, Color glowColor, out bool isAllIn1)
        {
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            if (shader == null) throw new MissingReferenceException("No unlit shader available for InspectOutlinePresenter.");

            var mat = new Material(shader);
            isAllIn1 = shader.name.IndexOf("AllIn1SpriteShader", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isAllIn1)
            {
                mat.EnableKeyword("OUTBASE_ON");
                mat.EnableKeyword("GLOW_ON");
                mat.EnableKeyword("ONLYOUTLINE_ON");

                mat.SetColor("_OutlineColor", outlineColor);
                mat.SetFloat("_OutlineAlpha", outlineColor.a);
                mat.SetFloat("_OutlineWidth", 0.02f);
                mat.SetFloat("_OutlineGlow", 2.2f);

                mat.SetColor("_GlowColor", glowColor);
                mat.SetFloat("_Glow", 1.5f);
            }
            else
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", outlineColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", outlineColor);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return mat;
        }

        private static Texture2D CreateBorderTexture()
        {
            const int size = 64;
            const int border = 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "InspectBorderTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };

            var pixels = new Color32[size * size];
            var white = new Color32(255, 255, 255, 255);
            var clear = new Color32(255, 255, 255, 8); // 3% faint interior tint
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isBorder = x < border || x >= size - border || y < border || y >= size - border;
                    pixels[y * size + x] = isBorder ? white : clear;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private void OnDestroy()
        {
            if (_hoverBoxMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_hoverBoxMaterial);
                else DestroyImmediate(_hoverBoxMaterial);
            }
            if (_hoverSpriteMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_hoverSpriteMaterial);
                else DestroyImmediate(_hoverSpriteMaterial);
            }
            if (_selectionBoxMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_selectionBoxMaterial);
                else DestroyImmediate(_selectionBoxMaterial);
            }
            if (_selectionSpriteMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_selectionSpriteMaterial);
                else DestroyImmediate(_selectionSpriteMaterial);
            }
            if (_borderTexture != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_borderTexture);
                else DestroyImmediate(_borderTexture);
            }
        }
    }
}
