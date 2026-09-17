using System.Collections.Generic;
using OneRoof.Application.Transit;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Deep presenter responsible for elevator shaft geometry (cavity, rails, columns, caps)
    /// and animated elevator car visuals.
    /// </summary>
    public sealed class ElevatorBankPresenter
    {
        private Transform _parent;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        private GameObject _shaftCavity;
        private GameObject _shaftRailLeft;
        private GameObject _shaftRailRight;
        private GameObject _shaftColumnLeft;
        private GameObject _shaftColumnRight;
        private GameObject _shaftPenthouse;
        private GameObject _shaftPitBuffer;
        private int _renderedShaftFloorCount;

        private readonly List<MeshRenderer> _elevatorViews = new List<MeshRenderer>();
        private readonly List<GameObject> _shaftObjects = new List<GameObject>();

        public IReadOnlyList<MeshRenderer> ElevatorViews => _elevatorViews;
        public int RenderedShaftFloorCount => _renderedShaftFloorCount;

        public void Initialize(Transform parent, Material worldMaterial, MaterialPropertyBlock colorBlock)
        {
            _parent = parent;
            _worldMaterial = worldMaterial;
            _colorBlock = colorBlock;
            _renderedShaftFloorCount = 0;
        }

        public void EnsureShaftViews(int floorCount)
        {
            var bottomY = TowerStructurePresenter.FloorY(0) - 0.74f;
            var topY = TowerStructurePresenter.FloorY(floorCount - 1) + 0.74f;
            var shaftHeight = topY - bottomY;
            var centerY = (bottomY + topY) * 0.5f;

            const float shaftCenterX = -1.90f;
            const float shaftWidth = 1.00f;
            const float colLeftX = -2.40f;
            const float colRightX = -1.40f;
            const float railLeftX = -2.36f;
            const float railRightX = -1.44f;

            if (_shaftCavity == null)
            {
                _shaftCavity = CreateQuad("Elevator Shaft Cavity", new Color(0.04f, 0.06f, 0.09f), new Vector3(shaftCenterX, centerY, 0.85f), new Vector2(shaftWidth, shaftHeight));
                _shaftRailLeft = CreateQuad("Shaft Rail Left", new Color(0.48f, 0.58f, 0.72f), new Vector3(railLeftX, centerY, 0.2f), new Vector2(0.035f, shaftHeight));
                _shaftRailRight = CreateQuad("Shaft Rail Right", new Color(0.48f, 0.58f, 0.72f), new Vector3(railRightX, centerY, 0.2f), new Vector2(0.035f, shaftHeight));
                _shaftColumnLeft = CreateQuad("Shaft Column Left", new Color(0.35f, 0.45f, 0.58f), new Vector3(colLeftX, centerY, 0.15f), new Vector2(0.06f, shaftHeight));
                _shaftColumnRight = CreateQuad("Shaft Column Right", new Color(0.35f, 0.45f, 0.58f), new Vector3(colRightX, centerY, 0.15f), new Vector2(0.06f, shaftHeight));
                _shaftPenthouse = CreateQuad("Shaft Penthouse Cap", new Color(0.38f, 0.48f, 0.62f), new Vector3(shaftCenterX, topY + 0.08f, 0.1f), new Vector2(1.12f, 0.16f));
                _shaftPitBuffer = CreateQuad("Shaft Pit Buffer", new Color(0.28f, 0.36f, 0.48f), new Vector3(shaftCenterX, bottomY - 0.08f, 0.1f), new Vector2(1.12f, 0.16f));
            }
            else
            {
                _shaftCavity.transform.position = new Vector3(shaftCenterX, centerY, 0.85f);
                _shaftCavity.transform.localScale = new Vector3(shaftWidth, shaftHeight, 1f);

                _shaftRailLeft.transform.position = new Vector3(railLeftX, centerY, 0.2f);
                _shaftRailLeft.transform.localScale = new Vector3(0.035f, shaftHeight, 1f);

                _shaftRailRight.transform.position = new Vector3(railRightX, centerY, 0.2f);
                _shaftRailRight.transform.localScale = new Vector3(0.035f, shaftHeight, 1f);

                _shaftColumnLeft.transform.position = new Vector3(colLeftX, centerY, 0.15f);
                _shaftColumnLeft.transform.localScale = new Vector3(0.06f, shaftHeight, 1f);

                _shaftColumnRight.transform.position = new Vector3(colRightX, centerY, 0.15f);
                _shaftColumnRight.transform.localScale = new Vector3(0.06f, shaftHeight, 1f);

                _shaftPenthouse.transform.position = new Vector3(shaftCenterX, topY + 0.08f, 0.1f);
                _shaftPitBuffer.transform.position = new Vector3(shaftCenterX, bottomY - 0.08f, 0.1f);
            }

            _renderedShaftFloorCount = floorCount;
        }

        public void EnsureElevatorViews(int targetCount)
        {
            while (_elevatorViews.Count < targetCount)
            {
                var index = _elevatorViews.Count;
                var x = targetCount == 1 ? -1.9f : (-2.15f + index * 0.5f);
                var view = CreateQuadRenderer($"Elevator Car {index}", new Color(0.25f, 0.92f, 0.65f), new Vector3(x, TowerStructurePresenter.FloorY(0), 0f), new Vector2(0.48f, 0.5f));
                _elevatorViews.Add(view);
            }
        }

        public void UpdateElevatorPositions(TowerProjection snapshot)
        {
            if (snapshot == null) return;

            for (var i = 0; i < snapshot.Elevators.Count; i++)
            {
                if (i >= _elevatorViews.Count)
                {
                    EnsureElevatorViews(snapshot.Elevators.Count);
                }

                var elevator = snapshot.Elevators[i];
                var targetY = TowerStructurePresenter.FloorY(elevator.Floor);
                var targetX = snapshot.Elevators.Count == 1 ? -1.9f : (-2.15f + i * 0.5f);

                var carTransform = _elevatorViews[i].transform;
                carTransform.position = Vector3.Lerp(carTransform.position, new Vector3(targetX, targetY, 0f), 0.25f);

                var carColor = elevator.PassengerCount > 0
                    ? new Color(0.3f, 0.95f, 0.7f)
                    : new Color(0.18f, 0.65f, 0.5f);
                SetRendererColor(_elevatorViews[i], carColor);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _shaftObjects.Count; i++)
            {
                var go = _shaftObjects[i];
                if (go != null)
                {
                    if (UnityEngine.Application.isPlaying) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                }
            }
            _shaftObjects.Clear();

            for (var i = 0; i < _elevatorViews.Count; i++)
            {
                var view = _elevatorViews[i];
                if (view != null)
                {
                    if (UnityEngine.Application.isPlaying) Object.Destroy(view.gameObject);
                    else Object.DestroyImmediate(view.gameObject);
                }
            }
            _elevatorViews.Clear();

            _shaftCavity = null;
            _shaftRailLeft = null;
            _shaftRailRight = null;
            _shaftColumnLeft = null;
            _shaftColumnRight = null;
            _shaftPenthouse = null;
            _shaftPitBuffer = null;
            _renderedShaftFloorCount = 0;
        }

        private GameObject CreateQuad(string name, Color color, Vector3 position, Vector2 size)
        {
            var renderer = CreateQuadRenderer(name, color, position, size);
            _shaftObjects.Add(renderer.gameObject);
            return renderer.gameObject;
        }

        private MeshRenderer CreateQuadRenderer(string name, Color color, Vector3 position, Vector2 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            if (_parent != null) go.transform.SetParent(_parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _worldMaterial;
            SetRendererColor(renderer, color);
            return renderer;
        }

        private void SetRendererColor(MeshRenderer renderer, Color color)
        {
            if (_colorBlock != null && renderer != null)
            {
                _colorBlock.SetColor("_BaseColor", color);
                _colorBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(_colorBlock);
            }
        }
    }
}
