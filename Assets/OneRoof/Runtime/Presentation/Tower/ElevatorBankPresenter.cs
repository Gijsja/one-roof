using System.Collections.Generic;
using OneRoof.Application.Transit;
using OneRoof.Domain.Transit;
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

        private sealed class LandingDoor
        {
            public int Floor;
            public int CarIndex;
            public GameObject Root;
            public Transform Left;
            public Transform Right;
            public Transform Recess;
            public Transform Header;
            public Transform Indicator;
        }
        private readonly List<LandingDoor> _landingDoors = new List<LandingDoor>();
        private int _landingCarCount;
        private GameObject _landingMeshRoot;
        private Mesh _landingMesh;
        private Material _landingMaterial;
        private MeshRenderer _landingRenderer;
        private MaterialPropertyBlock _landingProperties;
        public float LandingDoorsAlpha { get; private set; } = 1f;
        private Vector3[] _landingVertices;
        private Color[] _landingColors;
        private TowerProjection _landingMeshSnapshot;
        private int _landingMeshMinX;
        private int _landingMeshMaxX;
        public Mesh LandingMesh => _landingMesh;
        private bool _landingDoorsVisible = true;
        private bool _carVisualsVisible = true;
        public int LandingDoorCount => _landingDoors.Count;

        public static float DoorOpenFraction(ElevatorProjection elevator)
        {
            var remaining = elevator.DoorCycleTicks > 0
                ? Mathf.Clamp01((float)elevator.TimerTicksRemaining / elevator.DoorCycleTicks) : 0f;
            switch (elevator.Phase)
            {
                case ElevatorCarPhase.DoorsOpening: return 1f - remaining;
                case ElevatorCarPhase.OpenLoading: return 1f;
                case ElevatorCarPhase.DoorsClosing: return remaining;
                default: return 0f;
            }
        }

        public void SetLandingDoorsVisible(bool visible)
        {
            _landingDoorsVisible = visible;
            _carVisualsVisible = !visible;
            // Hallway landings replace the shaft cutaway. Hide complete cabins including
            // authored dressing so open apertures never reveal a second closed door.
            for (var i = 0; i < _elevatorViews.Count; i++)
                if (_elevatorViews[i] != null) _elevatorViews[i].gameObject.SetActive(!visible);
            if (_landingMeshRoot != null) _landingMeshRoot.SetActive(visible);
            for (var i = 0; i < _landingDoors.Count; i++)
                _landingDoors[i].Root.SetActive(visible);
        }

        public void SetLandingDoorsAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (Mathf.Approximately(LandingDoorsAlpha, alpha)) return;
            LandingDoorsAlpha = alpha;
            ApplyLandingAlpha();
        }

        private void ApplyLandingAlpha()
        {
            if (_landingRenderer == null) return;
            _landingProperties.Clear();
            var tint = new Color(1f, 1f, 1f, LandingDoorsAlpha);
            _landingProperties.SetColor("_BaseColor", tint);
            _landingProperties.SetColor("_Color", tint);
            _landingProperties.SetTexture("_BaseMap", Texture2D.whiteTexture);
            _landingProperties.SetTexture("_MainTex", Texture2D.whiteTexture);
            _landingRenderer.SetPropertyBlock(_landingProperties);
            _landingRenderer.enabled = LandingDoorsAlpha > .001f;
        }

        private void EnsureLandingDoors(int carCount)
        {
            if (_landingCarCount == carCount && _landingDoors.Count == _renderedShaftFloorCount * carCount) return;
            ClearLandingDoors();
            _landingCarCount = carCount;
            for (var floor = _renderedMinFloor; floor <= _renderedMaxFloor; floor++)
            for (var car = 0; car < carCount; car++)
            {
                var root = new GameObject($"Elevator Landing {floor} Car {car}");
                root.transform.SetParent(_parent, false);
                var landing = new LandingDoor { Floor = floor, CarIndex = car, Root = root };
                var back = CreateLandingPart(root.transform, "Landing Recess");
                back.transform.localScale = new Vector3(1.16f, 0.96f, 1f);
                // Cover the legacy moving cabin's closed-door dressing when the
                // landing opens, while staying behind the retracting panels.
                back.transform.localPosition = new Vector3(0f, 0f, -0.012f);
                landing.Recess = back;
                landing.Left = CreateLandingPart(root.transform, "Landing Door Left");
                landing.Right = CreateLandingPart(root.transform, "Landing Door Right");
                var header = CreateLandingPart(root.transform, "Landing Header");
                header.transform.localPosition = new Vector3(0f, 0.5f, -0.01f);
                header.transform.localScale = new Vector3(1.16f, 0.07f, 1f);
                landing.Header = header;
                landing.Indicator = CreateLandingPart(root.transform, "Landing Status Light");
                landing.Indicator.transform.localPosition = new Vector3(0f, 0.5f, -0.02f);
                landing.Indicator.transform.localScale = new Vector3(0.18f, 0.04f, 1f);
                root.SetActive(_landingDoorsVisible);
                _landingDoors.Add(landing);
            }
        }

        private Transform CreateLandingPart(Transform parent, string name)
        {
            // Lightweight markers keep per-landing geometry inspectable; rendering is batched.
            var part = new GameObject(name).transform;
            part.SetParent(parent, false);
            return part;
        }

        private void CreateLandingMesh()
        {
            _landingProperties ??= new MaterialPropertyBlock();
            _landingMeshRoot = new GameObject("Elevator Landing Mesh");
            _landingMeshRoot.transform.SetParent(_parent, false);
            var filter = _landingMeshRoot.AddComponent<MeshFilter>();
            var renderer = _landingMeshRoot.AddComponent<MeshRenderer>();
            _landingMaterial = new Material(_worldMaterial) { name = "Hallway Landing Fade Material", renderQueue = 3000 };
            _landingMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _landingMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _landingMaterial.SetInt("_ZWrite", 0);
            renderer.sharedMaterial = _landingMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _landingRenderer = renderer;
            ApplyLandingAlpha();
            _landingMesh = new Mesh { name = "Elevator Landing Geometry" };
            _landingMesh.MarkDynamic();
            _landingVertices = new Vector3[_landingDoors.Count * 20];
            _landingColors = new Color[_landingVertices.Length];
            var triangles = new int[_landingDoors.Count * 30];
            var uvs = new Vector2[_landingVertices.Length];
            for (var q = 0; q < _landingDoors.Count * 5; q++)
            {
                var v = q * 4;
                var t = q * 6;
                triangles[t] = v; triangles[t + 1] = v + 3; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 1;
                uvs[v] = Vector2.zero; uvs[v + 1] = Vector2.right;
                uvs[v + 2] = Vector2.one; uvs[v + 3] = Vector2.up;
            }
            _landingMesh.indexFormat = _landingVertices.Length > 65534
                ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            _landingMesh.vertices = _landingVertices;
            _landingMesh.triangles = triangles;
            _landingMesh.uv = uvs;
            filter.sharedMesh = _landingMesh;
            _landingMeshRoot.SetActive(_landingDoorsVisible);
        }

        private void WriteLandingQuad(int vertex, Transform marker, Color color)
        {
            var matrix = _landingMeshRoot.transform.worldToLocalMatrix * marker.localToWorldMatrix;
            _landingVertices[vertex] = matrix.MultiplyPoint3x4(new Vector3(-0.5f, -0.5f, 0f));
            _landingVertices[vertex + 1] = matrix.MultiplyPoint3x4(new Vector3(0.5f, -0.5f, 0f));
            _landingVertices[vertex + 2] = matrix.MultiplyPoint3x4(new Vector3(0.5f, 0.5f, 0f));
            _landingVertices[vertex + 3] = matrix.MultiplyPoint3x4(new Vector3(-0.5f, 0.5f, 0f));
            for (var i = 0; i < 4; i++) _landingColors[vertex + i] = color;
        }

        private void ClearLandingDoors()
        {
            for (var i = 0; i < _landingDoors.Count; i++)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(_landingDoors[i].Root);
                else Object.DestroyImmediate(_landingDoors[i].Root);
            }
            _landingDoors.Clear();
            _landingCarCount = 0;
            _landingMeshSnapshot = null;
            if (_landingMeshRoot != null)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(_landingMeshRoot);
                else Object.DestroyImmediate(_landingMeshRoot);
            }
            if (_landingMesh != null)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(_landingMesh);
                else Object.DestroyImmediate(_landingMesh);
            }
            _landingMeshRoot = null;
            _landingMesh = null;
            if (_landingMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(_landingMaterial);
                else Object.DestroyImmediate(_landingMaterial);
            }
            _landingMaterial = null;
            _landingRenderer = null;
            _landingVertices = null;
            _landingColors = null;
        }

        private void UpdateLandingDoors(TowerProjection snapshot)
        {
            EnsureLandingDoors(snapshot.Elevators.Count);
            if (_landingDoors.Count == 0) return;
            if (_landingMesh == null) CreateLandingMesh();
            if (ReferenceEquals(_landingMeshSnapshot, snapshot) && _landingMeshMinX == _shaftMinX && _landingMeshMaxX == _shaftMaxX) return;
            _landingMeshSnapshot = snapshot;
            _landingMeshMinX = _shaftMinX;
            _landingMeshMaxX = _shaftMaxX;
            for (var i = 0; i < _landingDoors.Count; i++)
            {
                var landing = _landingDoors[i];
                var elevator = snapshot.Elevators[landing.CarIndex];
                CalculateCarLayoutInCurrentShaft(landing.CarIndex, snapshot.Elevators.Count,
                    elevator.Capacity >= 12, out var x, out var width);
                landing.Root.transform.position = new Vector3(x, TowerStructurePresenter.FloorY(landing.Floor) - 0.17f, -0.16f);
                landing.Root.transform.localScale = new Vector3(width, 1f, 1f);
                var open = elevator.Floor == landing.Floor ? DoorOpenFraction(elevator) : 0f;
                // Panels retract into jambs rather than moving across neighboring landings.
                var panelWidth = 0.485f * (1f - open);
                landing.Left.localScale = landing.Right.localScale = new Vector3(panelWidth, 0.9f, 1f);
                landing.Left.localPosition = new Vector3(-0.49f + panelWidth * 0.5f, 0f, -0.015f);
                landing.Right.localPosition = new Vector3(0.49f - panelWidth * 0.5f, 0f, -0.015f);
                var vertex = i * 20;
                WriteLandingQuad(vertex, landing.Recess, new Color(0.035f, 0.07f, 0.08f));
                WriteLandingQuad(vertex + 4, landing.Left, new Color(0.44f, 0.55f, 0.56f));
                WriteLandingQuad(vertex + 8, landing.Right, new Color(0.35f, 0.47f, 0.49f));
                WriteLandingQuad(vertex + 12, landing.Header, new Color(0.67f, 0.68f, 0.57f));
                WriteLandingQuad(vertex + 16, landing.Indicator,
                    open > 0f ? new Color(0.34f, 0.9f, 0.62f) : new Color(0.88f, 0.64f, 0.26f));
            }
            _landingMesh.vertices = _landingVertices;
            _landingMesh.colors = _landingColors;
            _landingMesh.RecalculateBounds();
        }

        public IReadOnlyList<MeshRenderer> ElevatorViews => _elevatorViews;
        public int RenderedShaftFloorCount => _renderedShaftFloorCount;

        public Bounds GetShaftBounds()
        {
            var bottomY = TowerStructurePresenter.FloorY(_renderedMinFloor) - 0.74f;
            var topY = TowerStructurePresenter.FloorY(_renderedMaxFloor) + 0.74f;
            var shaftHeight = Mathf.Max(1.48f, topY - bottomY);
            var centerY = (bottomY + topY) * 0.5f;
            var left = -2.4f + _shaftMinX * 0.5f;
            var right = -2.4f + (_shaftMaxX + 1) * 0.5f;
            return new Bounds(new Vector3((left + right) * 0.5f, centerY, 0f), new Vector3(right - left + 0.06f, shaftHeight, 1f));
        }

        public bool IsPointerInShaft(Vector2 worldPos, out Bounds bounds)
        {
            bounds = GetShaftBounds();
            return worldPos.x >= bounds.min.x && worldPos.x <= bounds.max.x &&
                   worldPos.y >= bounds.min.y && worldPos.y <= bounds.max.y;
        }

        private int _renderedMinFloor;
        private int _renderedMaxFloor;
        private int _shaftMinX;
        private int _shaftMaxX = 1;

        public static void CalculateCarLayout(int carIndex, int totalCars, out float x, out float width)
        {
            const float shaftInternalLeft = -2.32f;
            const float shaftInternalRight = -1.48f;
            const float shaftWidth = shaftInternalRight - shaftInternalLeft;

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

        private void CalculateCarLayoutInCurrentShaft(int carIndex, int totalCars, bool isWide, out float x, out float width)
        {
            if (!isWide && _shaftMaxX - _shaftMinX <= 1)
            {
                CalculateCarLayout(carIndex, totalCars, out x, out width);
                return;
            }
            var left = -2.32f + _shaftMinX * 0.5f;
            var right = -1.48f + (_shaftMaxX - 1) * 0.5f;
            var available = Mathf.Max(0.4f, right - left);
            var gap = 0.03f;
            width = Mathf.Clamp((available - gap * Mathf.Max(0, totalCars - 1)) / Mathf.Max(1, totalCars), 0.16f, 0.72f);
            if (!isWide && _shaftMaxX - _shaftMinX > 1)
                width = Mathf.Min(0.48f, width);
            if (isWide)
                width = Mathf.Min(available, width * 1.2f);
            var span = totalCars * width + gap * Mathf.Max(0, totalCars - 1);
            var start = (left + right) * 0.5f - span * 0.5f + width * 0.5f;
            x = start + carIndex * (width + gap);
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

        public void EnsureShaftViews(int minFloor, int maxFloor) => EnsureShaftViews(minFloor, maxFloor, 0, 1);

        public void EnsureShaftViews(int minFloor, int maxFloor, int shaftMinX, int shaftMaxX)
        {
            if (minFloor > maxFloor) maxFloor = minFloor;
            var bottomY = TowerStructurePresenter.FloorY(minFloor) - 0.74f;
            var topY = TowerStructurePresenter.FloorY(maxFloor) + 0.74f;
            var shaftHeight = topY - bottomY;
            var centerY = (bottomY + topY) * 0.5f;

            _shaftMinX = shaftMinX;
            _shaftMaxX = Mathf.Max(shaftMinX, shaftMaxX);
            var shaftLeft = -2.4f + _shaftMinX * 0.5f;
            var shaftRight = -2.4f + (_shaftMaxX + 1) * 0.5f;
            var shaftCenterX = (shaftLeft + shaftRight) * 0.5f;
            var shaftWidth = shaftRight - shaftLeft;
            var colLeftX = shaftLeft;
            var colRightX = shaftRight;
            var railLeftX = shaftLeft + 0.04f;
            var railRightX = shaftRight - 0.04f;

            var penthouseColor = new Color(0.38f, 0.48f, 0.62f);
            var pitBufferColor = new Color(0.28f, 0.36f, 0.48f);

            if (_shaftCavity == null)
            {
                _shaftCavity = CreateQuad("Elevator Shaft Cavity", new Color(0.04f, 0.06f, 0.09f), new Vector3(shaftCenterX, centerY, 0.85f), new Vector2(shaftWidth, shaftHeight));
                _shaftRailLeft = CreateQuad("Shaft Rail Left", new Color(0.48f, 0.58f, 0.72f), new Vector3(railLeftX, centerY, 0.2f), new Vector2(0.035f, shaftHeight));
                _shaftRailRight = CreateQuad("Shaft Rail Right", new Color(0.48f, 0.58f, 0.72f), new Vector3(railRightX, centerY, 0.2f), new Vector2(0.035f, shaftHeight));
                _shaftColumnLeft = CreateQuad("Shaft Column Left", new Color(0.35f, 0.45f, 0.58f), new Vector3(colLeftX, centerY, 0.15f), new Vector2(0.06f, shaftHeight));
                _shaftColumnRight = CreateQuad("Shaft Column Right", new Color(0.35f, 0.45f, 0.58f), new Vector3(colRightX, centerY, 0.15f), new Vector2(0.06f, shaftHeight));
                _shaftPenthouse = CreateQuad("Shaft Penthouse Cap", penthouseColor, new Vector3(shaftCenterX, topY + 0.08f, 0.1f), new Vector2(shaftWidth + 0.12f, 0.16f));
                _shaftPitBuffer = CreateQuad("Shaft Pit Buffer", pitBufferColor, new Vector3(shaftCenterX, bottomY - 0.08f, 0.1f), new Vector2(shaftWidth + 0.12f, 0.16f));
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
                _shaftPenthouse.transform.localScale = new Vector3(shaftWidth + 0.12f, 0.16f, 1f);
                _shaftPitBuffer.transform.position = new Vector3(shaftCenterX, bottomY - 0.08f, 0.1f);
                _shaftPitBuffer.transform.localScale = new Vector3(shaftWidth + 0.12f, 0.16f, 1f);

                SetRendererColor(_shaftCavity.GetComponent<MeshRenderer>(), new Color(0.08f, 0.11f, 0.16f));
                SetRendererColor(_shaftRailLeft.GetComponent<MeshRenderer>(), new Color(0.48f, 0.58f, 0.72f));
                SetRendererColor(_shaftRailRight.GetComponent<MeshRenderer>(), new Color(0.48f, 0.58f, 0.72f));
                SetRendererColor(_shaftColumnLeft.GetComponent<MeshRenderer>(), new Color(0.35f, 0.45f, 0.58f));
                SetRendererColor(_shaftColumnRight.GetComponent<MeshRenderer>(), new Color(0.35f, 0.45f, 0.58f));
                SetRendererColor(_shaftPenthouse.GetComponent<MeshRenderer>(), penthouseColor);
                SetRendererColor(_shaftPitBuffer.GetComponent<MeshRenderer>(), pitBufferColor);
            }

            if (_renderedMinFloor != minFloor || _renderedMaxFloor != maxFloor) ClearLandingDoors();
            _renderedMinFloor = minFloor;
            _renderedMaxFloor = maxFloor;
            _renderedShaftFloorCount = maxFloor - minFloor + 1;
        }

        public void EnsureElevatorViews(int targetCount)
        {
            while (_elevatorViews.Count < targetCount)
            {
                var index = _elevatorViews.Count;
                CalculateCarLayoutInCurrentShaft(index, targetCount, false, out var x, out var carWidth);
                var view = CreateQuadRenderer($"Elevator Car {index}", new Color(0.13f, 0.31f, 0.29f), new Vector3(x, TowerStructurePresenter.FloorY(0), -0.12f), new Vector2(carWidth, 1.06f));
                view.gameObject.SetActive(_carVisualsVisible);
                _elevatorViews.Add(view);
            }

            for (var i = 0; i < _elevatorViews.Count; i++)
            {
                CalculateCarLayoutInCurrentShaft(i, _elevatorViews.Count, false, out var carX, out var carWidth);
                if (_elevatorViews[i] != null)
                {
                    // Authored Tower views used to retain their serialized default-white
                    // material until the first simulation tick. Normalize them to the
                    // presenter's material immediately so the initial frame matches
                    // dynamically-created cars.
                    _elevatorViews[i].sharedMaterial = _worldMaterial;
                    _elevatorViews[i].transform.position = new Vector3(carX, TowerStructurePresenter.FloorY(0), -0.12f);
                    _elevatorViews[i].transform.localScale = new Vector3(carWidth, 1.06f, 1f);
                    SetRendererColor(_elevatorViews[i], new Color(0.13f, 0.31f, 0.29f));
                    LayoutCarDressing(_elevatorViews[i], carWidth);
                }
            }
        }

        /// <summary>
        /// Cabin details are normalized to the car's scale and stay legible even
        /// when two or three cars share the shaft.
        /// </summary>
        private void LayoutCarDressing(MeshRenderer cabin, float carWidth)
        {
            if (cabin == null) return;
            var leftDoor = EnsureCarChild(cabin, "Car Door Left", new Color(0.12f, 0.23f, 0.26f));
            leftDoor.transform.localPosition = new Vector3(-0.225f, -0.035f, -0.035f);
            leftDoor.transform.localScale = new Vector3(0.43f, 0.78f, 1f);
            var rightDoor = EnsureCarChild(cabin, "Car Door Right", new Color(0.10f, 0.20f, 0.23f));
            rightDoor.transform.localPosition = new Vector3(0.225f, -0.035f, -0.035f);
            rightDoor.transform.localScale = new Vector3(0.43f, 0.78f, 1f);
            var seam = EnsureCarChild(cabin, "CarDoor", new Color(0.07f, 0.16f, 0.18f));
            seam.transform.localPosition = new Vector3(0f, -0.035f, -0.05f);
            seam.transform.localScale = new Vector3(0.035f, 0.80f, 1f);
            var roof = EnsureCarChild(cabin, "CarRoof", new Color(0.61f, 0.72f, 0.70f));
            roof.transform.localPosition = new Vector3(0f, 0.48f, -0.04f);
            roof.transform.localScale = new Vector3(1.12f, 0.11f, 1f);
            var sill = EnsureCarChild(cabin, "Car Sill", new Color(0.56f, 0.72f, 0.64f));
            sill.transform.localPosition = new Vector3(0f, -0.47f, -0.04f);
            sill.transform.localScale = new Vector3(1.10f, 0.08f, 1f);
            var indicator = EnsureCarChild(cabin, "Car Status Light", new Color(0.92f, 0.80f, 0.38f));
            indicator.transform.localPosition = new Vector3(0f, 0.48f, -0.065f);
            indicator.transform.localScale = new Vector3(0.22f, 0.045f, 1f);
            var leftJamb = EnsureCarChild(cabin, "Car Left Jamb", new Color(0.40f, 0.62f, 0.55f));
            leftJamb.transform.localPosition = new Vector3(-0.49f, -0.02f, -0.06f);
            leftJamb.transform.localScale = new Vector3(0.045f, 0.86f, 1f);
            var rightJamb = EnsureCarChild(cabin, "Car Right Jamb", new Color(0.40f, 0.62f, 0.55f));
            rightJamb.transform.localPosition = new Vector3(0.49f, -0.02f, -0.06f);
            rightJamb.transform.localScale = new Vector3(0.045f, 0.86f, 1f);
        }

        private MeshRenderer EnsureCarChild(MeshRenderer cabin, string childName, Color color)
        {
            var existing = cabin.transform.Find(childName);
            if (existing != null)
            {
                var renderer = existing.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    SetRendererColor(renderer, color);
                    return renderer;
                }
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = childName;
            go.transform.SetParent(cabin.transform, false);
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            var childRenderer = go.GetComponent<MeshRenderer>();
            childRenderer.sharedMaterial = _worldMaterial;
            SetRendererColor(childRenderer, color);
            return childRenderer;
        }

        public void UpdateElevatorPositions(TowerProjection snapshot)
        {
            if (snapshot == null) return;
            UpdateLandingDoors(snapshot);

            for (var i = 0; i < snapshot.Elevators.Count; i++)
            {
                if (i >= _elevatorViews.Count)
                {
                    EnsureElevatorViews(snapshot.Elevators.Count);
                }

                var elevator = snapshot.Elevators[i];
                var targetY = TowerStructurePresenter.FloorY(elevator.Floor);
                CalculateCarLayoutInCurrentShaft(i, snapshot.Elevators.Count, elevator.Capacity >= 12, out var targetX, out var carWidth);

                var carTransform = _elevatorViews[i].transform;
                carTransform.localScale = new Vector3(carWidth, 1.06f, 1f);
                carTransform.position = Vector3.Lerp(carTransform.position, new Vector3(targetX, targetY, -0.12f), 0.25f);
                LayoutCarDressing(_elevatorViews[i], carWidth);

                var carColor = elevator.PassengerCount > 0
                    ? new Color(0.20f, 0.44f, 0.39f)
                    : new Color(0.13f, 0.31f, 0.29f);
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
            ClearLandingDoors();
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
            if (renderer == null) return;
            if (_worldMaterial != null && renderer.sharedMaterial != _worldMaterial)
            {
                renderer.sharedMaterial = _worldMaterial;
            }
            if (_colorBlock != null)
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
