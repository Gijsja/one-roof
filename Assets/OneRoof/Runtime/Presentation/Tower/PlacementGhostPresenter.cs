using System;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Presentation component that renders a ghost footprint preview in the tower cutaway
    /// when the player hovers over grid cells in Build mode.
    /// Tints green for valid placements and red for invalid placements.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlacementGhostPresenter : MonoBehaviour
    {
        private static readonly Color ValidColor = new Color(0.2f, 0.88f, 0.45f, 0.55f);
        private static readonly Color InvalidColor = new Color(0.95f, 0.25f, 0.25f, 0.55f);

        private GameObject _ghostObject;
        private MeshRenderer _ghostRenderer;
        private MaterialPropertyBlock _colorBlock;
        private Material _ghostMaterial;

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
                Destroy(_ghostMaterial);
                _ghostMaterial = null;
            }

            if (_ghostObject != null)
            {
                Destroy(_ghostObject);
                _ghostObject = null;
            }
        }

        public void ShowGhost(Vector3 worldPosition, Vector2 size, bool isValid)
        {
            EnsureGhostObject();
            IsValid = isValid;

            _ghostObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.5f);
            _ghostObject.transform.localScale = new Vector3(Math.Max(0.1f, size.x), Math.Max(0.1f, size.y), 1f);

            var color = isValid ? ValidColor : InvalidColor;
            _colorBlock.SetColor("_BaseColor", color);
            _colorBlock.SetColor("_Color", color);
            _ghostRenderer.SetPropertyBlock(_colorBlock);

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
            if (_ghostObject != null)
            {
                return;
            }

            _ghostMaterial = CreateGhostMaterial();
            _colorBlock = new MaterialPropertyBlock();

            _ghostObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _ghostObject.name = "PlacementGhost";
            _ghostObject.transform.SetParent(transform, false);

            var col = _ghostObject.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }

            _ghostRenderer = _ghostObject.GetComponent<MeshRenderer>();
            _ghostRenderer.sharedMaterial = _ghostMaterial;

            _ghostObject.SetActive(false);
        }

        private static Material CreateGhostMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new MissingReferenceException("No unlit shader available for PlacementGhostPresenter.");
            }

            var mat = new Material(shader);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // Transparent in URP
            }
            return mat;
        }
    }
}
