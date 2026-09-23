using System;
using System.Collections.Generic;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Collider-free cutaway city beyond the ground-floor lobby, with three camera-depth layers.</summary>
    [DisallowMultipleComponent]
    public sealed class OutsideCityPresenter : MonoBehaviour
    {
        private static readonly float[][] Widths =
        {
            new[] { 3.6f, 2.4f, 3.1f, 2.7f, 3.9f, 2.3f, 3.4f, 2.8f, 3.2f },
            new[] { 2.8f, 3.2f, 2.5f, 3.6f, 2.7f, 3.1f, 2.8f, 3.5f, 2.6f },
            new[] { 3.2f, 2.7f, 3.5f, 2.9f, 3.4f, 2.8f, 3.7f, 3.0f }
        };

        private static readonly float[][] Heights =
        {
            new[] { 7.2f, 10.0f, 8.1f, 11.6f, 6.7f, 9.3f, 12.0f, 7.9f, 10.2f },
            new[] { 4.1f, 6.8f, 5.3f, 8.2f, 4.8f, 7.2f, 5.9f, 8.7f, 5.4f },
            new[] { 2.5f, 3.6f, 2.9f, 4.5f, 3.2f, 4.1f, 3.5f, 4.8f }
        };

        private static readonly float[] Parallax = { 0.08f, 0.20f, 0.36f };
        private static readonly float[] Depth = { 8f, 6f, 4f };
        private const float SkylineHeightScale = 0.42f;
        private const string RootName = "Outside City View";

        private readonly Transform[] _layers = new Transform[3];
        private readonly List<MeshRenderer>[] _facades =
        {
            new List<MeshRenderer>(), new List<MeshRenderer>(), new List<MeshRenderer>()
        };
        private readonly List<MeshRenderer> _windows = new List<MeshRenderer>();
        private MaterialPropertyBlock _block;
        private Camera _camera;
        private Material _material;
        private Mesh _quad;
        private Transform _root;
        private int _groundMaxX = int.MinValue;
        private int _floorCount = -1;
        private float _cameraOriginX;
        private int _lastLightBucket = int.MinValue;

        public int LayerCount => _root == null ? 0 : _layers.Length;
        public int FacadeCount => _facades[0].Count + _facades[1].Count + _facades[2].Count;
        public int WindowCount => _windows.Count;
        public IReadOnlyList<Transform> Layers => _layers;
        public IReadOnlyList<MeshRenderer> Windows => _windows;

        public void Initialize(Camera camera, Material material)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (material == null) throw new ArgumentNullException(nameof(material));
            _camera = camera;
            _material = material;
        }

        public void SyncGround(CellBounds ground, int floorCount)
        {
            if (_camera == null || _material == null) return;
            if (_root != null && _groundMaxX == ground.MaxX && _floorCount == floorCount) return;

            Clear();
            _groundMaxX = ground.MaxX;
            _floorCount = floorCount;
            _cameraOriginX = _camera.transform.position.x;
            _quad = CreateQuadMesh();
            _root = new GameObject(RootName).transform;
            _root.SetParent(transform, false);

            var edgeX = -2.4f + (ground.MaxX + 1) * 0.5f;
            var streetY = TowerStructurePresenter.FloorY(0) - 0.68f;
            for (var layer = 0; layer < _layers.Length; layer++)
            {
                var root = new GameObject(layer == 0 ? "Distant Skyline" : layer == 1 ? "Middle Skyline" : "Street Skyline").transform;
                root.SetParent(_root, false);
                _layers[layer] = root;
                root.localPosition = new Vector3(edgeX, 0f, 0f);
                BuildLayer(root, layer, streetY);
            }
            UpdateParallax();
            UpdateLighting(DayClock.FromTick(0));
        }

        private void BuildLayer(Transform layerRoot, int layer, float streetY)
        {
            var x = -6f;
            for (var i = 0; i < Widths[layer].Length; i++)
            {
                var width = Widths[layer][i];
                var height = Heights[layer][i] * SkylineHeightScale;
                var left = x;
                var centerX = left + width * 0.5f;
                var facade = CreateQuad(layerRoot, $"Building {i + 1}", centerX, streetY + height * 0.5f,
                    width, height, Depth[layer], NightFacadeColor(layer));
                _facades[layer].Add(facade);

                // Roofline details make each silhouette readable at overview zoom.
                if (i % 3 == 1)
                    CreateQuad(layerRoot, $"Roof crown {i + 1}", centerX, streetY + height + 0.07f,
                        width * 0.62f, 0.14f, Depth[layer] - 0.03f, RoofColor(layer));
                if (i % 4 == 2)
                    CreateQuad(layerRoot, $"Antenna {i + 1}", centerX + width * 0.18f,
                        streetY + height + 0.26f, 0.04f, 0.52f, Depth[layer] - 0.04f, RoofColor(layer));

                if (layer > 0)
                {
                    var columns = Mathf.Max(1, Mathf.FloorToInt((width - 0.45f) / 0.65f));
                    var rows = Mathf.Min(7, Mathf.FloorToInt((height - 0.5f) / 0.82f));
                    for (var row = 0; row < rows; row++)
                    {
                        for (var column = 0; column < columns; column++)
                        {
                            // Deterministic patchwork avoids a uniform glowing grid.
                            if ((i * 7 + row * 3 + column * 5) % 5 == 0) continue;
                            var windowX = left + 0.40f + column * 0.65f;
                            var windowY = streetY + 0.58f + row * 0.82f;
                            var window = CreateQuad(layerRoot, $"Window {i}-{row}-{column}", windowX,
                                windowY, 0.19f, 0.25f, Depth[layer] - 0.08f, new Color(1f, 0.70f, 0.34f));
                            _windows.Add(window);
                        }
                    }
                }
                x += width - 0.08f;
            }

            if (layer == 2)
            {
                // The street plane meets the lobby edge; storefront strips and lamps lend scale.
                CreateQuad(layerRoot, "Street apron", 11f, streetY - 0.20f, 36f, 0.18f, 3.8f,
                    new Color(0.18f, 0.25f, 0.30f));
                for (var i = 0; i < 5; i++)
                {
                    var lampX = 3f + i * 6.5f;
                    CreateQuad(layerRoot, $"Lamp post {i}", lampX, streetY + 0.42f, 0.055f, 1.18f,
                        3.7f, new Color(0.28f, 0.37f, 0.43f));
                    _windows.Add(CreateQuad(layerRoot, $"Lamp light {i}", lampX, streetY + 1.03f,
                        0.30f, 0.12f, 3.65f, new Color(1f, 0.70f, 0.34f)));
                }
            }
        }

        public void UpdateLighting(DayPhase phase)
        {
            if (_root == null) return;
            var bucket = phase.DayNumber * 48 + phase.Hour * 2 + (phase.Minute >= 30 ? 1 : 0);
            if (bucket == _lastLightBucket) return;
            _lastLightBucket = bucket;
            var hour = phase.Hour + phase.Minute / 60f;
            var night = hour < 6f ? Mathf.Clamp01((7f - hour) / 2f) :
                hour >= 18f ? Mathf.Clamp01((hour - 18f) / 2f) : 0f;
            var dayFacades = new[]
            {
                new Color(0.24f, 0.37f, 0.48f), new Color(0.18f, 0.30f, 0.39f), new Color(0.13f, 0.23f, 0.31f)
            };
            for (var layer = 0; layer < _facades.Length; layer++)
            {
                var color = Color.Lerp(dayFacades[layer], NightFacadeColor(layer), night);
                foreach (var facade in _facades[layer]) SetColor(facade, color);
            }
            var windowColor = Color.Lerp(new Color(0.24f, 0.36f, 0.43f),
                new Color(1f, 0.70f, 0.34f), night);
            foreach (var window in _windows) SetColor(window, windowColor);
        }

        public void UpdateParallax()
        {
            if (_root == null || _camera == null) return;
            var edgeX = -2.4f + (_groundMaxX + 1) * 0.5f;
            var displacement = _camera.transform.position.x - _cameraOriginX;
            for (var i = 0; i < _layers.Length; i++)
                if (_layers[i] != null) _layers[i].localPosition = new Vector3(edgeX + displacement * Parallax[i], 0f, 0f);
        }

        private void LateUpdate() => UpdateParallax();
        private void OnDestroy() => Clear();

        public void Clear()
        {
            // ExecuteAlways can lose its managed references across a domain reload while
            // its generated children remain in the scene. Remove every owned root.
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != RootName) continue;
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                DestroyOwned(child.gameObject);
            }
            if (_quad != null) DestroyOwned(_quad);
            _root = null;
            _quad = null;
            for (var i = 0; i < _layers.Length; i++)
            {
                _layers[i] = null;
                _facades[i].Clear();
            }
            _windows.Clear();
            _groundMaxX = int.MinValue;
            _floorCount = -1;
            _lastLightBucket = int.MinValue;
        }

        private MeshRenderer CreateQuad(Transform parent, string name, float x, float y,
            float width, float height, float z, Color color)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localScale = new Vector3(width, height, 1f);
            go.GetComponent<MeshFilter>().sharedMesh = _quad;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            SetColor(renderer, color);
            return renderer;
        }

        private void SetColor(MeshRenderer renderer, Color color)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            _block.Clear();
            _block.SetColor("_BaseColor", color);
            _block.SetColor("_Color", color);
            renderer.SetPropertyBlock(_block);
        }

        private static Color RoofColor(int layer) => layer == 0
            ? new Color(0.20f, 0.29f, 0.39f)
            : layer == 1 ? new Color(0.15f, 0.25f, 0.34f) : new Color(0.10f, 0.19f, 0.27f);

        private static Color NightFacadeColor(int layer) => layer == 0
            ? new Color(0.11f, 0.18f, 0.29f)
            : layer == 1 ? new Color(0.09f, 0.15f, 0.25f) : new Color(0.07f, 0.11f, 0.19f);

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh { name = "Outside City Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f),
                new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            // The tower camera looks toward +Z; front faces must point toward -Z.
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
