using System;
using System.Collections.Generic;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    /// <summary>
    /// Presentation component rendering an Oxygen Not Included-style connected utility network layer.
    /// Draws ground source/sink terminals, vertical risers, horizontal floor distribution raceways,
    /// consumer room drop lines, and circular connection junction ports using AllIn1SpriteShader
    /// with animated directional glowing pulses (GLOW_ON, TEXTURESCROLL_ON).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UtilitiesNetworkLayerPresenter : MonoBehaviour
    {
        public enum NetworkKind { Power, Water, Waste }

        private const float LayerZ = -0.35f;
        private const float NodeZ = -0.36f;
        private const float DefaultFloorHeight = TowerStructurePresenter.DefaultFloorHeight;

        private readonly List<LineRenderer> _linePool = new List<LineRenderer>();
        private readonly List<GameObject> _nodePool = new List<GameObject>();

        private UtilitiesOverlayProjection _overlay;
        private TowerTopologyProjection _topology;
        private Transform _layerRoot;
        private bool _visible;
        private int _activeLineCount;
        private int _activeNodeCount;

        private Material _activeLineMaterial;
        private Material _disruptedLineMaterial;
        private Material _activeNodeMaterial;
        private Material _disruptedNodeMaterial;
        private Texture2D _flowTexture;
        private Texture2D _portTexture;
        private bool _isAllIn1Shader;

        public NetworkKind SelectedNetwork { get; private set; } = NetworkKind.Power;
        public bool IsVisible => _visible;
        public int ActiveLineCount => _activeLineCount;
        public int ActiveNodeCount => _activeNodeCount;
        public bool IsAllIn1Shader => _isAllIn1Shader;
        public Material ActiveLineMaterial => _activeLineMaterial;
        public Material ActiveNodeMaterial => _activeNodeMaterial;

        public void SetVisible(bool visible)
        {
            _visible = visible;
            EnsureLayerRoot();
            _layerRoot.gameObject.SetActive(visible);
            if (visible) Rebuild();
        }

        public void Select(NetworkKind network)
        {
            if (SelectedNetwork == network) return;
            SelectedNetwork = network;
            ApplyNetworkColors();
            Rebuild();
        }

        public void UpdateOverlay(UtilitiesOverlayProjection overlay)
        {
            UpdateOverlay(overlay, _topology);
        }

        public void UpdateOverlay(UtilitiesOverlayProjection overlay, TowerTopologyProjection topology)
        {
            _overlay = overlay;
            if (topology != null) _topology = topology;
            Rebuild();
        }

        public void Rebuild()
        {
            if (_overlay == null)
            {
                DeactivateAll();
                return;
            }

            EnsureLayerRoot();
            EnsureMaterials();
            ApplyNetworkColors();

            _activeLineCount = 0;
            _activeNodeCount = 0;

            switch (SelectedNetwork)
            {
                case NetworkKind.Power:
                    BuildPowerNetwork();
                    break;
                case NetworkKind.Water:
                    BuildWaterNetwork();
                    break;
                case NetworkKind.Waste:
                    BuildWasteNetwork();
                    break;
            }

            for (var i = _activeLineCount; i < _linePool.Count; i++)
            {
                if (_linePool[i] != null) _linePool[i].gameObject.SetActive(false);
            }
            for (var i = _activeNodeCount; i < _nodePool.Count; i++)
            {
                if (_nodePool[i] != null) _nodePool[i].SetActive(false);
            }
        }

        private void BuildPowerNetwork()
        {
            var groundFloor = 0;
            var groundBaseY = TowerStructurePresenter.FloorY(groundFloor);
            var substationCenter = FindFirstRoomCenter(groundFloor, ElectricalGridState.SubstationContentId, -2.4f + 0.25f);
            var substationY = groundBaseY + 0.75f;

            // Ground Substation source terminal
            GetNodeMarker(new Vector3(substationCenter, substationY, NodeZ), 0.12f, _activeNodeMaterial, "Power_SubstationSource");

            for (var i = 0; i < _overlay.Floors.Count; i++)
            {
                var floorData = _overlay.Floors[i];
                var floor = floorData.Floor;
                var connected = floorData.PowerConnected && floorData.Voltage >= ElectricalGridState.BrownoutVoltageThreshold;
                var lineMat = connected ? _activeLineMaterial : _disruptedLineMaterial;
                var nodeMat = connected ? _activeNodeMaterial : _disruptedNodeMaterial;

                var riserColumn = floorData.PowerColumn;
                var riserX = CellCenterX(riserColumn);
                var baseY = TowerStructurePresenter.FloorY(floor);
                var ceilingY = baseY + 1.42f;

                // On Ground floor: connect Substation to the riser trunk
                if (floor == 0)
                {
                    GetLineSegment(
                        new Vector3(substationCenter, substationY, LayerZ),
                        new Vector3(riserX, substationY, LayerZ),
                        0.08f, lineMat, "Power_SubstationFeed");
                }

                // Vertical Riser trunk segment
                var hasRiser = floorData.PowerConnected || (_topology != null && HasRoomOfType(floor, ElectricalGridState.RiserContentId));
                if (hasRiser)
                {
                    var bottomY = (floor == 0) ? substationY : baseY;
                    var topY = baseY + DefaultFloorHeight;
                    GetLineSegment(
                        new Vector3(riserX, bottomY, LayerZ),
                        new Vector3(riserX, topY, LayerZ),
                        0.09f, lineMat, $"Power_Riser_FL{floor}");

                    GetNodeMarker(new Vector3(riserX, ceilingY, NodeZ), 0.08f, nodeMat, $"Power_RiserJunction_FL{floor}");
                }

                // Horizontal ceiling raceway and consumer drops
                if (_topology != null)
                {
                    var consumerRooms = GetConsumerRooms(floor);
                    var transCenter = FindFirstRoomCenterNullable(floor, ElectricalGridState.TransformerContentId);

                    if (consumerRooms.Count > 0 || transCenter.HasValue)
                    {
                        var minX = riserX;
                        var maxX = riserX;

                        if (transCenter.HasValue)
                        {
                            minX = Math.Min(minX, transCenter.Value);
                            maxX = Math.Max(maxX, transCenter.Value);
                        }
                        for (var c = 0; c < consumerRooms.Count; c++)
                        {
                            var cx = RoomCenterX(consumerRooms[c]);
                            minX = Math.Min(minX, cx);
                            maxX = Math.Max(maxX, cx);
                        }

                        // Ceiling distribution line
                        GetLineSegment(
                            new Vector3(minX, ceilingY, LayerZ),
                            new Vector3(maxX, ceilingY, LayerZ),
                            0.06f, lineMat, $"Power_CeilingRaceway_FL{floor}");

                        // Floor Transformer drop & marker
                        if (transCenter.HasValue)
                        {
                            var tx = transCenter.Value;
                            var transY = baseY + 0.80f;
                            GetLineSegment(
                                new Vector3(tx, ceilingY, LayerZ),
                                new Vector3(tx, transY, LayerZ),
                                0.05f, lineMat, $"Power_TransformerDrop_FL{floor}");
                            GetNodeMarker(new Vector3(tx, transY, NodeZ), 0.10f, nodeMat, $"Power_TransformerNode_FL{floor}");
                        }

                        // Consumer room drops & terminal ports
                        for (var c = 0; c < consumerRooms.Count; c++)
                        {
                            var rx = RoomCenterX(consumerRooms[c]);
                            var terminalY = baseY + 0.85f;
                            GetLineSegment(
                                new Vector3(rx, ceilingY, LayerZ),
                                new Vector3(rx, terminalY, LayerZ),
                                0.04f, lineMat, $"Power_RoomDrop_{consumerRooms[c].Id}");
                            GetNodeMarker(new Vector3(rx, terminalY, NodeZ), 0.06f, nodeMat, $"Power_RoomPort_{consumerRooms[c].Id}");
                        }
                    }
                }
            }
        }

        private void BuildWaterNetwork()
        {
            var groundFloor = 0;
            var groundBaseY = TowerStructurePresenter.FloorY(groundFloor);
            var pumpCenter = FindFirstRoomCenter(groundFloor, WaterWasteNetworkState.WaterPumpContentId, -2.4f + 0.25f);
            var pumpY = groundBaseY + 0.50f;

            // Ground Water Pump source terminal
            GetNodeMarker(new Vector3(pumpCenter, pumpY, NodeZ), 0.12f, _activeNodeMaterial, "Water_PumpSource");

            for (var i = 0; i < _overlay.Floors.Count; i++)
            {
                var floorData = _overlay.Floors[i];
                var floor = floorData.Floor;
                var connected = floorData.WaterConnected && floorData.WaterPressure >= WaterWasteNetworkState.MinimumServicePressure;
                var lineMat = connected ? _activeLineMaterial : _disruptedLineMaterial;
                var nodeMat = connected ? _activeNodeMaterial : _disruptedNodeMaterial;

                var riserColumn = floorData.WaterColumn;
                var riserX = CellCenterX(riserColumn);
                var baseY = TowerStructurePresenter.FloorY(floor);
                var subfloorY = baseY + 0.22f;

                // On Ground floor: connect Pump to the water riser trunk
                if (floor == 0)
                {
                    GetLineSegment(
                        new Vector3(pumpCenter, pumpY, LayerZ),
                        new Vector3(riserX, pumpY, LayerZ),
                        0.08f, lineMat, "Water_PumpFeed");
                }

                // Vertical Water Riser trunk segment
                var hasRiser = floorData.WaterConnected || (_topology != null && HasRoomOfType(floor, WaterWasteNetworkState.WaterRiserContentId));
                if (hasRiser)
                {
                    var bottomY = (floor == 0) ? pumpY : baseY;
                    var topY = baseY + DefaultFloorHeight;
                    GetLineSegment(
                        new Vector3(riserX, bottomY, LayerZ),
                        new Vector3(riserX, topY, LayerZ),
                        0.09f, lineMat, $"Water_Riser_FL{floor}");

                    GetNodeMarker(new Vector3(riserX, subfloorY, NodeZ), 0.08f, nodeMat, $"Water_RiserJunction_FL{floor}");
                }

                // Horizontal subfloor raceway and room taps
                if (_topology != null)
                {
                    var consumerRooms = GetConsumerRooms(floor);
                    var boosterCenter = FindFirstRoomCenterNullable(floor, WaterWasteNetworkState.BoosterPumpContentId);

                    if (consumerRooms.Count > 0 || boosterCenter.HasValue)
                    {
                        var minX = riserX;
                        var maxX = riserX;

                        if (boosterCenter.HasValue)
                        {
                            minX = Math.Min(minX, boosterCenter.Value);
                            maxX = Math.Max(maxX, boosterCenter.Value);
                        }
                        for (var c = 0; c < consumerRooms.Count; c++)
                        {
                            var cx = RoomCenterX(consumerRooms[c]);
                            minX = Math.Min(minX, cx);
                            maxX = Math.Max(maxX, cx);
                        }

                        // Subfloor distribution pipe
                        GetLineSegment(
                            new Vector3(minX, subfloorY, LayerZ),
                            new Vector3(maxX, subfloorY, LayerZ),
                            0.06f, lineMat, $"Water_SubfloorPipe_FL{floor}");

                        // Booster Pump node
                        if (boosterCenter.HasValue)
                        {
                            var bx = boosterCenter.Value;
                            var boosterY = baseY + 0.50f;
                            GetLineSegment(
                                new Vector3(bx, subfloorY, LayerZ),
                                new Vector3(bx, boosterY, LayerZ),
                                0.05f, lineMat, $"Water_BoosterFeed_FL{floor}");
                            GetNodeMarker(new Vector3(bx, boosterY, NodeZ), 0.10f, nodeMat, $"Water_BoosterNode_FL{floor}");
                        }

                        // Consumer room taps & inlet ports
                        for (var c = 0; c < consumerRooms.Count; c++)
                        {
                            var rx = RoomCenterX(consumerRooms[c]);
                            var inletY = baseY + 0.55f;
                            GetLineSegment(
                                new Vector3(rx, subfloorY, LayerZ),
                                new Vector3(rx, inletY, LayerZ),
                                0.04f, lineMat, $"Water_RoomTap_{consumerRooms[c].Id}");
                            GetNodeMarker(new Vector3(rx, inletY, NodeZ), 0.06f, nodeMat, $"Water_RoomPort_{consumerRooms[c].Id}");
                        }
                    }
                }
            }
        }

        private void BuildWasteNetwork()
        {
            var groundFloor = 0;
            var groundBaseY = TowerStructurePresenter.FloorY(groundFloor);
            var collectorCenter = FindFirstRoomCenter(groundFloor, WaterWasteNetworkState.WasteCollectionContentId, -2.4f + 0.25f);
            var collectorY = groundBaseY + 0.40f;

            // Ground Waste Collection sink terminal
            GetNodeMarker(new Vector3(collectorCenter, collectorY, NodeZ), 0.12f, _activeNodeMaterial, "Waste_CollectionSink");

            for (var i = 0; i < _overlay.Floors.Count; i++)
            {
                var floorData = _overlay.Floors[i];
                var floor = floorData.Floor;
                var connected = floorData.WasteConnected && floorData.WasteCause == "None";
                var lineMat = connected ? _activeLineMaterial : _disruptedLineMaterial;
                var nodeMat = connected ? _activeNodeMaterial : _disruptedNodeMaterial;

                var chuteColumn = floorData.WasteColumn;
                var chuteX = CellCenterX(chuteColumn);
                var baseY = TowerStructurePresenter.FloorY(floor);
                var subfloorY = baseY + 0.12f;

                // On Ground floor: connect Collector to the waste chute
                if (floor == 0)
                {
                    GetLineSegment(
                        new Vector3(collectorCenter, collectorY, LayerZ),
                        new Vector3(chuteX, collectorY, LayerZ),
                        0.08f, lineMat, "Waste_CollectorFeed");
                }

                // Vertical Waste Chute trunk segment
                var hasChute = floorData.WasteConnected || (_topology != null && HasRoomOfType(floor, WaterWasteNetworkState.WasteChuteContentId));
                if (hasChute)
                {
                    var bottomY = (floor == 0) ? collectorY : baseY;
                    var topY = baseY + DefaultFloorHeight;
                    GetLineSegment(
                        new Vector3(chuteX, bottomY, LayerZ),
                        new Vector3(chuteX, topY, LayerZ),
                        0.11f, lineMat, $"Waste_Chute_FL{floor}");

                    GetNodeMarker(new Vector3(chuteX, subfloorY, NodeZ), 0.08f, nodeMat, $"Waste_ChuteJunction_FL{floor}");
                }

                // Horizontal subfloor drainage raceway and room drains
                if (_topology != null)
                {
                    var consumerRooms = GetConsumerRooms(floor);

                    if (consumerRooms.Count > 0)
                    {
                        var minX = chuteX;
                        var maxX = chuteX;

                        for (var c = 0; c < consumerRooms.Count; c++)
                        {
                            var cx = RoomCenterX(consumerRooms[c]);
                            minX = Math.Min(minX, cx);
                            maxX = Math.Max(maxX, cx);
                        }

                        // Subfloor drainage pipe
                        GetLineSegment(
                            new Vector3(minX, subfloorY, LayerZ),
                            new Vector3(maxX, subfloorY, LayerZ),
                            0.06f, lineMat, $"Waste_SubfloorDrain_FL{floor}");

                        // Consumer room gravity drains & inlet ports
                        for (var c = 0; c < consumerRooms.Count; c++)
                        {
                            var rx = RoomCenterX(consumerRooms[c]);
                            var drainY = baseY + 0.40f;
                            GetLineSegment(
                                new Vector3(rx, drainY, LayerZ),
                                new Vector3(rx, subfloorY, LayerZ),
                                0.04f, lineMat, $"Waste_RoomDrain_{consumerRooms[c].Id}");
                            GetNodeMarker(new Vector3(rx, drainY, NodeZ), 0.06f, nodeMat, $"Waste_RoomPort_{consumerRooms[c].Id}");
                        }
                    }
                }
            }
        }

        private LineRenderer GetLineSegment(Vector3 start, Vector3 end, float width, Material mat, string name)
        {
            LineRenderer line;
            if (_activeLineCount < _linePool.Count)
            {
                line = _linePool[_activeLineCount];
            }
            else
            {
                var go = new GameObject($"Utility_Line_{_linePool.Count}");
                go.transform.SetParent(_layerRoot, false);
                line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.numCapVertices = 4;
                line.numCornerVertices = 4;
                line.textureMode = LineTextureMode.Tile;
                line.sortingOrder = 40;
                _linePool.Add(line);
            }

            _activeLineCount++;
            line.gameObject.name = name;
            line.sharedMaterial = mat;
            line.startWidth = width;
            line.endWidth = width;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.gameObject.SetActive(true);
            line.enabled = _visible;
            return line;
        }

        private GameObject GetNodeMarker(Vector3 position, float radius, Material mat, string name)
        {
            GameObject node;
            if (_activeNodeCount < _nodePool.Count)
            {
                node = _nodePool[_activeNodeCount];
            }
            else
            {
                node = GameObject.CreatePrimitive(PrimitiveType.Quad);
                node.name = $"Utility_Node_{_nodePool.Count}";
                node.transform.SetParent(_layerRoot, false);
                var col = node.GetComponent<Collider>();
                if (col != null)
                {
                    if (UnityEngine.Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
                var mr = node.GetComponent<MeshRenderer>();
                mr.sortingOrder = 42;
                _nodePool.Add(node);
            }

            _activeNodeCount++;
            node.name = name;
            node.transform.position = position;
            node.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            var renderer = node.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            node.SetActive(true);
            return node;
        }

        private void EnsureLayerRoot()
        {
            if (_layerRoot != null) return;
            var layer = new GameObject("Utility Network View Layer");
            _layerRoot = layer.transform;
            _layerRoot.SetParent(transform, false);
            layer.SetActive(_visible);
        }

        private void EnsureMaterials()
        {
            if (_activeLineMaterial != null && _disruptedLineMaterial != null) return;

            if (_flowTexture == null) _flowTexture = CreateFlowTexture();
            if (_portTexture == null) _portTexture = CreatePortNodeTexture();

            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            if (shader == null) throw new MissingReferenceException("No unlit or sprite shader available for UtilitiesNetworkLayerPresenter.");

            _isAllIn1Shader = shader.name.IndexOf("AllIn1SpriteShader", StringComparison.OrdinalIgnoreCase) >= 0;

            _activeLineMaterial = CreateNetworkMaterial(shader, _isAllIn1Shader, _flowTexture, isLine: true, isDisrupted: false);
            _disruptedLineMaterial = CreateNetworkMaterial(shader, _isAllIn1Shader, _flowTexture, isLine: true, isDisrupted: true);
            _activeNodeMaterial = CreateNetworkMaterial(shader, _isAllIn1Shader, _portTexture, isLine: false, isDisrupted: false);
            _disruptedNodeMaterial = CreateNetworkMaterial(shader, _isAllIn1Shader, _portTexture, isLine: false, isDisrupted: true);

            ApplyNetworkColors();
        }

        private void ApplyNetworkColors()
        {
            if (_activeLineMaterial == null) return;

            Color activeCol, activeGlow, disCol, disGlow;
            float scrollSpeed;

            switch (SelectedNetwork)
            {
                case NetworkKind.Water:
                    activeCol = new Color(0.18f, 0.82f, 1f, 1f);
                    activeGlow = new Color(0.10f, 0.65f, 1f, 1f);
                    disCol = new Color(0.40f, 0.55f, 0.85f, 0.7f);
                    disGlow = new Color(0.30f, 0.45f, 0.75f, 0.5f);
                    scrollSpeed = 1.2f;
                    break;
                case NetworkKind.Waste:
                    activeCol = new Color(0.78f, 0.42f, 1f, 1f);
                    activeGlow = new Color(0.68f, 0.25f, 0.95f, 1f);
                    disCol = new Color(0.90f, 0.25f, 0.55f, 0.7f);
                    disGlow = new Color(0.80f, 0.15f, 0.45f, 0.5f);
                    scrollSpeed = -1.2f;
                    break;
                case NetworkKind.Power:
                default:
                    activeCol = new Color(1f, 0.78f, 0.18f, 1f);
                    activeGlow = new Color(1f, 0.60f, 0.10f, 1f);
                    disCol = new Color(0.95f, 0.35f, 0.15f, 0.75f);
                    disGlow = new Color(0.85f, 0.20f, 0.10f, 0.5f);
                    scrollSpeed = 1.5f;
                    break;
            }

            SetMaterialProperties(_activeLineMaterial, activeCol, activeGlow, scrollSpeed);
            SetMaterialProperties(_disruptedLineMaterial, disCol, disGlow, 0f);
            SetMaterialProperties(_activeNodeMaterial, activeCol, activeGlow, 0f);
            SetMaterialProperties(_disruptedNodeMaterial, disCol, disGlow, 0f);
        }

        private void SetMaterialProperties(Material mat, Color color, Color glowColor, float scrollSpeed)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            if (_isAllIn1Shader)
            {
                if (mat.HasProperty("_GlowColor")) mat.SetColor("_GlowColor", glowColor);
                if (mat.HasProperty("_TextureScrollXSpeed")) mat.SetFloat("_TextureScrollXSpeed", scrollSpeed);
            }
        }

        private static Material CreateNetworkMaterial(Shader shader, bool isAllIn1, Texture2D texture, bool isLine, bool isDisrupted)
        {
            var mat = new Material(shader);
            mat.mainTexture = texture;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);

            if (isAllIn1)
            {
                mat.EnableKeyword("GLOW_ON");
                mat.SetFloat("_Glow", isDisrupted ? 0.6f : 1.8f);

                if (isLine)
                {
                    mat.EnableKeyword("TEXTURESCROLL_ON");
                    mat.SetFloat("_TextureScrollXSpeed", isDisrupted ? 0.0f : 1.5f);
                    mat.SetFloat("_TextureScrollYSpeed", 0.0f);
                }
            }
            else
            {
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return mat;
        }

        private static Texture2D CreateFlowTexture()
        {
            const int width = 64;
            const int height = 8;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "UtilityFlow_ProceduralTex";

            var colors = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var edge = Mathf.Sin((y + 0.5f) / height * Mathf.PI);
                for (var x = 0; x < width; x++)
                {
                    var t = (x % 32) / 32f;
                    var intensity = (t >= 0.15f && t <= 0.85f)
                        ? Mathf.Sin((t - 0.15f) / 0.7f * Mathf.PI)
                        : 0f;
                    var alpha = Mathf.Clamp01(0.20f + 0.80f * intensity) * edge;
                    colors[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D CreatePortNodeTexture()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "UtilityPort_ProceduralTex";

            var colors = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var maxRadius = center;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / maxRadius;
                    var dy = (y - center) / maxRadius;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha;
                    if (dist < 0.28f)
                    {
                        alpha = 1.0f;
                    }
                    else if (dist < 0.50f)
                    {
                        alpha = 0.15f;
                    }
                    else if (dist <= 0.85f)
                    {
                        alpha = 0.95f;
                    }
                    else if (dist < 1.0f)
                    {
                        alpha = Mathf.Clamp01((1.0f - dist) / 0.15f) * 0.95f;
                    }
                    else
                    {
                        alpha = 0f;
                    }

                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply(false, true);
            return tex;
        }

        private static float CellCenterX(int column) => -2.4f + column * 0.5f + 0.25f;

        private static float RoomCenterX(Room room)
        {
            var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
            var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
            return (worldLeft + worldRight) * 0.5f;
        }

        private static bool IsInfrastructure(ContentId type) =>
            type == ElectricalGridState.SubstationContentId ||
            type == ElectricalGridState.RiserContentId ||
            type == ElectricalGridState.TransformerContentId ||
            type == WaterWasteNetworkState.WaterPumpContentId ||
            type == WaterWasteNetworkState.WaterRiserContentId ||
            type == WaterWasteNetworkState.BoosterPumpContentId ||
            type == WaterWasteNetworkState.WasteChuteContentId ||
            type == WaterWasteNetworkState.WasteCollectionContentId;

        private List<Room> GetConsumerRooms(int floor)
        {
            var result = new List<Room>();
            if (_topology == null) return result;
            var rooms = _topology.GetRoomsOnFloor(floor);
            for (var i = 0; i < rooms.Count; i++)
            {
                if (!IsInfrastructure(rooms[i].ContentType))
                {
                    result.Add(rooms[i]);
                }
            }
            return result;
        }

        private bool HasRoomOfType(int floor, ContentId type)
        {
            if (_topology == null) return false;
            var rooms = _topology.GetRoomsOnFloor(floor);
            for (var i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].ContentType == type) return true;
            }
            return false;
        }

        private float FindFirstRoomCenter(int floor, ContentId type, float fallbackX)
        {
            if (_topology == null) return fallbackX;
            var rooms = _topology.GetRoomsOnFloor(floor);
            for (var i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].ContentType == type) return RoomCenterX(rooms[i]);
            }
            return fallbackX;
        }

        private float? FindFirstRoomCenterNullable(int floor, ContentId type)
        {
            if (_topology == null) return null;
            var rooms = _topology.GetRoomsOnFloor(floor);
            for (var i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].ContentType == type) return RoomCenterX(rooms[i]);
            }
            return null;
        }

        private void DeactivateAll()
        {
            _activeLineCount = 0;
            _activeNodeCount = 0;
            for (var i = 0; i < _linePool.Count; i++) if (_linePool[i] != null) _linePool[i].gameObject.SetActive(false);
            for (var i = 0; i < _nodePool.Count; i++) if (_nodePool[i] != null) _nodePool[i].SetActive(false);
        }

        private void ClearGeometry()
        {
            for (var i = 0; i < _linePool.Count; i++)
            {
                if (_linePool[i] != null)
                {
                    if (UnityEngine.Application.isPlaying) Destroy(_linePool[i].gameObject);
                    else DestroyImmediate(_linePool[i].gameObject);
                }
            }
            _linePool.Clear();
            _activeLineCount = 0;

            for (var i = 0; i < _nodePool.Count; i++)
            {
                if (_nodePool[i] != null)
                {
                    if (UnityEngine.Application.isPlaying) Destroy(_nodePool[i]);
                    else DestroyImmediate(_nodePool[i]);
                }
            }
            _nodePool.Clear();
            _activeNodeCount = 0;
        }

        private void OnDestroy()
        {
            ClearGeometry();
            DestroyAsset(_activeLineMaterial);
            DestroyAsset(_disruptedLineMaterial);
            DestroyAsset(_activeNodeMaterial);
            DestroyAsset(_disruptedNodeMaterial);
            DestroyAsset(_flowTexture);
            DestroyAsset(_portTexture);
        }

        private static void DestroyAsset(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
