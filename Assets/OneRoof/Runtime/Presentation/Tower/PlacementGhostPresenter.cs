using System;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Presentation component that renders a ghost footprint preview in the tower cutaway
    /// when the player hovers over grid cells in Build mode.
    /// Uses AllIn1SpriteShader when available for a sci-fi holographic footprint with
    /// animated scanlines and dynamic validity outlines, falling back to transparent unlit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlacementGhostPresenter : MonoBehaviour
    {
        private static readonly Color ValidColor = new Color(0.2f, 0.88f, 0.45f, 0.55f);
        private static readonly Color InvalidColor = new Color(0.95f, 0.25f, 0.25f, 0.55f);

        private static readonly Color ValidStripeColor = new Color(0.35f, 1f, 0.6f, 0.85f);
        private static readonly Color InvalidStripeColor = new Color(1f, 0.25f, 0.25f, 0.95f);

        private static readonly Color ValidOutlineColor = new Color(0.2f, 0.95f, 0.5f, 1f);
        private static readonly Color InvalidOutlineColor = new Color(1f, 0.15f, 0.15f, 1f);

        private GameObject _ghostObject;
        private MeshRenderer _ghostRenderer;
        private MeshFilter _ghostMeshFilter;
        private Mesh _ghostMesh;
        private Color[] _meshColors;
        private MaterialPropertyBlock _colorBlock;
        private Material _ghostMaterial;
        private Texture2D _ghostTexture;
        private bool _isAllIn1Shader;

        public bool IsVisible => _ghostObject != null && _ghostObject.activeSelf;

        public bool IsValid { get; private set; }

        public Vector3 CurrentPosition => _ghostObject != null ? _ghostObject.transform.position : Vector3.zero;

        public Vector2 CurrentSize => _ghostObject != null ? new Vector2(_ghostObject.transform.localScale.x, _ghostObject.transform.localScale.y) : Vector2.zero;

        private void Awake()
        {
            EnsureGhostObject();
        }

        private void OnDestroy()
        {
            if (_ghostMaterial != null)
            {
                DestroyAsset(_ghostMaterial);
                _ghostMaterial = null;
            }

            if (_ghostTexture != null)
            {
                DestroyAsset(_ghostTexture);
                _ghostTexture = null;
            }

            if (_ghostMesh != null)
            {
                DestroyAsset(_ghostMesh);
                _ghostMesh = null;
            }

            if (_ghostObject != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(_ghostObject);
                }
                else
                {
                    DestroyImmediate(_ghostObject);
                }
                _ghostObject = null;
            }
        }

        private static void DestroyAsset(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (UnityEngine.Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        public void ShowGhost(Vector3 worldPosition, Vector2 size, bool isValid, Color? customColor = null)
        {
            EnsureGhostObject();
            IsValid = isValid;

            _ghostObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.5f);
            _ghostObject.transform.localScale = new Vector3(Math.Max(0.1f, size.x), Math.Max(0.1f, size.y), 1f);

            Color baseColor;
            Color stripeColor;
            Color outlineColor;
            float stripeSpeed;
            float outlineWidth;
            float outlineGlow;

            if (customColor.HasValue)
            {
                baseColor = customColor.Value;
                stripeColor = customColor.Value;
                outlineColor = customColor.Value;
                stripeSpeed = isValid ? 3.0f : 6.0f;
                outlineWidth = isValid ? 0.008f : 0.016f;
                outlineGlow = isValid ? 1.8f : 3.0f;
            }
            else if (isValid)
            {
                baseColor = ValidColor;
                stripeColor = ValidStripeColor;
                outlineColor = ValidOutlineColor;
                stripeSpeed = 3.0f;
                outlineWidth = 0.008f;
                outlineGlow = 1.8f;
            }
            else
            {
                baseColor = InvalidColor;
                stripeColor = InvalidStripeColor;
                outlineColor = InvalidOutlineColor;
                stripeSpeed = 7.0f;
                outlineWidth = 0.016f;
                outlineGlow = 3.5f;
            }

            _colorBlock.SetColor("_BaseColor", baseColor);
            _colorBlock.SetColor("_Color", baseColor);

            if (_isAllIn1Shader)
            {
                _colorBlock.SetColor("_HologramStripeColor", stripeColor);
                _colorBlock.SetFloat("_HologramStripesSpeed", stripeSpeed);
                _colorBlock.SetColor("_OutlineColor", outlineColor);
                _colorBlock.SetFloat("_OutlineWidth", outlineWidth);
                _colorBlock.SetFloat("_OutlineGlow", outlineGlow);
                _colorBlock.SetColor("_GlowColor", outlineColor);
            }

            _ghostRenderer.SetPropertyBlock(_colorBlock);

            // Update mesh vertex colors for shaders reading v.color
            if (_ghostMesh != null)
            {
                if (_meshColors == null || _meshColors.Length != 4)
                {
                    _meshColors = new Color[4];
                }
                _meshColors[0] = baseColor;
                _meshColors[1] = baseColor;
                _meshColors[2] = baseColor;
                _meshColors[3] = baseColor;
                _ghostMesh.colors = _meshColors;
            }

            if (!_ghostObject.activeSelf)
            {
                _ghostObject.SetActive(true);
            }
        }

        public void HideGhost()
        {
            if (_ghostObject != null && _ghostObject.activeSelf)
            {
                _ghostObject.SetActive(false);
            }
        }

        private void EnsureGhostObject()
        {
            if (_colorBlock == null)
            {
                _colorBlock = new MaterialPropertyBlock();
            }

            if (_ghostMaterial == null)
            {
                _ghostMaterial = CreateGhostMaterial(out _isAllIn1Shader);
            }

            if (_ghostObject == null)
            {
                var existing = transform.Find("PlacementGhost");
                if (existing != null)
                {
                    _ghostObject = existing.gameObject;
                }
                else
                {
                    _ghostObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _ghostObject.name = "PlacementGhost";
                    _ghostObject.transform.SetParent(transform, false);

                    var col = _ghostObject.GetComponent<Collider>();
                    if (col != null)
                    {
                        DestroyImmediate(col);
                    }
                }

                _ghostObject.SetActive(false);
            }

            if (_ghostMeshFilter == null && _ghostObject != null)
            {
                _ghostMeshFilter = _ghostObject.GetComponent<MeshFilter>();
                if (_ghostMeshFilter != null && _ghostMesh == null)
                {
                    _ghostMesh = _ghostMeshFilter.mesh;
                }
            }

            if (_ghostRenderer == null && _ghostObject != null)
            {
                _ghostRenderer = _ghostObject.GetComponent<MeshRenderer>();
            }

            if (_ghostRenderer != null)
            {
                if (_ghostRenderer.sharedMaterial == null)
                {
                    _ghostRenderer.sharedMaterial = _ghostMaterial;
                }

                if (_isAllIn1Shader && _ghostTexture == null)
                {
                    _ghostTexture = CreateBorderTexture();
                    _ghostMaterial.mainTexture = _ghostTexture;
                }
            }
        }

        private static Material CreateGhostMaterial(out bool isAllIn1Shader)
        {
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new MissingReferenceException("No unlit shader available for PlacementGhostPresenter.");
            }

            var mat = new Material(shader);
            isAllIn1Shader = shader.name.IndexOf("AllIn1SpriteShader", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isAllIn1Shader)
            {
                mat.EnableKeyword("HOLOGRAM_ON");
                mat.EnableKeyword("OUTBASE_ON");
                mat.EnableKeyword("GLOW_ON");

                mat.SetFloat("_HologramStripesAmount", 0.08f);
                mat.SetFloat("_HologramUnmodAmount", 0.0f);
                mat.SetFloat("_HologramMinAlpha", 0.25f);
                mat.SetFloat("_HologramMaxAlpha", 0.85f);
                mat.SetFloat("_HologramBlend", 0.75f);

                mat.SetFloat("_OutlineAlpha", 0.9f);
                mat.SetFloat("_OutlineWidth", 0.008f);
                mat.SetFloat("_OutlineGlow", 1.8f);

                mat.SetFloat("_Glow", 1.2f);
            }
            else
            {
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1); // Transparent in URP
                }
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
                name = "GhostBorderTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            var insideColor = new Color32(255, 255, 255, 255);
            var clearColor = new Color32(255, 255, 255, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < border || x >= size - border || y < border || y >= size - border)
                    {
                        pixels[y * size + x] = clearColor;
                    }
                    else
                    {
                        pixels[y * size + x] = insideColor;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
