using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Topology;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Deep presenter responsible for structural floor slab quads and baseline lines in the cutaway view.
    /// </summary>
    public sealed class TowerStructurePresenter
    {
        public const float DefaultFloorHeight = 1.75f;
        public const float BaseFloorY = -3.2f;
        public const int InitialFloorCount = 5;

        private Transform _parent;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;
        private readonly List<GameObject> _structureObjects = new List<GameObject>();
        private readonly HashSet<GameObject> _authoredObjects = new HashSet<GameObject>();
        private readonly Dictionary<int, CellBounds> _appliedSlabs = new Dictionary<int, CellBounds>();
        private int _renderedFloorCount;

        public int RenderedFloorCount => _renderedFloorCount;
        public IReadOnlyList<GameObject> StructureObjects => _structureObjects;

        public static float FloorY(int floor) => BaseFloorY + floor * DefaultFloorHeight;

        public void Initialize(Transform parent, Material worldMaterial, MaterialPropertyBlock colorBlock)
        {
            _parent = parent;
            _worldMaterial = worldMaterial;
            _colorBlock = colorBlock;
            _renderedFloorCount = 0;
            _structureObjects.Clear();
            _authoredObjects.Clear();
            _appliedSlabs.Clear();
            while (_parent != null && _parent.Find($"Floor Slab {_renderedFloorCount}") != null)
            {
                Adopt($"Floor Slab {_renderedFloorCount}");
                Adopt($"Floor Line L {_renderedFloorCount}");
                Adopt($"Floor Line R {_renderedFloorCount}");
                _renderedFloorCount++;
            }

        }

        public void EnsureFloorViews(TowerTopologyProjection topology)
        {
            var targetCount = topology != null ? topology.FloorCount : InitialFloorCount;

            while (_renderedFloorCount < targetCount)
            {
                var floor = _renderedFloorCount;
                var y = FloorY(floor);
                var isLobby = floor == 0;

                var floorColor = isLobby
                    ? new Color(0.11f, 0.15f, 0.22f)
                    : new Color(0.09f, 0.12f, 0.18f);

                float minX = -14f, maxX = 16f;
                if (topology != null && topology.TryGetFloorSlab(floor, out var slab))
                {
                    minX = slab.MinX;
                    maxX = slab.MaxX;
                }

                var worldLeft = -2.4f + minX * 0.5f;
                var worldRight = -2.4f + (maxX + 1) * 0.5f;
                var width = worldRight - worldLeft;
                var centerX = (worldLeft + worldRight) * 0.5f;

                // Main floor slab background
                var slabRenderer = CreateQuad($"Floor Slab {floor}", floorColor, new Vector3(centerX, y, 1f), new Vector2(width, 1.55f));
                slabRenderer.gameObject.AddComponent<VisualEffectsPresenter>().BeginConstruction();

                // Floor baseline dividers - split around elevator shaft [-2.40f, -1.40f] so the shaft remains an open vertical chute
                const float shaftLeft = -2.40f;
                const float shaftRight = -1.40f;
                var floorLineY = y - 0.74f;
                var floorLineColor = new Color(0.35f, 0.45f, 0.58f);

                if (shaftLeft > worldLeft)
                {
                    var leftSegWidth = shaftLeft - worldLeft;
                    var leftSegCenter = (worldLeft + shaftLeft) * 0.5f;
                    CreateQuad($"Floor Line L {floor}", floorLineColor, new Vector3(leftSegCenter, floorLineY, 0.05f), new Vector2(leftSegWidth, 0.05f));
                }

                if (worldRight > shaftRight)
                {
                    var rightSegWidth = worldRight - shaftRight;
                    var rightSegCenter = (shaftRight + worldRight) * 0.5f;
                    CreateQuad($"Floor Line R {floor}", floorLineColor, new Vector3(rightSegCenter, floorLineY, 0.05f), new Vector2(rightSegWidth, 0.05f));
                }

                if (topology != null && topology.TryGetFloorSlab(floor, out var builtSlab))
                {
                    _appliedSlabs[floor] = builtSlab;
                }
                else
                {
                    _appliedSlabs[floor] = new CellBounds(floor, (int)minX, (int)maxX);
                }

                _renderedFloorCount++;
            }

            if (topology != null)
            {
                // Only rewrite quads whose slab bounds actually changed (e.g.
                // ground expansion). Unconditional rewrites fight the
                // construction/dissolve scale animations on every Ensure call.
                foreach (var pair in topology.FloorSlabs)
                {
                    if (_appliedSlabs.TryGetValue(pair.Key, out var applied) && applied.Equals(pair.Value))
                    {
                        continue;
                    }
                    ApplyFloorGeometry(pair.Key, pair.Value);
                    _appliedSlabs[pair.Key] = pair.Value;
                }
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _structureObjects.Count; i++)
            {
                var go = _structureObjects[i];
                if (go != null && !_authoredObjects.Contains(go))
                {
                    if (UnityEngine.Application.isPlaying) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                }
            }

            _structureObjects.Clear();
            _authoredObjects.Clear();
            _appliedSlabs.Clear();
            _renderedFloorCount = 0;
        }

        private void Adopt(string name)
        {
            var child = _parent.Find(name);
            if (child == null) return;
            _structureObjects.Add(child.gameObject);
            _authoredObjects.Add(child.gameObject);
        }

        private void ApplyFloorGeometry(int floor, CellBounds slab)
        {
            var worldLeft = -2.4f + slab.MinX * 0.5f;
            var worldRight = -2.4f + (slab.MaxX + 1) * 0.5f;
            var width = worldRight - worldLeft;
            var centerX = (worldLeft + worldRight) * 0.5f;
            var floorY = FloorY(floor);
            UpdateQuad($"Floor Slab {floor}", new Vector3(centerX, floorY, 1f), new Vector2(width, 1.55f));
            const float shaftLeft = -2.40f;
            const float shaftRight = -1.40f;
            var lineY = floorY - 0.74f;
            UpdateQuad($"Floor Line L {floor}", new Vector3((worldLeft + shaftLeft) * 0.5f, lineY, 0.05f), new Vector2(Mathf.Max(0f, shaftLeft - worldLeft), 0.05f));
            UpdateQuad($"Floor Line R {floor}", new Vector3((shaftRight + worldRight) * 0.5f, lineY, 0.05f), new Vector2(Mathf.Max(0f, worldRight - shaftRight), 0.05f));
        }

        private void UpdateQuad(string name, Vector3 position, Vector2 size)
        {
            var child = _parent != null ? _parent.Find(name) : null;
            if (child == null) return;
            child.gameObject.SetActive(size.x > 0f);
            child.localPosition = position;
            child.localScale = new Vector3(size.x, size.y, 1f);
        }

        private MeshRenderer CreateQuad(string name, Color color, Vector3 position, Vector2 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            if (_parent != null) go.transform.SetParent(_parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _worldMaterial;
            if (_colorBlock != null)
            {
                _colorBlock.SetColor("_BaseColor", color);
                _colorBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(_colorBlock);
            }

            _structureObjects.Add(go);
            return renderer;
        }
    }
}
