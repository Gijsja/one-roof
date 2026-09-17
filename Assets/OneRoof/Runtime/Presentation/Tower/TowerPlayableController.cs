using System;
using System.Collections.Generic;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.Application.Modes.Commands;
using OneRoof.Application.Overlays;
using OneRoof.Application.Prediction;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Content;
using OneRoof.Domain.Commands;
using OneRoof.Presentation.Architecture;
using OneRoof.Presentation.Furnishings;
using OneRoof.Presentation.Overlays;
using OneRoof.Presentation.Population;
using OneRoof.UI.Inspectors;
using OneRoof.UI.Modes;
using OneRoof.UI.Prediction;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Master presentation controller for the interactive First Playable Tower scene (Assets/Scenes/Tower.unity).
    /// Assembles the building cutaway, persistent residents, elevator bank transit simulation,
    /// Mode Shell (Build/Inspect/Data/Manage), Elevator Wait Flow Overlay, Root-Cause Inspector Card,
    /// Placement Prediction Preview, and Interactive Grid Placement into an interactive playable proof.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TowerPlayableController : MonoBehaviour
    {
        private const float DefaultTickInterval = 0.35f;
        public const int InitialResidentCount = 50;
        public const int InitialFloorCount = 5;

        // Core application sessions & services (pure C#, non-MonoBehaviour)
        private TowerSimulationSession _simulationSession;
        private ModeShellSession _modeSession;
        private ElevatorWaitOverlayService _overlayService;
        private ElevatorPlacementPredictor _predictor;

        // Presentation & UI subcomponents
        private ModeShellBarController _modeBar;
        private ElevatorWaitOverlayPresenter _overlayPresenter;
        private CongestionInspectorCardView _inspectorCard;
        private PlacementPreviewCardView _placementCard;
        private GridPlacementController _gridPlacement;
        private PlacementGhostPresenter _ghostPresenter;

        // Visual world rendering state
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;
        private readonly List<Renderer> _residentViews = new List<Renderer>();
        private readonly List<NpcSkeletalHierarchy> _residentSkeletons = new List<NpcSkeletalHierarchy>();
        private readonly List<MeshRenderer> _elevatorViews = new List<MeshRenderer>();
        private readonly List<GameObject> _worldObjects = new List<GameObject>();
        private readonly HashSet<OneRoof.Domain.Identity.EntityId> _renderedRoomIds = new HashSet<OneRoof.Domain.Identity.EntityId>();
        private GameObject _shaftCavity;
        private GameObject _shaftRailLeft;
        private GameObject _shaftRailRight;
        private GameObject _shaftColumnLeft;
        private GameObject _shaftColumnRight;
        private GameObject _shaftPenthouse;
        private GameObject _shaftPitBuffer;
        private int _renderedShaftFloorCount;
        private int _renderedFloorCount = 0;

        private float _tickAccumulator;
        private float _tickInterval = DefaultTickInterval;
        private bool _isPaused;

        private GUIStyle _hudHeaderStyle;
        private GUIStyle _hudMetricStyle;
        private GUIStyle _hudButtonStyle;
        private GUIStyle _hudHelpStyle;

        public TowerSimulationSession SimulationSession => _simulationSession;
        public TowerSimulationSession TransitSession => _simulationSession;
        public ModeShellSession ModeSession => _modeSession;
        public GridPlacementController GridPlacement => _gridPlacement;
        public PlacementGhostPresenter GhostPresenter => _ghostPresenter;

        public void Initialize()
        {
            if (_simulationSession != null) return;
            InitializeSessions();
            InitializeSubcomponents();
            CreateWorldGeometry();
            SubscribeEvents();
            RenderVisualSnapshot();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            if (_modeSession != null)
            {
                _modeSession.ModeChanged -= OnModeChanged;
            }

            if (_gridPlacement != null)
            {
                _gridPlacement.PlacementExecuted -= OnPlacementExecuted;
            }
        }

        private void OnDestroy()
        {
            OnDisable();

            if (_worldMaterial != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(_worldMaterial);
                }
                else
                {
                    DestroyImmediate(_worldMaterial);
                }
                _worldMaterial = null;
            }
        }

        private void Update()
        {
            if (!UnityEngine.Application.isPlaying) return;

            if (_simulationSession == null)
            {
                Initialize();
            }

            if (_simulationSession == null) return;

            HandleKeyboardInputs();

            if (!_isPaused)
            {
                _tickAccumulator += Time.deltaTime;
                while (_tickAccumulator >= _tickInterval)
                {
                    _tickAccumulator -= _tickInterval;
                    _simulationSession.AdvanceOneTick();
                }
            }

            UpdateOverlayAndPredictions();
            RenderVisualSnapshot();
        }

        private void InitializeSessions()
        {
            _simulationSession = new TowerSimulationSession();
            _modeSession = new ModeShellSession();
            _overlayService = new ElevatorWaitOverlayService();
            _predictor = new ElevatorPlacementPredictor();

            _colorBlock = new MaterialPropertyBlock();
            _worldMaterial = CreateWorldMaterial();
        }

        private void InitializeSubcomponents()
        {
            // Wire ModeShellBarController
            _modeBar = gameObject.GetComponent<ModeShellBarController>() ?? gameObject.AddComponent<ModeShellBarController>();
            _modeBar.Session = _modeSession;

            // Wire ElevatorWaitOverlayPresenter
            _overlayPresenter = gameObject.GetComponent<ElevatorWaitOverlayPresenter>() ?? gameObject.AddComponent<ElevatorWaitOverlayPresenter>();
            _overlayPresenter.SetVisible(false);

            // Wire CongestionInspectorCardView
            _inspectorCard = gameObject.GetComponent<CongestionInspectorCardView>() ?? gameObject.AddComponent<CongestionInspectorCardView>();
            _inspectorCard.Session = _modeSession;

            // Wire PlacementPreviewCardView
            _placementCard = gameObject.GetComponent<PlacementPreviewCardView>() ?? gameObject.AddComponent<PlacementPreviewCardView>();

            // Wire PlacementGhostPresenter & GridPlacementController
            _ghostPresenter = gameObject.GetComponent<PlacementGhostPresenter>() ?? gameObject.AddComponent<PlacementGhostPresenter>();
            _gridPlacement = gameObject.GetComponent<GridPlacementController>() ?? gameObject.AddComponent<GridPlacementController>();
            _gridPlacement.ModeSession = _modeSession;
            _gridPlacement.SimulationSession = _simulationSession;
            _gridPlacement.GhostPresenter = _ghostPresenter;
            _gridPlacement.PlacementExecuted += OnPlacementExecuted;
            var initialCam = GameObject.Find("Tower Camera")?.GetComponent<Camera>() ?? Camera.main;
            if (initialCam != null)
            {
                _gridPlacement.Camera = initialCam;
            }
        }

        private void SubscribeEvents()
        {
            _modeSession.ModeChanged += OnModeChanged;
        }

        private void OnModeChanged(ModeShellProjection projection)
        {
            // Toggle overlay visibility based on Data mode
            _overlayPresenter.SetVisible(projection.IsDataMode);

            if (projection.IsDataMode)
            {
                var congestion = _simulationSession.CongestionProjection();
                _overlayPresenter.UpdateOverlay(_overlayService.CreateOverlay(congestion));
            }

            // If entering Build mode with elevator tool, open prediction card
            if (projection.IsBuildMode && projection.SelectedBuildTool == "transit:elevator_car")
            {
                UpdatePlacementCard();
            }
            else if (!projection.IsBuildMode && _placementCard.IsOpen)
            {
                _placementCard.Close();
            }

            // If leaving Inspect mode, close inspector card
            if (!projection.IsInspectMode && _inspectorCard.IsOpen)
            {
                _inspectorCard.Close();
            }
        }

        private void OnPlacementExecuted(CommandResult result)
        {
            if (result.Accepted)
            {
                UpdateCamera();
                EnsureShaftViews();
                EnsureFloorViews();
                EnsureRoomViews();
                EnsureElevatorViews();
                EnsureResidentViews();
            }
        }

        private void HandleKeyboardInputs()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    _isPaused = !_isPaused;
                }
                else if (keyboard.rKey.wasPressedThisFrame)
                {
                    ResetCommuteSimulation();
                }
                else if (keyboard.tKey.wasPressedThisFrame && _isPaused)
                {
                    _simulationSession.AdvanceOneTick();
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _isPaused = !_isPaused;
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCommuteSimulation();
            }
            else if (Input.GetKeyDown(KeyCode.T) && _isPaused)
            {
                _simulationSession.AdvanceOneTick();
            }
#endif
        }

        private void UpdateOverlayAndPredictions()
        {
            var congestion = _simulationSession.CongestionProjection();

            if (_overlayPresenter.IsVisible)
            {
                _overlayPresenter.UpdateOverlay(_overlayService.CreateOverlay(congestion));
            }

            if (_placementCard.IsOpen)
            {
                var preview = _predictor.PredictAddition(congestion);
                _placementCard.SetPreview(preview, OnConfirmElevatorPlacement);
            }
        }

        public void InspectBottleneck()
        {
            _modeSession.SwitchMode(InteractionMode.Inspect);
            var congestion = _simulationSession.CongestionProjection();
            var overlay = _overlayService.CreateOverlay(congestion);

            var inspectorProjection = new ElevatorCongestionInspectorProjection(
                floorLevel: 0,
                title: "Floor 0 Elevator Congestion",
                symptomDescription: $"Morning commute bottleneck: {congestion.TotalQueued} residents waiting for elevator throughput.",
                contributingCauses: overlay.ContributingCauses,
                suggestedResponseAction: overlay.RecommendedAction,
                canDirectRouteToBuild: true,
                targetBuildTool: "transit:elevator_car");

            _inspectorCard.Inspect(inspectorProjection);
        }

        public void ToggleDataOverlay()
        {
            if (_modeSession.CurrentMode == InteractionMode.Data)
            {
                _modeSession.SwitchMode(InteractionMode.Inspect);
            }
            else
            {
                _modeSession.SwitchMode(InteractionMode.Data);
                _modeSession.SetActiveOverlay("overlay:elevator_wait");
            }
        }

        private void UpdatePlacementCard()
        {
            var congestion = _simulationSession.CongestionProjection();
            var preview = _predictor.PredictAddition(congestion);
            _placementCard.SetPreview(preview, OnConfirmElevatorPlacement);
        }

        public void ShowPlacementPreview()
        {
            if (_modeSession.CurrentMode != InteractionMode.Build)
            {
                _modeSession.SwitchMode(InteractionMode.Build);
            }

            if (_modeSession.Projection().SelectedBuildTool != "transit:elevator_car")
            {
                _modeSession.SelectBuildTool("transit:elevator_car");
            }

            UpdatePlacementCard();
        }

        public void OnConfirmElevatorPlacement()
        {
            _simulationSession.AddCapacity();
            _placementCard.Close();
            EnsureElevatorViews();
        }

        public void ResetCommuteSimulation()
        {
            _simulationSession.Reset();
            if (_gridPlacement != null)
            {
                _gridPlacement.SimulationSession = _simulationSession;
            }
            _inspectorCard.Close();
            _placementCard.Close();
            _overlayPresenter.SetVisible(_modeSession.CurrentMode == InteractionMode.Data);
            ClearWorldGeometry();
            CreateWorldGeometry();
        }

        private void CreateWorldGeometry()
        {
            ClearWorldGeometry();
            UpdateCamera();
            EnsureShaftViews();
            EnsureFloorViews();
            EnsureRoomViews();
            EnsureElevatorViews();
            EnsureResidentViews();
        }

        private void UpdateCamera()
        {
            var camObj = GameObject.Find("Tower Camera");
            Camera cam;
            if (camObj == null)
            {
                camObj = new GameObject("Tower Camera");
                cam = camObj.AddComponent<Camera>();
            }
            else
            {
                cam = camObj.GetComponent<Camera>();
            }

            camObj.tag = "MainCamera";

            if (cam != null)
            {
                if (_gridPlacement != null)
                {
                    _gridPlacement.Camera = cam;
                }

                var floorCount = _simulationSession != null ? _simulationSession.Topology.FloorCount : InitialFloorCount;
                var centerY = FloorY(0) + (floorCount - 1) * 1.75f * 0.5f;

                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(6.8f, (floorCount + 1) * 1.15f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
                cam.transform.position = new Vector3(-1.6f, centerY, -10f);

                // Wire interactive pan/zoom navigation controller
                var camCtrl = camObj.GetComponent<TowerCameraController>() ?? camObj.AddComponent<TowerCameraController>();
                camCtrl.Camera = cam;
                camCtrl.SetOverviewDefaults(new Vector3(-1.6f, centerY, -10f), cam.orthographicSize);
                camCtrl.SetBounds(-16f, 16f, FloorY(0) - 2f, FloorY(floorCount - 1) + 4f);
            }
        }

        private void ClearWorldGeometry()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name == "PlacementGhost") continue;
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            _worldObjects.Clear();
            _residentViews.Clear();
            _residentSkeletons.Clear();
            _elevatorViews.Clear();
            _renderedRoomIds.Clear();
            _shaftCavity = null;
            _shaftRailLeft = null;
            _shaftRailRight = null;
            _shaftColumnLeft = null;
            _shaftColumnRight = null;
            _shaftPenthouse = null;
            _shaftPitBuffer = null;
            _renderedShaftFloorCount = 0;
            _renderedFloorCount = 0;
        }

        private void EnsureShaftViews()
        {
            var floorCount = _simulationSession != null ? _simulationSession.Topology.FloorCount : InitialFloorCount;
            var bottomY = FloorY(0) - 0.74f;
            var topY = FloorY(floorCount - 1) + 0.74f;
            var shaftHeight = topY - bottomY;
            var centerY = (bottomY + topY) * 0.5f;

            const float shaftCenterX = -1.90f;
            const float shaftWidth = 1.00f; // Exactly discrete cells 0..1 (from -2.40f to -1.40f)
            const float colLeftX = -2.40f;
            const float colRightX = -1.40f;
            const float railLeftX = -2.36f;
            const float railRightX = -1.44f;

            if (_shaftCavity == null)
            {
                // Dark interior shaft cavity (behind cars/rails, in front of floor slab background)
                _shaftCavity = CreateQuad("Elevator Shaft Cavity", new Color(0.04f, 0.06f, 0.09f), new Vector3(shaftCenterX, centerY, 0.85f), new Vector2(shaftWidth, shaftHeight), transform).gameObject;

                // Guide rails running the entire vertical length
                _shaftRailLeft = CreateQuad("Shaft Rail Left", new Color(0.48f, 0.58f, 0.72f), new Vector3(railLeftX, centerY, 0.2f), new Vector2(0.035f, shaftHeight), transform).gameObject;
                _shaftRailRight = CreateQuad("Shaft Rail Right", new Color(0.48f, 0.58f, 0.72f), new Vector3(railRightX, centerY, 0.2f), new Vector2(0.035f, shaftHeight), transform).gameObject;

                // Outer structural vertical columns framing the shaft
                _shaftColumnLeft = CreateQuad("Shaft Column Left", new Color(0.35f, 0.45f, 0.58f), new Vector3(colLeftX, centerY, 0.15f), new Vector2(0.06f, shaftHeight), transform).gameObject;
                _shaftColumnRight = CreateQuad("Shaft Column Right", new Color(0.35f, 0.45f, 0.58f), new Vector3(colRightX, centerY, 0.15f), new Vector2(0.06f, shaftHeight), transform).gameObject;

                // Rooftop elevator machine room / penthouse cap
                _shaftPenthouse = CreateQuad("Shaft Penthouse Cap", new Color(0.38f, 0.48f, 0.62f), new Vector3(shaftCenterX, topY + 0.08f, 0.1f), new Vector2(1.12f, 0.16f), transform).gameObject;

                // Foundation pit buffer
                _shaftPitBuffer = CreateQuad("Shaft Pit Buffer", new Color(0.28f, 0.36f, 0.48f), new Vector3(shaftCenterX, bottomY - 0.08f, 0.1f), new Vector2(1.12f, 0.16f), transform).gameObject;
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

        private void EnsureFloorViews()
        {
            var topology = _simulationSession != null ? _simulationSession.Topology : null;
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
                CreateQuad($"Floor Slab {floor}", floorColor, new Vector3(centerX, y, 1f), new Vector2(width, 1.55f), transform);

                // Floor baseline dividers - split around elevator shaft [-2.40f, -1.40f] so the shaft remains an open vertical chute
                const float shaftLeft = -2.40f;
                const float shaftRight = -1.40f;
                var floorLineY = y - 0.74f;
                var floorLineColor = new Color(0.35f, 0.45f, 0.58f);

                if (shaftLeft > worldLeft)
                {
                    var leftSegWidth = shaftLeft - worldLeft;
                    var leftSegCenter = (worldLeft + shaftLeft) * 0.5f;
                    CreateQuad($"Floor Line L {floor}", floorLineColor, new Vector3(leftSegCenter, floorLineY, 0.05f), new Vector2(leftSegWidth, 0.05f), transform);
                }

                if (worldRight > shaftRight)
                {
                    var rightSegWidth = worldRight - shaftRight;
                    var rightSegCenter = (shaftRight + worldRight) * 0.5f;
                    CreateQuad($"Floor Line R {floor}", floorLineColor, new Vector3(rightSegCenter, floorLineY, 0.05f), new Vector2(rightSegWidth, 0.05f), transform);
                }

                _renderedFloorCount++;
            }
        }

        private void EnsureRoomViews()
        {
            if (_simulationSession == null) return;
            var topology = _simulationSession.Topology;

            // Remove views for rooms that were demolished
            var activeRoomIds = new HashSet<OneRoof.Domain.Identity.EntityId>(topology.Rooms.Keys);
            var demolishedIds = new List<OneRoof.Domain.Identity.EntityId>();
            foreach (var id in _renderedRoomIds)
            {
                if (!activeRoomIds.Contains(id))
                {
                    demolishedIds.Add(id);
                    var child = transform.Find($"RoomView_{id}");
                    if (child != null)
                    {
                        _worldObjects.Remove(child.gameObject);
                        if (UnityEngine.Application.isPlaying) Destroy(child.gameObject);
                        else DestroyImmediate(child.gameObject);
                    }
                }
            }
            foreach (var id in demolishedIds)
            {
                _renderedRoomIds.Remove(id);
            }

            foreach (var room in topology.Rooms.Values)
            {
                if (_renderedRoomIds.Contains(room.Id)) continue;
                _renderedRoomIds.Add(room.Id);

                var contentTypeStr = room.ContentType.Value ?? "";
                if (contentTypeStr.Equals("transit:elevator_shaft") || contentTypeStr.Equals("elevator_shaft"))
                {
                    continue; // shaft rendered via EnsureShaftViews
                }

                var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
                var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
                var width = worldRight - worldLeft;
                var centerX = (worldLeft + worldRight) * 0.5f;
                var y = FloorY(room.Floor);

                var isResidential = contentTypeStr.StartsWith("residential") || contentTypeStr.StartsWith("room:apartment");
                var isOffice = contentTypeStr.Equals("commercial:office", StringComparison.OrdinalIgnoreCase) || contentTypeStr.Contains("office");
                var isDiner = (contentTypeStr.StartsWith("commercial") || contentTypeStr.StartsWith("room:diner")) && !isOffice;
                var isStairwell = contentTypeStr.Equals("amenity:stairwell", StringComparison.OrdinalIgnoreCase) || contentTypeStr.Contains("stairwell");
                var isLobby = contentTypeStr.Contains("lobby");
                var isWestSide = centerX < -1.9f;

                if (isResidential || isOffice || isDiner || isLobby)
                {
                    // Production 9-sliced architectural backdrop, fixtures, and themed furnishings
                    var roomObj = new GameObject($"RoomView_{room.Id}");
                    roomObj.transform.SetParent(transform, false);
                    roomObj.transform.position = new Vector3(centerX, y, 0f);

                    var backdropPresenter = roomObj.AddComponent<RoomBackdropPresenter>();
                    backdropPresenter.Setup(contentTypeStr, width, 1.42f, worldLeft, worldRight, y, isWestSide);

                    var furnishingPresenter = roomObj.AddComponent<RoomFurnishingPresenter>();
                    furnishingPresenter.FurnishRoom(contentTypeStr, width, 1.42f, isWestSide);

                    _worldObjects.Add(roomObj);
                }

                if (isResidential)
                {
                    // Apartment unit number plaque near door
                    var doorX = isWestSide ? (worldRight - 0.35f) : (worldLeft + 0.35f);
                    CreateQuad($"Apt Tag {room.Id}", new Color(0.30f, 0.42f, 0.56f), new Vector3(doorX, y + 0.25f, 0.42f), new Vector2(0.28f, 0.09f), transform);

                    // Structural dividing walls
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.38f, 0.48f, 0.62f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.38f, 0.48f, 0.62f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                }
                else if (isOffice)
                {
                    // Structural dividing walls
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                }
                else if (isDiner)
                {
                    // Commercial Diner: warm bistro awning
                    CreateQuad($"Diner Awning {room.Id}", new Color(0.85f, 0.42f, 0.25f), new Vector3(centerX, y + 0.58f, 0.55f), new Vector2(1.3f, 0.16f), transform);

                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                }
                else if (isStairwell)
                {
                    // Architectural Stairwell: industrial stair chute with step treads and safety rails
                    CreateQuad($"Stair Bg {room.Id}", new Color(0.12f, 0.16f, 0.22f), new Vector3(centerX, y, 0.7f), new Vector2(width - 0.04f, 1.45f), transform);

                    // 4 ascending step rungs
                    CreateQuad($"Stair Tread 1 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX - 0.30f, y - 0.45f, 0.55f), new Vector2(0.35f, 0.06f), transform);
                    CreateQuad($"Stair Tread 2 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX - 0.10f, y - 0.15f, 0.55f), new Vector2(0.35f, 0.06f), transform);
                    CreateQuad($"Stair Tread 3 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX + 0.10f, y + 0.15f, 0.55f), new Vector2(0.35f, 0.06f), transform);
                    CreateQuad($"Stair Tread 4 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX + 0.30f, y + 0.45f, 0.55f), new Vector2(0.35f, 0.06f), transform);

                    // High-visibility yellow safety handrail
                    CreateQuad($"Stair Rail {room.Id}", new Color(0.88f, 0.76f, 0.30f), new Vector3(centerX, y, 0.50f), new Vector2(width * 0.75f, 0.04f), transform);

                    // Green emergency stair exit sign
                    CreateQuad($"Stair Exit Sign {room.Id}", new Color(0.20f, 0.85f, 0.45f), new Vector3(centerX, y + 0.58f, 0.48f), new Vector2(0.26f, 0.10f), transform);

                    CreateQuad($"Room Wall L {room.Id}", new Color(0.42f, 0.50f, 0.62f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.42f, 0.50f, 0.62f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                }
                else if (isLobby)
                {
                    // Reception Lobby desk and dividing walls
                    CreateQuad($"Lobby Desk {room.Id}", new Color(0.48f, 0.58f, 0.70f), new Vector3(centerX - 1.2f, y - 0.46f, 0.55f), new Vector2(1.2f, 0.30f), transform);

                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), transform);
                }
                else
                {
                    // Generic room fallback
                    CreateQuad($"Room {room.Id} ({contentTypeStr})", new Color(0.18f, 0.24f, 0.32f), new Vector3(centerX, y, 0.6f), new Vector2(width - 0.08f, 1.4f), transform);
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.35f, 0.45f, 0.58f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.45f), transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.35f, 0.45f, 0.58f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.45f), transform);
                }
            }
        }

        private void EnsureElevatorViews()
        {
            var targetCount = _simulationSession.Simulation.ElevatorBank.Cars.Count;
            while (_elevatorViews.Count < targetCount)
            {
                var index = _elevatorViews.Count;
                var x = targetCount == 1 ? -1.9f : (-2.15f + index * 0.5f);
                var view = CreateQuad($"Elevator Car {index}", new Color(0.25f, 0.92f, 0.65f), new Vector3(x, FloorY(0), 0f), new Vector2(0.48f, 0.5f), transform);
                _elevatorViews.Add(view);
            }
        }

        private void EnsureResidentViews()
        {
            var targetCount = _simulationSession.ResidentCount;
            while (_residentViews.Count < targetCount)
            {
                var index = _residentViews.Count;
                var go = new GameObject($"Resident View {index + 1}");
                go.transform.SetParent(transform, false);
                go.transform.position = Vector3.zero;

                var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
                skeletal.Initialize(index);

                _residentViews.Add(skeletal.MainRenderer);
                _residentSkeletons.Add(skeletal);
                _worldObjects.Add(go);
            }
        }

        private void RenderVisualSnapshot()
        {
            EnsureShaftViews();
            EnsureFloorViews();
            EnsureRoomViews();
            EnsureElevatorViews();
            EnsureResidentViews();
            UpdateCamera();

            var snapshot = _simulationSession.Projection();
            var floorCount = Math.Max(InitialFloorCount, _simulationSession.Topology.FloorCount);

            // Update elevator cars
            for (var i = 0; i < snapshot.Elevators.Count; i++)
            {
                if (i >= _elevatorViews.Count)
                {
                    EnsureElevatorViews();
                }

                var elevator = snapshot.Elevators[i];
                var targetY = FloorY(elevator.Floor);
                var targetX = snapshot.Elevators.Count == 1 ? -1.9f : (-2.15f + i * 0.5f);

                var carTransform = _elevatorViews[i].transform;
                carTransform.position = Vector3.Lerp(carTransform.position, new Vector3(targetX, targetY, 0f), 0.25f);

                // Tint car brighter when carrying passengers
                var carColor = elevator.PassengerCount > 0
                    ? new Color(0.3f, 0.95f, 0.7f)
                    : new Color(0.18f, 0.65f, 0.5f);
                SetRendererColor(_elevatorViews[i], carColor);
            }

            // Update resident views
            var queuedCountsPerFloor = new int[floorCount];
            var arrivedCountsPerFloor = new int[floorCount];
            var time = Time.time;
            var topology = _simulationSession.Topology;

            for (var i = 0; i < snapshot.Residents.Count; i++)
            {
                if (i >= _residentViews.Count)
                {
                    break;
                }

                var resident = snapshot.Residents[i];
                var residentTransform = _residentViews[i].transform;
                var skeletal = i < _residentSkeletons.Count ? _residentSkeletons[i] : null;

                switch (resident.Status)
                {
                    case TransitResidentStatus.InRoom:
                    case TransitResidentStatus.Arrived:
                    {
                        // Position resident inside their designated room (apartment, diner, office)
                        var placedInRoom = false;
                        if (resident.RoomId.HasValue && topology != null && topology.TryGetRoom(new OneRoof.Domain.Identity.EntityId(resident.RoomId.Value), out var room))
                        {
                            var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
                            var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
                            var roomWidth = worldRight - worldLeft;
                            var interiorLeft = worldLeft + 0.35f;
                            var interiorRight = worldRight - 0.35f;

                            float roomX;
                            if (roomWidth <= 1.2f)
                            {
                                roomX = (worldLeft + worldRight) * 0.5f;
                            }
                            else
                            {
                                var slot = resident.SlotInRoom;
                                var step = (interiorRight - interiorLeft) / 3f;
                                roomX = Mathf.Clamp(interiorLeft + (slot % 4) * step, interiorLeft, interiorRight);
                            }

                            var roomY = FloorY(room.Floor) - 0.58f;
                            residentTransform.position = new Vector3(roomX, roomY, -0.15f);

                            // Orient character facing slightly toward room center
                            var roomCenterX = (worldLeft + worldRight) * 0.5f;
                            var faceScaleX = (roomX < roomCenterX) ? 1f : -1f;
                            residentTransform.localScale = new Vector3(faceScaleX, 1f, 1f);

                            skeletal?.SetTransitStatus(TransitResidentStatus.InRoom);
                            placedInRoom = true;
                        }

                        if (!placedInRoom)
                        {
                            // Fallback if room not found
                            var destFloor = resident.DestinationFloor;
                            if (destFloor < 0 || destFloor >= floorCount) destFloor = 0;
                            var slot = arrivedCountsPerFloor[destFloor]++;
                            var arrivedX = -0.8f + (slot % 14) * 0.42f;
                            var arrivedY = FloorY(destFloor) - 0.58f;
                            residentTransform.position = new Vector3(arrivedX, arrivedY, -0.1f);
                            skeletal?.SetTransitStatus(TransitResidentStatus.InRoom);
                        }
                        break;
                    }

                    case TransitResidentStatus.Walking:
                    {
                        // Resident actively walking along corridor between room door and elevator
                        var walkX = -2.4f + resident.CellX * 0.5f + 0.25f;
                        var walkFloor = resident.Floor;
                        if (walkFloor < 0 || walkFloor >= floorCount) walkFloor = 0;
                        var walkY = FloorY(walkFloor) - 0.58f;
                        residentTransform.position = new Vector3(walkX, walkY, -0.2f);
                        skeletal?.SetTransitStatus(TransitResidentStatus.Walking);
                        break;
                    }

                    case TransitResidentStatus.Queued:
                    {
                        // Resident waiting at elevator landing on their specific floor
                        var queueFloor = resident.Floor;
                        if (queueFloor < 0 || queueFloor >= floorCount) queueFloor = 0;
                        var slot = queuedCountsPerFloor[queueFloor]++;
                        var queueX = -1.45f - (slot % 12) * 0.28f;
                        var queueY = FloorY(queueFloor) - 0.58f;
                        residentTransform.position = new Vector3(queueX, queueY, -0.2f);
                        residentTransform.localScale = new Vector3(1f, 1f, 1f); // face right toward elevator doors
                        skeletal?.SetTransitStatus(TransitResidentStatus.Queued);
                        break;
                    }

                    case TransitResidentStatus.Riding:
                    {
                        // Inside elevator car
                        var elevatorIndex = FindPassengerElevator(snapshot, resident.ResidentId);
                        if (elevatorIndex >= snapshot.Elevators.Count) elevatorIndex = 0;
                        var carY = FloorY(snapshot.Elevators[elevatorIndex].Floor) - 0.25f;
                        var carX = snapshot.Elevators.Count == 1 ? -1.9f : (-2.15f + elevatorIndex * 0.5f);
                        residentTransform.position = new Vector3(carX + ((i % 2) - 0.5f) * 0.12f, carY, -0.3f);
                        skeletal?.SetTransitStatus(TransitResidentStatus.Riding);
                        break;
                    }
                }

                // Evaluate emotional expression based on transit congestion or room activity
                if (resident.Status == TransitResidentStatus.Queued)
                {
                    // Explanation chain symptom: Queue wait time threshold
                    if (resident.WaitTicks >= 30)
                    {
                        skeletal?.SetEmote(NpcEmoteKind.Anger);
                    }
                    else if (resident.WaitTicks >= 15)
                    {
                        skeletal?.SetEmote(NpcEmoteKind.Sweat);
                    }
                    else if (resident.WaitTicks >= 5)
                    {
                        skeletal?.SetEmote(NpcEmoteKind.Ellipsis);
                    }
                    else
                    {
                        skeletal?.SetEmote(NpcEmoteKind.None);
                    }
                }
                else if (resident.Status == TransitResidentStatus.InRoom)
                {
                    switch (resident.Activity)
                    {
                        case OneRoof.Domain.Population.ActivityKind.Sleeping:
                            skeletal?.SetEmote(NpcEmoteKind.Sleeping);
                            break;
                        case OneRoof.Domain.Population.ActivityKind.Working:
                            skeletal?.SetEmote(NpcEmoteKind.Lightbulb);
                            break;
                        case OneRoof.Domain.Population.ActivityKind.Leisure:
                            skeletal?.SetEmote(NpcEmoteKind.MusicNote);
                            break;
                        default:
                            skeletal?.SetEmote(NpcEmoteKind.None);
                            break;
                    }
                }
                else
                {
                    skeletal?.SetEmote(NpcEmoteKind.None);
                }

                skeletal?.ApplyProceduralAnimation(time);
            }
        }

        private static int FindPassengerElevator(TowerProjection snapshot, int residentId)
        {
            for (var i = 0; i < snapshot.Elevators.Count; i++)
            {
                var ids = snapshot.Elevators[i].PassengerIds;
                for (var j = 0; j < ids.Count; j++)
                {
                    if (ids[j] == residentId)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        private void OnGUI()
        {
            if (_simulationSession == null)
            {
                Initialize();
            }

            if (_simulationSession == null || _modeSession == null)
            {
                return;
            }

            EnsureStyles();

            var snapshot = _simulationSession.Projection();
            var congestion = _simulationSession.CongestionProjection();
            var totalRes = Math.Max(1, _simulationSession.ResidentCount);

            // Top-left dashboard HUD
            var hudHeight = _modeSession.CurrentMode == InteractionMode.Build ? 260 : 220;
            var hudRect = new Rect(20, 20, 380, hudHeight);
            GUILayout.BeginArea(hudRect, GUI.skin.box);

            GUILayout.Label("ONE ROOF — FIRST PLAYABLE SLICE", _hudHeaderStyle);
            GUILayout.Label($"Sim Tick: {snapshot.Tick}  •  Status: {(_isPaused ? "[PAUSED]" : "[RUNNING]")}", _hudMetricStyle);
            GUILayout.Label($"Treasury: ${_simulationSession.Economy.CashBalance:N0}  •  Residents: {_simulationSession.ResidentCount}  •  Floors: {_simulationSession.FloorCount}", _hudMetricStyle);
            GUILayout.Space(4);

            var overlay = _overlayPresenter.CurrentOverlay ?? _overlayService.CreateOverlay(congestion);
            var severityBadge = overlay.OverallSeverity.ToString().ToUpperInvariant();

            GUILayout.Label($"Lobby Queue: {snapshot.QueueLength}/{totalRes} waiting  [{severityBadge}]", _hudMetricStyle);
            GUILayout.Label($"Delivered: {snapshot.ArrivedCount}/{totalRes} arrived", _hudMetricStyle);
            GUILayout.Label($"Average Completed Wait: {snapshot.AverageWaitTicks:F1} ticks", _hudMetricStyle);
            GUILayout.Label($"Elevator Bank: {snapshot.Elevators.Count} active car(s)", _hudMetricStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Inspect Bottleneck [I]", _hudButtonStyle, GUILayout.Height(28)))
            {
                InspectBottleneck();
            }
            if (GUILayout.Button("Flow Overlay [D]", _hudButtonStyle, GUILayout.Height(28)))
            {
                ToggleDataOverlay();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Car [B]", _hudButtonStyle, GUILayout.Height(28)))
            {
                ShowPlacementPreview();
            }
            if (GUILayout.Button("+ Add Car Now", _hudButtonStyle, GUILayout.Height(28)))
            {
                OnConfirmElevatorPlacement();
            }
            if (GUILayout.Button("Reset [R]", _hudButtonStyle, GUILayout.Height(28)))
            {
                ResetCommuteSimulation();
            }
            GUILayout.EndHorizontal();

            if (_modeSession.CurrentMode == InteractionMode.Build)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Apt", _hudButtonStyle, GUILayout.Height(24)))
                {
                    _modeSession.SelectBuildTool("residential:apartment");
                }
                if (GUILayout.Button("+ Diner", _hudButtonStyle, GUILayout.Height(24)))
                {
                    _modeSession.SelectBuildTool("commercial:diner");
                }
                if (GUILayout.Button("+ Shaft", _hudButtonStyle, GUILayout.Height(24)))
                {
                    _modeSession.SelectBuildTool("transit:elevator_shaft");
                }
                if (GUILayout.Button("+ Slab", _hudButtonStyle, GUILayout.Height(24)))
                {
                    _modeSession.SelectBuildTool("floor:slab");
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);
            GUILayout.Label("Shortcuts: [Space] Pause  [1] Build  [2] Inspect  [3] Data  [R-Click / Esc] Cancel", _hudHelpStyle);
            GUILayout.EndArea();
        }

        private MeshRenderer CreateQuad(string name, Color color, Vector3 position, Vector2 size, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _worldMaterial;
            SetRendererColor(renderer, color);

            _worldObjects.Add(go);
            return renderer;
        }

        private void SetRendererColor(MeshRenderer renderer, Color color)
        {
            _colorBlock.SetColor("_BaseColor", color);
            _colorBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(_colorBlock);
        }

        private static Material CreateWorldMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new MissingReferenceException("No suitable unlit shader found for TowerPlayableController.");
            }

            return new Material(shader);
        }

        public static float FloorY(int floor) => -3.2f + floor * 1.75f;

        private static Color ResolveResidentColor(int residentIndex)
        {
            return residentIndex % 3 == 0
                ? new Color(1f, 0.65f, 0.3f)
                : residentIndex % 3 == 1
                    ? new Color(0.35f, 0.75f, 1f)
                    : new Color(0.9f, 0.45f, 0.7f);
        }

        private void EnsureStyles()
        {
            if (_hudHeaderStyle != null)
            {
                return;
            }

            _hudHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.25f, 0.88f, 1f) }
            };

            _hudMetricStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _hudButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _hudHelpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
            };
        }
    }
}
