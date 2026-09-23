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
                AdoptSingle($"Floor Slab {_renderedFloorCount}");
                AdoptSingle($"Floor Line L {_renderedFloorCount}");
                AdoptSingle($"Floor Line R {_renderedFloorCount}");
                _renderedFloorCount++;
            }

            AdoptSingle("Street Edge");
            AdoptSingle("Street Horizon");

        }

        public void EnsureFloorViews(TowerTopologyProjection topology)
        {
            var targetCount = topology != null ? topology.FloorCount : InitialFloorCount;

            // A presenter may be recreated while its old views remain serialized
            // in a scene (ExecuteAlways / domain reload). Ground-floor starts can
            // therefore inherit slab quads from the five-floor fixture. Retire
            // those views before ensuring the topology's actual floor count.
            while (_renderedFloorCount > targetCount)
            {
                var floor = --_renderedFloorCount;
                DestroyNamedView($"Floor Slab {floor}");
                DestroyNamedView($"Floor Line L {floor}");
                DestroyNamedView($"Floor Line R {floor}");
                _appliedSlabs.Remove(floor);
            }

            while (_renderedFloorCount < targetCount)
            {
                var floor = _renderedFloorCount;
                if (TryAdoptExisting(floor, topology))
                {
                    _renderedFloorCount++;
                    continue;
                }
                var y = FloorY(floor);

                var floorColor = FloorColor(floor);

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
                var floorLineColor = FloorLineColor;

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
                        EnsureFloorAppearance(pair.Key);
                        continue;
                    }
                    ApplyFloorGeometry(pair.Key, pair.Value);
                    _appliedSlabs[pair.Key] = pair.Value;
                }
                if (topology.TryGetFloorSlab(0, out var ground)) EnsureStreetEdge(ground);
            }
        }

        private void EnsureStreetEdge(CellBounds ground)
        {
            var right = -2.4f + (ground.MaxX + 1) * 0.5f;
            if (_parent.Find("Street Edge") == null)
                CreateQuad("Street Edge", new Color(0.18f, 0.23f, 0.29f), Vector3.zero, Vector2.one);
            if (_parent.Find("Street Horizon") == null)
                CreateQuad("Street Horizon", new Color(0.38f, 0.48f, 0.55f), Vector3.zero, Vector2.one);
            UpdateQuad("Street Edge", new Vector3(right + 1.15f, FloorY(0) - 0.72f, 0.4f),
                new Vector2(2.3f, 0.22f), new Color(0.18f, 0.23f, 0.29f));
            UpdateQuad("Street Horizon", new Vector3(right + 1.15f, FloorY(0) - 0.86f, 0.3f),
                new Vector2(2.3f, 0.04f), new Color(0.38f, 0.48f, 0.55f));
        }

        private void DestroyNamedView(string name)
        {
            if (_parent == null) return;
            var child = _parent.Find(name);
            if (child == null) return;
            _structureObjects.Remove(child.gameObject);
            _authoredObjects.Remove(child.gameObject);
            if (UnityEngine.Application.isPlaying) Object.Destroy(child.gameObject);
            else Object.DestroyImmediate(child.gameObject);
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

        /// <summary>
        /// Adopts the first quad with the given name and destroys any runtime
        /// duplicates. Duplicates accumulate when a fresh presenter meets quads
        /// it never tracked (domain reloads, Clear/Ensure cycles); extras render
        /// with empty property blocks (white) and fight geometry updates.
        /// </summary>
        private void AdoptSingle(string name)
        {
            if (_parent == null) return;
            GameObject keep = null;
            var duplicates = new List<GameObject>();
            for (var i = 0; i < _parent.childCount; i++)
            {
                var child = _parent.GetChild(i).gameObject;
                if (!child.name.Equals(name, System.StringComparison.Ordinal)) continue;
                if (keep == null) keep = child;
                else duplicates.Add(child);
            }

            if (keep != null)
            {
                _structureObjects.Add(keep);
                _authoredObjects.Add(keep);
            }
            for (var i = 0; i < duplicates.Count; i++)
            {
                var duplicate = duplicates[i];
                _structureObjects.Remove(duplicate);
                if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(duplicate);
                else UnityEngine.Object.DestroyImmediate(duplicate);
            }
        }

        /// <summary>
        /// Adopts an already-present floor quad instead of creating a duplicate
        /// when this presenter lost track of it (e.g. after Clear or a reload).
        /// </summary>
        private bool TryAdoptExisting(int floor, TowerTopologyProjection topology)
        {
            if (_parent == null || _parent.Find($"Floor Slab {floor}") == null) return false;
            AdoptSingle($"Floor Slab {floor}");
            AdoptSingle($"Floor Line L {floor}");
            AdoptSingle($"Floor Line R {floor}");
            if (topology != null && topology.TryGetFloorSlab(floor, out var slab))
            {
                _appliedSlabs[floor] = slab;
            }
            return true;
        }

        private static Color FloorColor(int floor)
        {
            return floor == 0
                ? new Color(0.11f, 0.15f, 0.22f)
                : new Color(0.09f, 0.12f, 0.18f);
        }

        private static readonly Color FloorLineColor = new Color(0.35f, 0.45f, 0.58f);

        private void EnsureFloorAppearance(int floor)
        {
            EnsureQuadAppearance($"Floor Slab {floor}", FloorColor(floor));
            EnsureQuadAppearance($"Floor Line L {floor}", FloorLineColor);
            EnsureQuadAppearance($"Floor Line R {floor}", FloorLineColor);
        }

        private void EnsureQuadAppearance(string name, Color color)
        {
            var child = _parent != null ? _parent.Find(name) : null;
            var renderer = child != null ? child.GetComponent<MeshRenderer>() : null;
            if (renderer == null || _worldMaterial == null) return;

            if (_colorBlock == null) _colorBlock = new MaterialPropertyBlock();
            _colorBlock.Clear();
            renderer.GetPropertyBlock(_colorBlock);
            var existingColor = _colorBlock.GetColor("_BaseColor");
            if (renderer.sharedMaterial == _worldMaterial && ColorsMatch(existingColor, color)) return;

            renderer.sharedMaterial = _worldMaterial;
            _colorBlock.SetColor("_BaseColor", color);
            _colorBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(_colorBlock);
        }

        private static bool ColorsMatch(Color a, Color b)
        {
            const float epsilon = 0.01f;
            return Mathf.Abs(a.r - b.r) < epsilon && Mathf.Abs(a.g - b.g) < epsilon &&
                   Mathf.Abs(a.b - b.b) < epsilon && Mathf.Abs(a.a - b.a) < epsilon;
        }

        private void ApplyFloorGeometry(int floor, CellBounds slab)
        {
            var worldLeft = -2.4f + slab.MinX * 0.5f;
            var worldRight = -2.4f + (slab.MaxX + 1) * 0.5f;
            var width = worldRight - worldLeft;
            var centerX = (worldLeft + worldRight) * 0.5f;
            var floorY = FloorY(floor);
            UpdateQuad($"Floor Slab {floor}", new Vector3(centerX, floorY, 1f), new Vector2(width, 1.55f), FloorColor(floor));
            const float shaftLeft = -2.40f;
            const float shaftRight = -1.40f;
            var lineY = floorY - 0.74f;
            UpdateQuad($"Floor Line L {floor}", new Vector3((worldLeft + shaftLeft) * 0.5f, lineY, 0.05f), new Vector2(Mathf.Max(0f, shaftLeft - worldLeft), 0.05f), FloorLineColor);
            UpdateQuad($"Floor Line R {floor}", new Vector3((shaftRight + worldRight) * 0.5f, lineY, 0.05f), new Vector2(Mathf.Max(0f, worldRight - shaftRight), 0.05f), FloorLineColor);
        }

        private void UpdateQuad(string name, Vector3 position, Vector2 size, Color color)
        {
            var child = _parent != null ? _parent.Find(name) : null;
            if (child == null) return;
            child.gameObject.SetActive(size.x > 0f);
            child.localPosition = position;
            child.localScale = new Vector3(size.x, size.y, 1f);
            // Heal stale quads (e.g. authored or forgotten duplicates) that never
            // received a property block: without it they render the white base material.
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null && _worldMaterial != null)
            {
                renderer.sharedMaterial = _worldMaterial;
                if (_colorBlock != null)
                {
                    _colorBlock.SetColor("_BaseColor", color);
                    _colorBlock.SetColor("_Color", color);
                    renderer.SetPropertyBlock(_colorBlock);
                }
            }
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
