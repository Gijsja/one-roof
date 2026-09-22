using System;
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
            _mode = severity > 0f ? Mode.Agitated : Mode.None;
            _severity = Mathf.Clamp01(severity);
            _elapsed = 0f;
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
                if (demolished)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void Update() => Advance(UnityEngine.Application.isPlaying ? Time.deltaTime : 0f);

        private void Begin(Mode mode, float duration)
        {
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
                _block.SetFloat("_DissolveAmount", _mode == Mode.Demolishing ? progress : 0f);
                _block.SetFloat("_Fade", alpha);
                _block.SetFloat("_Glow", glow);
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
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _auraMaterial = new Material(shader) { renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent };
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
            if (_auraMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_auraMaterial);
                else DestroyImmediate(_auraMaterial);
            }
        }
    }
}
