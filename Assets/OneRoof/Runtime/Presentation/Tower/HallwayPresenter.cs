using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Topology;
using EntityId = OneRoof.Domain.Identity.EntityId;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Foreground circulation tread, drawn in one mesh behind walking Residents.</summary>
    public sealed class HallwayPresenter
    {
        private GameObject _root;
        private Mesh _mesh;
        private readonly Dictionary<int, CellBounds> _slabs = new Dictionary<int, CellBounds>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private bool _visible = true;
        private GameObject _frontageRoot;
        private Mesh _frontageMesh;
        private Material _frontageMaterial;
        private MeshRenderer _frontageRenderer;
        private MeshRenderer _treadRenderer;
        private MaterialPropertyBlock _frontageProperties;
        private readonly Dictionary<EntityId, Room> _frontageRooms = new Dictionary<EntityId, Room>();
        private readonly Dictionary<EntityId, HallwayRoomAccess> _access = new Dictionary<EntityId, HallwayRoomAccess>();
        private readonly List<Vector3> _frontageVertices = new List<Vector3>();
        private readonly List<Color> _frontageColors = new List<Color>();
        private readonly List<int> _frontageTriangles = new List<int>();
        private float _wallAlpha = 1f;
        private TowerTopologyProjection _frontageTopology;
        public float WallAlpha => _wallAlpha;
        public int RenderedRoomAccessCount => _access.Count;
        public int FrontageCount => _access.Count;
        public int NumberedDoorCount => _access.Count;
        public int OpenBayCount { get; private set; }
        public Mesh FrontageMesh => _frontageMesh;
        public Material FrontageMaterial => _frontageMaterial;
        public bool TryGetRoomAccess(EntityId id, out HallwayRoomAccess access) => _access.TryGetValue(id, out access);

        public readonly struct HallwayRoomAccess
        {
            public HallwayRoomAccess(EntityId roomId, float doorX, Rect windowBounds, bool diner)
            {
                RoomId = roomId; DoorX = doorX; WindowBounds = windowBounds; HasDinerOpening = diner;
            }
            public EntityId RoomId { get; }
            public string Number => RoomId.Value.ToString();
            public float DoorX { get; }
            public Rect WindowBounds { get; }
            public bool HasDinerOpening { get; }
        }

        public void SetWallAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (Mathf.Approximately(_wallAlpha, alpha)) return;
            _wallAlpha = alpha;
            ApplyWallAlpha();
        }

        private void ApplyWallAlpha()
        {
            if (_frontageRenderer == null) return;
            _frontageProperties.SetColor("_BaseColor", new Color(1f, 1f, 1f, _wallAlpha));
            _frontageProperties.SetColor("_Color", new Color(1f, 1f, 1f, _wallAlpha));
            _frontageRenderer.SetPropertyBlock(_frontageProperties);
            if (_treadRenderer != null) _treadRenderer.SetPropertyBlock(_frontageProperties);
        }

        public int RenderedFloorCount => _slabs.Count;

        public void Initialize(Transform parent, Material material, MaterialPropertyBlock block)
        {
            Clear();
            _frontageProperties ??= new MaterialPropertyBlock();
            _root = new GameObject("Hallway Circulation");
            _root.transform.SetParent(parent, false);
            _root.AddComponent<MeshFilter>();
            var renderer = _root.AddComponent<MeshRenderer>();
            _treadRenderer = renderer;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var properties = block ?? new MaterialPropertyBlock();
            properties.Clear();
            properties.SetColor("_BaseColor", Color.white);
            properties.SetColor("_Color", Color.white);
            properties.SetTexture("_BaseMap", Texture2D.whiteTexture);
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            renderer.SetPropertyBlock(properties);
            _frontageRoot = new GameObject("Hallway Room Frontages");
            _frontageRoot.transform.SetParent(_root.transform, false);
            _frontageRoot.AddComponent<MeshFilter>();
            _frontageRenderer = _frontageRoot.AddComponent<MeshRenderer>();
            _frontageMaterial = new Material(material) { name = "Hallway Frontage Fade Material" };
            _frontageMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _frontageMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _frontageMaterial.SetFloat("_ZWrite", 0f);
            _frontageMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            _frontageRenderer.sharedMaterial = _frontageMaterial;
            _treadRenderer.sharedMaterial = _frontageMaterial;
            _frontageRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _frontageRenderer.receiveShadows = false;
            _frontageProperties.Clear();
            _frontageProperties.SetTexture("_BaseMap", Texture2D.whiteTexture);
            _frontageProperties.SetTexture("_MainTex", Texture2D.whiteTexture);
            ApplyWallAlpha();
            _root.SetActive(_visible);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_root != null) _root.SetActive(visible);
        }

        public void EnsureHallways(TowerTopologyProjection topology)
        {
            if (_root == null) return;
            var count = topology != null ? topology.FloorSlabs.Count : 0;
            var changed = count != _slabs.Count;
            if (!changed && topology != null)
                foreach (var pair in topology.FloorSlabs)
                    if (!_slabs.TryGetValue(pair.Key, out var old) || !old.Equals(pair.Value))
                    {
                        changed = true;
                        break;
                    }
            EnsureRoomFrontages(topology);
            if (!changed) return;
            _slabs.Clear();
            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();
            _uvs.Clear();
            if (topology != null)
                foreach (var pair in topology.FloorSlabs)
                {
                    _slabs.Add(pair.Key, pair.Value);
                    // The undercity has its own explicit corridor presentation.
                    if (pair.Key < 0) continue;
                    var left = -2.4f + pair.Value.MinX * 0.5f;
                    var right = -2.4f + (pair.Value.MaxX + 1) * 0.5f;
                    var baseline = TowerStructurePresenter.FloorY(pair.Key) - 0.74f;
                    AddQuad(left, right, baseline, baseline + 0.20f, -0.16f, new Color(0.56f, 0.53f, 0.44f));
                    AddQuad(left, right, baseline + 0.18f, baseline + 0.21f, -0.17f, new Color(0.25f, 0.39f, 0.39f));
                    AddQuad(left, right, baseline, baseline + 0.025f, -0.175f, new Color(0.76f, 0.69f, 0.51f));
                    // Sparse joints establish a horizontal walkway without concealing room interiors.
                    for (var x = left + 1f; x < right; x += 1f)
                        AddQuad(x, Mathf.Min(right, x + 0.012f), baseline + 0.025f, baseline + 0.18f, -0.18f, new Color(0.44f, 0.43f, 0.37f));
                }
            if (_mesh == null) _mesh = new Mesh { name = "Hallway Circulation Mesh" };
            _mesh.Clear();
            _mesh.indexFormat = _vertices.Count > 65534 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.SetColors(_colors);
            _mesh.SetUVs(0, _uvs);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _root.GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        private static bool HasFrontage(Room room) => room.Floor >= 0 &&
            !room.ContentType.Value.StartsWith("transit:");

        private void EnsureRoomFrontages(TowerTopologyProjection topology)
        {
            var count = 0;
            var changed = !ReferenceEquals(_frontageTopology, topology);
            if (topology != null)
                foreach (var room in topology.Rooms.Values)
                {
                    if (!HasFrontage(room)) continue;
                    count++;
                    if (!_frontageRooms.TryGetValue(room.Id, out var old) || !old.Bounds.Equals(room.Bounds) ||
                        old.ContentType.Value != room.ContentType.Value || !ReferenceEquals(old, room)) changed = true;
                }
            if (!changed && count == _frontageRooms.Count) return;
            _frontageTopology = topology;
            _frontageRooms.Clear(); _access.Clear();
            OpenBayCount = 0;
            _frontageVertices.Clear(); _frontageColors.Clear(); _frontageTriangles.Clear();
            if (topology != null)
                foreach (var room in topology.Rooms.Values)
                {
                    if (!HasFrontage(room)) continue;
                    _frontageRooms.Add(room.Id, room);
                    BuildRoomFrontage(room, topology);
                }
            if (_frontageMesh == null) _frontageMesh = new Mesh { name = "Hallway Room Frontage Geometry" };
            _frontageMesh.Clear();
            _frontageMesh.indexFormat = _frontageVertices.Count > 65534 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            _frontageMesh.SetVertices(_frontageVertices);
            _frontageMesh.SetColors(_frontageColors);
            _frontageMesh.SetTriangles(_frontageTriangles, 0);
            _frontageMesh.RecalculateBounds();
            _frontageRoot.GetComponent<MeshFilter>().sharedMesh = _frontageMesh;
        }

        private void BuildRoomFrontage(Room room, TowerTopologyProjection topology)
        {
            var left = -2.4f + room.Bounds.MinX * 0.5f;
            var right = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
            var bottom = TowerStructurePresenter.FloorY(room.Floor) - 0.54f;
            var width = right - left;
            var doorWidth = Mathf.Min(0.44f, width * 0.30f);
            var doorX = left + width * 0.22f;
            // Bind to a real room portal when supplied; old projection fixtures retain a sensible fallback.
            if (topology.TryGetRoomEntrance(room.Id, out var entrance))
                doorX = Mathf.Clamp(-2.4f + entrance.X * 0.5f + 0.25f,
                    left + doorWidth * 0.5f + 0.04f, right - doorWidth * 0.5f - 0.04f);
            var door = new Rect(doorX - doorWidth * 0.5f, bottom, doorWidth, 0.91f);
            var diner = room.ContentType.Value == "commercial:diner";
            if (diner) OpenBayCount++;
            // Choose the larger side of the actual entrance for the viewing bay.
            var roomOnRight = right - door.xMax >= door.xMin - left;
            var bayLeft = roomOnRight ? door.xMax + 0.09f : left + 0.08f;
            var bayRight = roomOnRight ? right - 0.08f : door.xMin - 0.09f;
            var bayWidth = Mathf.Max(0.04f, bayRight - bayLeft);
            var window = new Rect(bayLeft, bottom + (diner ? 0.08f : 0.38f), bayWidth, diner ? 0.94f : 0.48f);
            _access.Add(room.Id, new HallwayRoomAccess(room.Id, doorX, window, diner));
            // Open cutaway: retain entrance markers without a wall covering the room.
            var panel = new Color(0.24f, 0.36f, 0.35f);
            FrontageQuad(door.xMin + 0.025f, door.xMax - 0.025f, door.yMin, door.yMax - 0.025f, -0.174f, panel);
            Frame(door, 0.025f, new Color(0.72f, 0.69f, 0.55f));
            FrontageQuad(door.xMax - 0.095f, door.xMax - 0.060f, bottom + 0.39f, bottom + 0.46f, -0.182f, new Color(0.90f, 0.75f, 0.40f));

            // Stable entity ID numbers avoid renumbering existing doors during expansion.
            var number = room.Id.Value.ToString();
            var digitWidth = Mathf.Min(0.075f, doorWidth / Mathf.Max(1, number.Length));
            var labelWidth = number.Length * digitWidth;
            FrontageQuad(doorX - labelWidth * 0.5f - 0.03f, doorX + labelWidth * 0.5f + 0.03f,
                bottom + 0.95f, bottom + 1.10f, -0.18f, new Color(0.12f, 0.25f, 0.26f));
            for (var i = 0; i < number.Length; i++)
                DrawDigit(number[i] - '0', doorX - labelWidth * 0.5f + i * digitWidth, bottom + 0.97f, digitWidth * 0.85f);
        }

        private void Frame(Rect rect, float trim, Color color)
        {
            FrontageQuad(rect.xMin - trim, rect.xMin, rect.yMin, rect.yMax, -0.178f, color);
            FrontageQuad(rect.xMax, rect.xMax + trim, rect.yMin, rect.yMax, -0.178f, color);
            FrontageQuad(rect.xMin - trim, rect.xMax + trim, rect.yMax, rect.yMax + trim, -0.178f, color);
            FrontageQuad(rect.xMin - trim, rect.xMax + trim, rect.yMin - trim, rect.yMin, -0.178f, color);
        }

        private static readonly int[] DigitSegments = { 63, 6, 91, 79, 102, 109, 125, 7, 127, 111 };
        private void DrawDigit(int digit, float x, float y, float width)
        {
            if (digit < 0 || digit > 9) return;
            var mask = DigitSegments[digit]; var height = 0.10f; var line = 0.011f;
            var ink = new Color(0.97f, 0.90f, 0.66f);
            if ((mask & 1) != 0) FrontageQuad(x, x + width, y + height - line, y + height, -0.185f, ink);
            if ((mask & 2) != 0) FrontageQuad(x + width - line, x + width, y + height * 0.5f, y + height, -0.185f, ink);
            if ((mask & 4) != 0) FrontageQuad(x + width - line, x + width, y, y + height * 0.5f, -0.185f, ink);
            if ((mask & 8) != 0) FrontageQuad(x, x + width, y, y + line, -0.185f, ink);
            if ((mask & 16) != 0) FrontageQuad(x, x + line, y, y + height * 0.5f, -0.185f, ink);
            if ((mask & 32) != 0) FrontageQuad(x, x + line, y + height * 0.5f, y + height, -0.185f, ink);
            if ((mask & 64) != 0) FrontageQuad(x, x + width, y + height * 0.5f - line * 0.5f, y + height * 0.5f + line * 0.5f, -0.185f, ink);
        }

        private void FrontageQuad(float left, float right, float bottom, float top, float z, Color color)
        {
            if (right <= left || top <= bottom) return;
            var start = _frontageVertices.Count;
            _frontageVertices.Add(new Vector3(left, bottom, z)); _frontageVertices.Add(new Vector3(right, bottom, z));
            _frontageVertices.Add(new Vector3(right, top, z)); _frontageVertices.Add(new Vector3(left, top, z));
            for (var i = 0; i < 4; i++) _frontageColors.Add(color);
            _frontageTriangles.Add(start); _frontageTriangles.Add(start + 3); _frontageTriangles.Add(start + 2);
            _frontageTriangles.Add(start); _frontageTriangles.Add(start + 2); _frontageTriangles.Add(start + 1);
        }

        private void AddQuad(float left, float right, float bottom, float top, float z, Color color)
        {
            var start = _vertices.Count;
            _vertices.Add(new Vector3(left, bottom, z));
            _vertices.Add(new Vector3(right, bottom, z));
            _vertices.Add(new Vector3(right, top, z));
            _vertices.Add(new Vector3(left, top, z));
            for (var i = 0; i < 4; i++) _colors.Add(color);
            _uvs.Add(Vector2.zero);
            _uvs.Add(Vector2.right);
            _uvs.Add(Vector2.one);
            _uvs.Add(Vector2.up);
            _triangles.Add(start); _triangles.Add(start + 3); _triangles.Add(start + 2);
            _triangles.Add(start); _triangles.Add(start + 2); _triangles.Add(start + 1);
        }

        public void Clear()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_frontageMesh != null) Destroy(_frontageMesh);
            if (_frontageMaterial != null) Destroy(_frontageMaterial);
            _frontageTopology = null;
            _frontageMesh = null; _frontageMaterial = null; _frontageRoot = null; _frontageRenderer = null;
            _frontageRooms.Clear(); _access.Clear();
            OpenBayCount = 0; _treadRenderer = null;
            if (_root != null) Destroy(_root);
            _mesh = null;
            _root = null;
            _slabs.Clear();
        }

        private static void Destroy(Object value)
        {
            if (UnityEngine.Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
