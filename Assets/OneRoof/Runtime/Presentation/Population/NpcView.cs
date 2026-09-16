using OneRoof.Application.Population;
using OneRoof.Domain.Population;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Unity view representation for a single pooled NPC.
    /// Owned by <see cref="NpcViewPool"/> and bound strictly through entity IDs
    /// and read-only <see cref="NpcProjection"/> records.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcView : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock;

        public int? BoundEntityId { get; private set; }

        public bool IsBound => BoundEntityId.HasValue;

        public NpcProjection CurrentProjection { get; private set; }

        public MeshRenderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<MeshRenderer>());

        private void Awake()
        {
            EnsureRendererAndPropertyBlock();
        }

        public void Bind(in NpcProjection projection, Vector3 worldPosition)
        {
            BoundEntityId = projection.PersonId;
            CurrentProjection = projection;
            transform.position = worldPosition;
            gameObject.SetActive(true);
            UpdateVisualAppearance(projection);
        }

        public void UpdateView(in NpcProjection projection, Vector3 worldPosition)
        {
            CurrentProjection = projection;
            transform.position = worldPosition;
            UpdateVisualAppearance(projection);
        }

        public void Unbind()
        {
            BoundEntityId = null;
            CurrentProjection = default;
            gameObject.SetActive(false);
        }

        private void EnsureRendererAndPropertyBlock()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void UpdateVisualAppearance(in NpcProjection projection)
        {
            EnsureRendererAndPropertyBlock();

            if (_renderer == null)
            {
                return;
            }

            var color = ResolveNpcColor(projection);
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorPropertyId, color);
            _propertyBlock.SetColor(ColorPropertyId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        public static Color ResolveNpcColor(in NpcProjection projection)
        {
            if (projection.IsInTransit)
            {
                return new Color(1f, 0.85f, 0.25f); // Gold / transit
            }

            switch (projection.CurrentActivity)
            {
                case ActivityKind.Working:
                    return new Color(0.35f, 0.75f, 1f); // Blue / work
                case ActivityKind.Eating:
                    return new Color(0.95f, 0.45f, 0.35f); // Coral / eat
                case ActivityKind.Leisure:
                    return new Color(0.55f, 0.9f, 0.45f); // Green / leisure
                case ActivityKind.Sleeping:
                    return new Color(0.45f, 0.4f, 0.65f); // Muted purple / sleep
                default:
                    // Household color tint
                    var h = (projection.HouseholdId * 37 % 360) / 360f;
                    return Color.HSVToRGB(h, 0.65f, 0.85f);
            }
        }
    }
}
