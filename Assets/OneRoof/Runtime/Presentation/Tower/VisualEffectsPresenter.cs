using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Small presentation-only shader/fallback effect used by construction, demolition, and congestion views.</summary>
    [DisallowMultipleComponent]
    public sealed class VisualEffectsPresenter : MonoBehaviour
    {
        private const float DefaultDuration = 0.55f;
        private MaterialPropertyBlock _block;
        private Renderer[] _renderers;
        private float _elapsed;
        private float _duration;
        private float _severity;
        private Mode _mode;
        private GameObject _auraObject;
        private MeshRenderer _auraRenderer;
        private Material _auraMaterial;
        private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private readonly List<Material> _effectMaterials = new List<Material>();
        private Texture2D _fadeTexture;
        private Shader _effectShader;

        private enum Mode { None, Constructing, Demolishing, Agitated }

        public bool IsTransitioning => _mode == Mode.Constructing || _mode == Mode.Demolishing;
        public float Severity => _severity;

        public void BeginConstruction(float duration = DefaultDuration)
        {
            Begin(Mode.Constructing, duration);
        }

        public void BeginDemolition(float duration = DefaultDuration)
        {
            Begin(Mode.Demolishing, duration);
        }

        public void SetAgitation(float severity)
        {
            var nextMode = severity > 0f ? Mode.Agitated : Mode.None;
            if (_mode != nextMode) _elapsed = 0f;
            _mode = nextMode;
            _severity = Mathf.Clamp01(severity);
            if (_mode == Mode.Agitated) EnsureAura();
            if (_auraObject != null) _auraObject.SetActive(_mode == Mode.Agitated);
            CacheRenderers();
            Apply();
        }

        public void Advance(float deltaTime)
        {
            if (_mode == Mode.None) return;
            _elapsed += Mathf.Max(0f, deltaTime);
            Apply();
            if ((_mode == Mode.Constructing || _mode == Mode.Demolishing) && _elapsed >= _duration)
            {
                var demolished = _mode == Mode.Demolishing;
                _mode = Mode.None;
                RestoreMaterials();
                if (demolished)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void Update() => Advance(UnityEngine.Application.isPlaying ? Time.deltaTime : 0f);

        private void Begin(Mode mode, float duration)
        {
            RestoreMaterials();
            _mode = mode;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            CacheRenderers();
            Apply();
        }

        private void CacheRenderers()
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (_mode != Mode.Constructing && _mode != Mode.Demolishing) return;
            if (_effectShader == null) _effectShader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");
            if (_effectShader == null) return;
            if (_fadeTexture == null) _fadeTexture = CreateFadeTexture();
            foreach (var renderer in _renderers)
            {
                if (renderer == null || renderer == _auraRenderer || _originalMaterials.ContainsKey(renderer)) continue;
                var originals = renderer.sharedMaterials;
                var replacements = new Material[originals.Length];
                for (var i = 0; i < originals.Length; i++)
                {
                    var source = originals[i];
                    if (source == null) continue;
                    var effect = new Material(_effectShader) { name = source.name + " (city transition)" };
                    effect.mainTexture = source.mainTexture;
                    effect.color = source.HasProperty("_Color") ? source.GetColor("_Color")
                        : source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
                    effect.EnableKeyword("FADE_ON");
                    effect.EnableKeyword("GLOW_ON");
                    effect.SetTexture("_FadeTex", _fadeTexture);
                    effect.SetFloat("_FadeBurnWidth", 0.07f);
                    effect.SetFloat("_FadeBurnTransition", 0.08f);
                    effect.SetColor("_FadeBurnColor", TransitionColor(_mode));
                    effect.SetFloat("_FadeBurnGlow", 1.5f);
                    effect.SetColor("_GlowColor", TransitionColor(_mode));
                    replacements[i] = effect;
                    _effectMaterials.Add(effect);
                }
                _originalMaterials.Add(renderer, originals);
                renderer.sharedMaterials = replacements;
            }
        }

        private static Color TransitionColor(Mode mode) => mode == Mode.Constructing
            ? new Color(0.23f, 0.78f, 1f) : new Color(1f, 0.42f, 0.18f);

        private static Texture2D CreateFadeTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "City transition grain", filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var noise = ((x * 73 + y * 151 + x * y * 17) & 255) / 255f;
                    var value = Mathf.Clamp01(0.55f * noise + 0.45f * y / (size - 1f));
                    pixels[y * size + x] = new Color(value, value, value, 1f);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void RestoreMaterials()
        {
            foreach (var pair in _originalMaterials)
                if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
            _originalMaterials.Clear();
            foreach (var material in _effectMaterials) DestroyOwned(material);
            _effectMaterials.Clear();
            DestroyOwned(_fadeTexture);
            _fadeTexture = null;
        }

        private void Apply()
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            if (_renderers == null || _renderers.Length == 0) CacheRenderers();
            if (_renderers == null) return;
            var progress = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
            var alpha = 1f;
            var glow = 0f;
            if (_mode == Mode.Constructing) { alpha = progress; glow = 1f - progress; }
            else if (_mode == Mode.Demolishing) { alpha = 1f - progress; glow = progress; }
            else if (_mode == Mode.Agitated) { glow = _severity * (0.55f + 0.45f * Mathf.Sin(_elapsed * 7f)); }

            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetFloat("_FadeAmount", _mode == Mode.Constructing ? 1f - progress : _mode == Mode.Demolishing ? progress : -0.1f);
                _block.SetFloat("_Alpha", alpha);
                _block.SetFloat("_Glow", glow * 0.9f);
                _block.SetColor("_GlowColor", Color.Lerp(new Color(0.2f, 0.9f, 1f), new Color(1f, 0.28f, 0.08f), _severity));
                renderer.SetPropertyBlock(_block);
            }

            if (_auraObject != null && _auraRenderer != null)
            {
                var pulse = 1f + glow * 0.32f;
                _auraObject.transform.localScale = new Vector3(pulse, pulse, 1f);
                _auraRenderer.GetPropertyBlock(_block);
                _block.SetColor("_BaseColor", new Color(1f, 0.26f, 0.06f, glow * 0.18f));
                _block.SetColor("_Color", new Color(1f, 0.26f, 0.06f, glow * 0.18f));
                _auraRenderer.SetPropertyBlock(_block);
            }
        }

        private void EnsureAura()
        {
            if (_auraObject != null) return;
            _auraObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _auraObject.name = "CongestionAgitationAura";
            _auraObject.transform.SetParent(transform, false);
            _auraObject.transform.localPosition = new Vector3(0f, 0.2f, 0.12f);
            _auraObject.transform.localScale = new Vector3(0.58f, 0.78f, 1f);
            var collider = _auraObject.GetComponent<Collider>();
            if (collider != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            _auraRenderer = _auraObject.GetComponent<MeshRenderer>();
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader")
                ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _auraMaterial = new Material(shader) { renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent };
                if (shader.name == "AllIn1SpriteShader/AllIn1SpriteShader")
                {
                    _auraMaterial.EnableKeyword("GLOW_ON");
                    _auraMaterial.SetColor("_GlowColor", new Color(1f, 0.33f, 0.12f));
                    _auraMaterial.SetFloat("_Glow", 0.6f);
                }
                if (_auraMaterial.HasProperty("_Surface")) _auraMaterial.SetFloat("_Surface", 1f);
                _auraMaterial.SetOverrideTag("RenderType", "Transparent");
                _auraMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _auraMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _auraMaterial.SetInt("_ZWrite", 0);
                _auraRenderer.sharedMaterial = _auraMaterial;
            }
        }

        private void OnDestroy()
        {
            RestoreMaterials();
            if (_auraMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_auraMaterial);
                else DestroyImmediate(_auraMaterial);
            }
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
