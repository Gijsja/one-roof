using System;
using System.Collections.Generic;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.Application.Modes.Commands;
using OneRoof.Application.Overlays;
using OneRoof.Application.Prediction;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Commands;
using OneRoof.Presentation.Overlays;
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
        private readonly List<MeshRenderer> _residentViews = new List<MeshRenderer>();
        private readonly List<MeshRenderer> _elevatorViews = new List<MeshRenderer>();
        private readonly List<GameObject> _worldObjects = new List<GameObject>();
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

        private void OnDestroy()
        {
            if (_modeSession != null)
            {
                _modeSession.ModeChanged -= OnModeChanged;
            }

            if (_gridPlacement != null)
            {
                _gridPlacement.PlacementExecuted -= OnPlacementExecuted;
            }

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
                EnsureFloorViews();
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
            EnsureElevatorViews();
            EnsureResidentViews();
        }

        private void CreateWorldGeometry()
        {
            // Set up camera
            var camObj = GameObject.Find("Tower Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Tower Camera");
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6.4f;
                cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
                camObj.transform.position = new Vector3(1.1f, 0.4f, -10f);
            }

            // Elevator Shaft cavity at X = -2.4
            CreateQuad("Elevator Shaft Cavity", new Color(0.06f, 0.08f, 0.12f), new Vector3(-2.4f, 0.3f, 0.8f), new Vector2(1.8f, 10.5f), transform);
            CreateQuad("Shaft Rail Left", new Color(0.3f, 0.38f, 0.48f), new Vector3(-3.25f, 0.3f, 0.2f), new Vector2(0.04f, 10.5f), transform);
            CreateQuad("Shaft Rail Right", new Color(0.3f, 0.38f, 0.48f), new Vector3(-1.55f, 0.3f, 0.2f), new Vector2(0.04f, 10.5f), transform);

            EnsureFloorViews();
            EnsureElevatorViews();
            EnsureResidentViews();
        }

        private void EnsureFloorViews()
        {
            var targetCount = _simulationSession != null ? _simulationSession.Topology.FloorCount : InitialFloorCount;
            while (_renderedFloorCount < targetCount)
            {
                var floor = _renderedFloorCount;
                var y = FloorY(floor);
                var isLobby = floor == 0;

                var floorColor = isLobby
                    ? new Color(0.13f, 0.18f, 0.26f)
                    : new Color(0.10f, 0.14f, 0.20f);

                // Main floor slab
                CreateQuad($"Floor Slab {floor}", floorColor, new Vector3(1.1f, y, 1f), new Vector2(10.8f, 1.55f), transform);

                // Floor baseline divider
                CreateQuad($"Floor Line {floor}", new Color(0.35f, 0.45f, 0.58f), new Vector3(1.1f, y - 0.74f, 0f), new Vector2(10.8f, 0.05f), transform);

                // Room division posts on residential floors
                if (!isLobby)
                {
                    CreateQuad($"Wall L {floor}", new Color(0.20f, 0.26f, 0.35f), new Vector3(0.5f, y, 0.5f), new Vector2(0.06f, 1.4f), transform);
                    CreateQuad($"Wall R {floor}", new Color(0.20f, 0.26f, 0.35f), new Vector3(3.5f, y, 0.5f), new Vector2(0.06f, 1.4f), transform);
                }

                _renderedFloorCount++;
            }
        }

        private void EnsureElevatorViews()
        {
            var targetCount = _simulationSession.Simulation.ElevatorBank.Cars.Count;
            while (_elevatorViews.Count < targetCount)
            {
                var index = _elevatorViews.Count;
                var x = targetCount == 1 ? -2.4f : (-2.85f + index * 0.9f);
                var view = CreateQuad($"Elevator Car {index}", new Color(0.25f, 0.92f, 0.65f), new Vector3(x, FloorY(0), 0f), new Vector2(0.8f, 0.5f), transform);
                _elevatorViews.Add(view);
            }
        }

        private void EnsureResidentViews()
        {
            var targetCount = _simulationSession.ResidentCount;
            while (_residentViews.Count < targetCount)
            {
                var index = _residentViews.Count;
                var view = CreateQuad($"Resident View {index + 1}", ResolveResidentColor(index), Vector3.zero, new Vector2(0.22f, 0.44f), transform);
                _residentViews.Add(view);
            }
        }

        private void RenderVisualSnapshot()
        {
            EnsureFloorViews();
            EnsureElevatorViews();
            EnsureResidentViews();

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
                var targetX = snapshot.Elevators.Count == 1 ? -2.4f : (-2.85f + i * 0.9f);

                var carTransform = _elevatorViews[i].transform;
                carTransform.position = Vector3.Lerp(carTransform.position, new Vector3(targetX, targetY, 0f), 0.25f);

                // Tint car brighter when carrying passengers
                var carColor = elevator.PassengerCount > 0
                    ? new Color(0.3f, 0.95f, 0.7f)
                    : new Color(0.18f, 0.65f, 0.5f);
                SetRendererColor(_elevatorViews[i], carColor);
            }

            // Update resident views
            var queueIndex = 0;
            var arrivedCountsPerFloor = new int[floorCount];

            for (var i = 0; i < snapshot.Residents.Count; i++)
            {
                if (i >= _residentViews.Count)
                {
                    break;
                }

                var resident = snapshot.Residents[i];
                var residentTransform = _residentViews[i].transform;

                switch (resident.Status)
                {
                    case TransitResidentStatus.Queued:
                    {
                        var queueX = -1.2f + (queueIndex % 20) * 0.32f;
                        var queueY = FloorY(0) - 0.45f + (queueIndex / 20) * 0.42f;
                        residentTransform.position = new Vector3(queueX, queueY, -0.2f);
                        SetRendererColor(_residentViews[i], new Color(1f, 0.65f, 0.25f)); // Amber waiting
                        queueIndex++;
                        break;
                    }
                    case TransitResidentStatus.Riding:
                    {
                        // Inside elevator car
                        var elevatorIndex = i % Math.Max(1, snapshot.Elevators.Count);
                        var carY = FloorY(snapshot.Elevators[elevatorIndex].Floor);
                        var carX = snapshot.Elevators.Count == 1 ? -2.4f : (-2.85f + elevatorIndex * 0.9f);
                        residentTransform.position = new Vector3(carX + ((i % 4) - 1.5f) * 0.15f, carY, -0.3f);
                        SetRendererColor(_residentViews[i], new Color(0.25f, 0.85f, 1f)); // Cyan transit
                        break;
                    }
                    case TransitResidentStatus.Arrived:
                    {
                        var destFloor = resident.DestinationFloor;
                        if (destFloor < 0 || destFloor >= floorCount) destFloor = 0;
                        var slot = arrivedCountsPerFloor[destFloor]++;
                        var arrivedX = -0.8f + (slot % 16) * 0.42f;
                        var arrivedY = FloorY(destFloor) - 0.35f;
                        residentTransform.position = new Vector3(arrivedX, arrivedY, -0.1f);
                        SetRendererColor(_residentViews[i], new Color(0.35f, 0.85f, 0.5f)); // Green arrived
                        break;
                    }
                }
            }
        }

        private void OnGUI()
        {
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
                if (GUILayout.Button("+ Apartment", _hudButtonStyle, GUILayout.Height(24)))
                {
                    _modeSession.SelectBuildTool("residential:apartment");
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
            GUILayout.Label("Shortcuts: [Space] Pause  [1] Build  [2] Inspect  [3] Data  [Esc] Cancel", _hudHelpStyle);
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
