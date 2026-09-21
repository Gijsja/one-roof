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
        private readonly HashSet<GameObject> _authoredObjects = new HashSet<GameObject>();

        public IReadOnlyList<MeshRenderer> ElevatorViews => _elevatorViews;
        public int RenderedShaftFloorCount => _renderedShaftFloorCount;

        public Bounds GetShaftBounds()
        {
            var bottomY = TowerStructurePresenter.FloorY(_renderedMinFloor) - 0.74f;
            var topY = TowerStructurePresenter.FloorY(_renderedMaxFloor) + 0.74f;
            var shaftHeight = Mathf.Max(1.48f, topY - bottomY);
            var centerY = (bottomY + topY) * 0.5f;
            return new Bounds(new Vector3(-1.90f, centerY, 0f), new Vector3(1.06f, shaftHeight, 1f));
        }

        public bool IsPointerInShaft(Vector2 worldPos, out Bounds bounds)
        {
            bounds = GetShaftBounds();
            return worldPos.x >= bounds.min.x && worldPos.x <= bounds.max.x &&
                   worldPos.y >= bounds.min.y && worldPos.y <= bounds.max.y;
        }

        private int _renderedMinFloor;
        private int _renderedMaxFloor;

        public static void CalculateCarLayout(int carIndex, int totalCars, out float x, out float width)
        {
            const float shaftInternalLeft = -2.32f;
            const float shaftInternalRight = -1.48f;
            const float shaftWidth = shaftInternalRight - shaftInternalLeft; // 0.84f

            if (totalCars <= 1)
            {
                x = -1.90f;
                width = 0.48f;
                return;
            }

            var gap = 0.03f;
            var totalGaps = (totalCars - 1) * gap;
            width = Mathf.Clamp((shaftWidth - totalGaps) / totalCars, 0.16f, 0.44f);
            var actualSpan = totalCars * width + totalGaps;
            var startX = -1.90f - actualSpan * 0.5f + width * 0.5f;
            x = startX + carIndex * (width + gap);
        }

        public void Initialize(Transform parent, Material worldMaterial, MaterialPropertyBlock colorBlock)
        {
            _parent = parent;
            _worldMaterial = worldMaterial;
            _colorBlock = colorBlock;
            _renderedShaftFloorCount = 0;
            _renderedMinFloor = 0;
            _renderedMaxFloor = 0;
            _shaftObjects.Clear();
            _elevatorViews.Clear();
            _authoredObjects.Clear();
            _shaftCavity = Adopt("Elevator Shaft Cavity");
            _shaftRailLeft = Adopt("Shaft Rail Left");
            _shaftRailRight = Adopt("Shaft Rail Right");
            _shaftColumnLeft = Adopt("Shaft Column Left");
            _shaftColumnRight = Adopt("Shaft Column Right");
            _shaftPenthouse = Adopt("Shaft Penthouse Cap");
            _shaftPitBuffer = Adopt("Shaft Pit Buffer");
            for (var index = 0; _parent != null; index++)
            {
                var child = _parent.Find($"Elevator Car {index}");
                if (child == null) break;
                var renderer = child.GetComponent<MeshRenderer>();
                if (renderer == null) break;
                _elevatorViews.Add(renderer);
                _authoredObjects.Add(child.gameObject);
            }
        }

        public void EnsureShaftViews(int floorCount) => EnsureShaftViews(0, Mathf.Max(0, floorCount - 1));

        public void EnsureShaftViews(int minFloor, int maxFloor)
        {
            if (minFloor > maxFloor) maxFloor = minFloor;
            var bottomY = TowerStructurePresenter.FloorY(minFloor) - 0.74f;
            var topY = TowerStructurePresenter.FloorY(maxFloor) + 0.74f;
            var shaftHeight = topY - bottomY;
            var centerY = (bottomY + topY) * 0.5f;

            const float shaftCenterX = -1.90f;
            const float shaftWidth = 1.00f;
            const float colLeftX = -2.40f;
            const float colRightX = -1.40f;
            const float railLeftX = -2.36f;
            const float railRightX = -1.44f;

            var penthouseColor = new Color(0.38f, 0.48f, 0.62f);
            var pitBufferColor = new Color(0.28f, 0.36f, 0.48f);

            if (_shaftCavity == null)
            {
                _shaftCavity = CreateQuad("Elevator Shaft Cavity", new Color(0.04f, 0.06f, 0.09f), new Vector3(shaftCenterX, centerY, 0.85f), new Vector2(shaftWidth, shaftHeight));
                _shaftRailLeft = CreateQuad("Shaft Rail Left", new Color(0.48f, 0.58f, 0.72f), new Vector3(railLeftX, centerY, 0.2f), new Vector2(0.035f, shaftHeight));
                _shaftRailRight = CreateQuad("Shaft Rail Right", new Color(0.48f, 0.58f, 0.72f), new Vector3(railRightX, centerY, 0.2f), new Vector2(0.035f, shaftHeight));
                _shaftColumnLeft = CreateQuad("Shaft Column Left", new Color(0.35f, 0.45f, 0.58f), new Vector3(colLeftX, centerY, 0.15f), new Vector2(0.06f, shaftHeight));
                _shaftColumnRight = CreateQuad("Shaft Column Right", new Color(0.35f, 0.45f, 0.58f), new Vector3(colRightX, centerY, 0.15f), new Vector2(0.06f, shaftHeight));
                _shaftPenthouse = CreateQuad("Shaft Penthouse Cap", penthouseColor, new Vector3(shaftCenterX, topY + 0.08f, 0.1f), new Vector2(1.12f, 0.16f));
                _shaftPitBuffer = CreateQuad("Shaft Pit Buffer", pitBufferColor, new Vector3(shaftCenterX, bottomY - 0.08f, 0.1f), new Vector2(1.12f, 0.16f));
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

                SetRendererColor(_shaftPenthouse.GetComponent<MeshRenderer>(), penthouseColor);
                SetRendererColor(_shaftPitBuffer.GetComponent<MeshRenderer>(), pitBufferColor);
            }

            _renderedMinFloor = minFloor;
            _renderedMaxFloor = maxFloor;
            _renderedShaftFloorCount = maxFloor - minFloor + 1;
        }

        public void EnsureElevatorViews(int targetCount)
        {
            while (_elevatorViews.Count < targetCount)
            {
                var index = _elevatorViews.Count;
                CalculateCarLayout(index, targetCount, out var x, out var carWidth);
                var view = CreateQuadRenderer($"Elevator Car {index}", new Color(0.25f, 0.92f, 0.65f), new Vector3(x, TowerStructurePresenter.FloorY(0), 0f), new Vector2(carWidth, 0.5f));
                _elevatorViews.Add(view);
            }

            for (var i = 0; i < _elevatorViews.Count; i++)
            {
                CalculateCarLayout(i, _elevatorViews.Count, out _, out var carWidth);
                if (_elevatorViews[i] != null)
                {
                    _elevatorViews[i].transform.localScale = new Vector3(carWidth, 0.5f, 1f);
                }
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
                CalculateCarLayout(i, snapshot.Elevators.Count, out var targetX, out var carWidth);

                var carTransform = _elevatorViews[i].transform;
                carTransform.localScale = new Vector3(carWidth, 0.5f, 1f);
                carTransform.position = Vector3.Lerp(carTransform.position, new Vector3(targetX, targetY, 0f), 0.25f);

                var carColor = elevator.PassengerCount > 0
                    ? new Color(0.3f, 0.95f, 0.7f)
                    : new Color(0.18f, 0.65f, 0.5f);
                SetRendererColor(_elevatorViews[i], carColor);

                // A nearly-full car is a visible crowding symptom; the pulse is deliberately
                // presentation-only and derives solely from the immutable projection.
                var severity = elevator.Capacity > 0
                    ? Mathf.Clamp01(((float)elevator.PassengerCount / elevator.Capacity - 0.65f) / 0.35f)
                    : 0f;
                var effects = _elevatorViews[i].GetComponent<VisualEffectsPresenter>()
                    ?? _elevatorViews[i].gameObject.AddComponent<VisualEffectsPresenter>();
                effects.SetAgitation(severity);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _shaftObjects.Count; i++)
            {
                var go = _shaftObjects[i];
                if (go != null && !_authoredObjects.Contains(go))
                {
                    if (UnityEngine.Application.isPlaying) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                }
            }
            _shaftObjects.Clear();

            for (var i = 0; i < _elevatorViews.Count; i++)
            {
                var view = _elevatorViews[i];
                if (view != null && !_authoredObjects.Contains(view.gameObject))
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
            _renderedMinFloor = 0;
            _renderedMaxFloor = 0;
            _authoredObjects.Clear();
        }

        private void CleanupOrphanedObjects()
        {
            if (_parent == null) return;
            var toDestroy = new List<GameObject>();
            for (var i = 0; i < _parent.childCount; i++)
            {
                var child = _parent.GetChild(i).gameObject;
                if (child == null) continue;
                if (child.name.StartsWith("Elevator Shaft") ||
                    child.name.StartsWith("Shaft Rail") ||
                    child.name.StartsWith("Shaft Column") ||
                    child.name.StartsWith("Shaft Penthouse") ||
                    child.name.StartsWith("Shaft Pit") ||
                    child.name.StartsWith("Elevator Car"))
                {
                    // If not in tracked lists, destroy orphan
                    if (!_shaftObjects.Contains(child) && !_elevatorViews.Exists(v => v != null && v.gameObject == child))
                    {
                        toDestroy.Add(child);
                    }
                }
            }
            foreach (var go in toDestroy)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(go);
                else Object.DestroyImmediate(go);
            }
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

        private GameObject Adopt(string name)
        {
            var child = _parent != null ? _parent.Find(name) : null;
            if (child == null) return null;
            _shaftObjects.Add(child.gameObject);
            _authoredObjects.Add(child.gameObject);
            return child.gameObject;
        }
    }
}
