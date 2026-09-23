using System;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.Application.Overlays;
using OneRoof.Application.Prediction;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Presentation.Overlays;
using OneRoof.UI.Inspectors;
using OneRoof.UI.Modes;
using OneRoof.UI.Prediction;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Which simulation the scene boots: the five-floor first-playable fixture or a from-scratch ground floor.</summary>
    public enum TowerStartMode
    {
        StandardFiveFloor = 0,
        GroundFloorStart = 1
    }

    /// <summary>Presentation coordinator routing projection snapshots to four deep presenters.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TowerPlayableController : MonoBehaviour
    {
        public const int InitialResidentCount = 50, InitialFloorCount = 5;

        [SerializeField] private TowerStartMode _startMode = TowerStartMode.StandardFiveFloor;

        private TowerSimulationSession _sim; private ModeShellSession _mode;
        private TowerDataOverlays _dataOverlays; private ElevatorPlacementPredictor _predictor;
        private ModeShellBarController _modeBar; private ElevatorWaitOverlayPresenter _overlayPresenter;
        private SatisfactionOverlayPresenter _satisfactionPresenter;
        private PopulationOverlayPresenter _populationPresenter;
        private ScrutinyOverlayPresenter _scrutinyPresenter;
        private FootTrafficOverlayPresenter _footTrafficPresenter;
        private BusinessHealthOverlayPresenter _businessHealthPresenter;
        private UtilitiesOverlayPresenter _utilitiesPresenter;
        private UtilitiesNetworkLayerPresenter _utilitiesNetworkLayer;
        private CongestionInspectorCardView _inspectorCard; private PlacementPreviewCardView _placementCard;
        private GridPlacementController _gridPlacement; private PlacementGhostPresenter _ghostPresenter;
        private InspectSelectionController _inspectSelection; private InspectOutlinePresenter _inspectOutline;
        private TowerDashboardHudView _hudView;
        private TowerAtmospherePresenter _atmosphere;
        private OutsideCityPresenter _outside;

        private readonly TowerStructurePresenter _structure = new TowerStructurePresenter();
        private readonly ElevatorBankPresenter _elevator = new ElevatorBankPresenter();
        private readonly RoomPresenter _room = new RoomPresenter();
        private readonly TowerResidentPresenter _resident = new TowerResidentPresenter();

        private Material _worldMat; private MaterialPropertyBlock _colorBlock;
        private float _tickAcc, _tickInterval = 0.35f; private bool _isPaused;

        public TowerSimulationSession SimulationSession => _sim; public TowerSimulationSession TransitSession => _sim;
        public ModeShellSession ModeSession => _mode; public TowerDataOverlays DataOverlays => _dataOverlays;
        public ElevatorWaitOverlayPresenter OverlayPresenter => _overlayPresenter; public ElevatorPlacementPredictor Predictor => _predictor;
        public SatisfactionOverlayPresenter SatisfactionPresenter => _satisfactionPresenter;
        public PopulationOverlayPresenter PopulationPresenter => _populationPresenter;
        public ScrutinyOverlayPresenter ScrutinyPresenter => _scrutinyPresenter;
        public UtilitiesOverlayPresenter UtilitiesPresenter => _utilitiesPresenter;
        public GridPlacementController GridPlacement => _gridPlacement; public PlacementGhostPresenter GhostPresenter => _ghostPresenter;
        public InspectSelectionController InspectSelection => _inspectSelection; public InspectOutlinePresenter InspectOutline => _inspectOutline;
        public TowerStructurePresenter StructurePresenter => _structure; public ElevatorBankPresenter ElevatorPresenter => _elevator;
        public RoomPresenter RoomPresenter => _room; public TowerResidentPresenter ResidentPresenter => _resident;
        public TowerAtmospherePresenter AtmospherePresenter => _atmosphere;
        public TowerStartMode StartMode => _startMode;
        public bool IsGroundStart => _startMode == TowerStartMode.GroundFloorStart;
        public bool IsPaused => _isPaused; public static float FloorY(int floor) => TowerStructurePresenter.FloorY(floor);

        public void Initialize()
        {
            if (_sim != null) return;
            _sim = IsGroundStart ? TowerSimulationSession.CreateGroundFloorStart() : new TowerSimulationSession();
            if (!IsGroundStart) SeedMorningRush();
            _mode = new ModeShellSession();
            _dataOverlays = new TowerDataOverlays(_sim); _predictor = new ElevatorPlacementPredictor();
            _colorBlock = new MaterialPropertyBlock(); _worldMat = CreateWorldMaterial();

            _structure.Initialize(transform, _worldMat, _colorBlock); _elevator.Initialize(transform, _worldMat, _colorBlock);
            _room.Initialize(transform, _worldMat, _colorBlock); _resident.Initialize(transform);
            InitSubcomponents(); _mode.ModeChanged += OnModeChanged; CreateWorldGeometry();
        }

        private void Awake() => Initialize(); private void OnEnable() => Initialize();
        private void OnDisable() { if (_mode != null) _mode.ModeChanged -= OnModeChanged; if (_gridPlacement != null) _gridPlacement.PlacementExecuted -= OnPlacement; if (_populationPresenter != null) _populationPresenter.FloorInspectionRequested -= InspectPopulationFloor; if (_scrutinyPresenter != null) _scrutinyPresenter.InspectionRequested -= InspectScrutiny; if (_utilitiesPresenter != null) { _utilitiesPresenter.InspectionRequested -= InspectUtilities; _utilitiesPresenter.NetworkSelected -= OnUtilityNetworkSelected; } }
        private void OnDestroy() { OnDisable(); _atmosphere?.Clear(); _outside?.Clear(); if (_worldMat != null) { if (UnityEngine.Application.isPlaying) Destroy(_worldMat); else DestroyImmediate(_worldMat); } }

        private void Update()
        {
            if (!UnityEngine.Application.isPlaying) return;
            if (_sim == null) Initialize();
            HandleKeyboard();
            if (!_isPaused && (_tickAcc += Time.deltaTime) >= _tickInterval) { _tickAcc -= _tickInterval; _sim.AdvanceOneTick(); }
            UpdateOverlayAndPredictions();
            RenderVisualSnapshot();
        }

        private void InitSubcomponents()
        {
            (_modeBar = Ensure<ModeShellBarController>()).Session = _mode;
            (_overlayPresenter = Ensure<ElevatorWaitOverlayPresenter>()).SetVisible(false);
            (_satisfactionPresenter = Ensure<SatisfactionOverlayPresenter>()).SetVisible(false);
            (_populationPresenter = Ensure<PopulationOverlayPresenter>()).SetVisible(false);
            _populationPresenter.FloorInspectionRequested += InspectPopulationFloor;
            (_scrutinyPresenter = Ensure<ScrutinyOverlayPresenter>()).SetVisible(false);
            (_footTrafficPresenter = Ensure<FootTrafficOverlayPresenter>()).SetVisible(false);
            (_businessHealthPresenter = Ensure<BusinessHealthOverlayPresenter>()).SetVisible(false);
            (_utilitiesPresenter = Ensure<UtilitiesOverlayPresenter>()).SetVisible(false); _utilitiesPresenter.InspectionRequested += InspectUtilities;
            _utilitiesPresenter.NetworkSelected += OnUtilityNetworkSelected;
            (_utilitiesNetworkLayer = Ensure<UtilitiesNetworkLayerPresenter>()).SetVisible(false);
            _scrutinyPresenter.InspectionRequested += InspectScrutiny;
            (_inspectorCard = Ensure<CongestionInspectorCardView>()).Session = _mode;
            _placementCard = Ensure<PlacementPreviewCardView>(); _ghostPresenter = Ensure<PlacementGhostPresenter>();
            _gridPlacement = Ensure<GridPlacementController>(); _gridPlacement.ModeSession = _mode;
            _gridPlacement.SimulationSession = _sim; _gridPlacement.GhostPresenter = _ghostPresenter;
            _gridPlacement.PlacementExecuted += OnPlacement; (_hudView = Ensure<TowerDashboardHudView>()).Controller = this;
            _inspectOutline = Ensure<InspectOutlinePresenter>(); (_inspectSelection = Ensure<InspectSelectionController>()).ModeSession = _mode;
            _inspectSelection.SimulationSession = _sim; _inspectSelection.RoomPresenter = _room; _inspectSelection.ElevatorPresenter = _elevator;
            _inspectSelection.ResidentPresenter = _resident; _inspectSelection.OutlinePresenter = _inspectOutline;
            _atmosphere = Ensure<TowerAtmospherePresenter>(); _atmosphere.Initialize();
            var camera = TowerCameraController.EnsureTowerCamera(_sim.FloorCount, _gridPlacement, resetView: true);
            _outside = Ensure<OutsideCityPresenter>(); _outside.Initialize(camera, _worldMat);
            if (IsGroundStart && _mode.CurrentMode != InteractionMode.Build) _mode.SwitchMode(InteractionMode.Build);
        }

        private T Ensure<T>() where T : Component => GetComponent<T>() ?? gameObject.AddComponent<T>();

        private void OnModeChanged(ModeShellProjection p)
        {
            var satisfaction = p.IsDataMode && p.ActiveOverlayId == "overlay:satisfaction";
            var population = p.IsDataMode && p.ActiveOverlayId == "overlay:population";
            var scrutiny = p.IsDataMode && p.ActiveOverlayId == "overlay:scrutiny";
            var footTraffic = p.IsDataMode && p.ActiveOverlayId == "overlay:foot_traffic";
            var businessHealth = p.IsDataMode && p.ActiveOverlayId == "overlay:business_health";
            var utilities = p.IsDataMode && p.ActiveOverlayId == "overlay:utilities";
            _overlayPresenter.SetVisible(p.IsDataMode && !satisfaction && !population && !scrutiny && !footTraffic && !businessHealth && !utilities); _satisfactionPresenter.SetVisible(satisfaction); _populationPresenter.SetVisible(population); _scrutinyPresenter.SetVisible(scrutiny); _footTrafficPresenter.SetVisible(footTraffic); _businessHealthPresenter.SetVisible(businessHealth); _utilitiesPresenter.SetVisible(utilities);
            _utilitiesNetworkLayer.SetVisible(utilities);
            if (satisfaction) _satisfactionPresenter.UpdateOverlay(_dataOverlays.Satisfaction);
            else if (population) _populationPresenter.UpdateOverlay(_dataOverlays.Population);
            else if (scrutiny) _scrutinyPresenter.UpdateOverlay(_dataOverlays.Scrutiny);
            else if (footTraffic) _footTrafficPresenter.UpdateOverlay(_dataOverlays.FootTraffic);
            else if (businessHealth) _businessHealthPresenter.UpdateOverlay(_dataOverlays.BusinessHealth);
            else if (utilities) { _utilitiesPresenter.UpdateOverlay(_dataOverlays.Utilities); _utilitiesNetworkLayer.UpdateOverlay(_dataOverlays.Utilities, _sim?.TopologyProjection()); }
            else if (p.IsDataMode) _overlayPresenter.UpdateOverlay(_dataOverlays.ElevatorWait);
            if (p.IsBuildMode && p.SelectedBuildTool == "transit:elevator_car") UpdatePlacementCard();
            else if (!p.IsBuildMode && _placementCard.IsOpen) _placementCard.Close();
            if (!p.IsInspectMode && _inspectorCard.IsOpen) _inspectorCard.Close();
        }

        private void OnPlacement(CommandResult r) { if (r.Accepted) { UpdateCamera(resetView: false); SyncPresenterGeometry(); } }

        public void SyncPresenterGeometry()
        {
            var topo = _sim?.TopologyProjection(); var fl = topo != null ? topo.FloorCount : InitialFloorCount;
            var minFloor = _sim?.ElevatorMinFloor ?? 0;
            var maxFloor = _sim?.ElevatorMaxFloor ?? (fl - 1);
            _elevator.EnsureShaftViews(minFloor, maxFloor); _structure.EnsureFloorViews(topo); _room.EnsureRoomViews(topo);
            if (topo != null && topo.TryGetFloorSlab(0, out var ground)) _outside?.SyncGround(ground, fl);
            _elevator.EnsureElevatorViews(_sim?.ElevatorCarCount ?? 1); _resident.EnsureResidentViews(_sim?.ResidentCount ?? InitialResidentCount);
            if (_utilitiesNetworkLayer != null && _utilitiesNetworkLayer.IsVisible && _dataOverlays != null) _utilitiesNetworkLayer.UpdateOverlay(_dataOverlays.Utilities, topo);
        }

        private void HandleKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null) { if (kb.spaceKey.wasPressedThisFrame) _isPaused = !_isPaused; else if (kb.rKey.wasPressedThisFrame) ResetCommuteSimulation(); else if (kb.tKey.wasPressedThisFrame && _isPaused) _sim.AdvanceOneTick(); }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) _isPaused = !_isPaused; else if (Input.GetKeyDown(KeyCode.R)) ResetCommuteSimulation(); else if (Input.GetKeyDown(KeyCode.T) && _isPaused) _sim.AdvanceOneTick();
#endif
        }

        private void UpdateOverlayAndPredictions()
        {
            var c = _sim.CongestionProjection();
            if (_overlayPresenter.IsVisible) _overlayPresenter.UpdateOverlay(_dataOverlays.ElevatorWait);
            if (_satisfactionPresenter.IsVisible) _satisfactionPresenter.UpdateOverlay(_dataOverlays.Satisfaction);
            if (_populationPresenter.IsVisible) _populationPresenter.UpdateOverlay(_dataOverlays.Population);
            if (_scrutinyPresenter.IsVisible) _scrutinyPresenter.UpdateOverlay(_dataOverlays.Scrutiny);
            if (_footTrafficPresenter.IsVisible) _footTrafficPresenter.UpdateOverlay(_dataOverlays.FootTraffic);
            if (_businessHealthPresenter.IsVisible) _businessHealthPresenter.UpdateOverlay(_dataOverlays.BusinessHealth);
            if (_utilitiesPresenter.IsVisible) { _utilitiesPresenter.UpdateOverlay(_dataOverlays.Utilities); _utilitiesNetworkLayer.UpdateOverlay(_dataOverlays.Utilities, _sim?.TopologyProjection()); }
            if (_placementCard.IsOpen) _placementCard.SetPreview(_predictor.PredictAddition(c), OnConfirmElevatorPlacement);
        }

        public void InspectBottleneck() { _mode.SwitchMode(InteractionMode.Inspect); var c = _sim.CongestionProjection(); var ov = _dataOverlays.ElevatorWait; _inspectorCard.Inspect(new ElevatorCongestionInspectorProjection(0, "Floor 0 Elevator Congestion", $"Morning commute bottleneck: {c.TotalQueued} residents waiting.", ov.ContributingCauses, ov.RecommendedAction, true, "transit:elevator_car")); }

        public void ToggleDataOverlay() { if (_mode.CurrentMode == InteractionMode.Data) _mode.SwitchMode(InteractionMode.Inspect); else { _mode.SwitchMode(InteractionMode.Data); _mode.SetActiveOverlay("overlay:elevator_wait"); } }
        public void ShowSatisfactionOverlay() { _mode.SetActiveOverlay("overlay:satisfaction"); }
        public void ShowPopulationOverlay() { _mode.SetActiveOverlay("overlay:population"); }
        public void ShowScrutinyOverlay() { _mode.SetActiveOverlay("overlay:scrutiny"); }
        public void ShowFootTrafficOverlay() { _mode.SetActiveOverlay("overlay:foot_traffic"); }
        public void ShowBusinessHealthOverlay() { _mode.SetActiveOverlay("overlay:business_health"); }
        public void ShowUtilitiesOverlay() { _mode.SetActiveOverlay("overlay:utilities"); }
        private void OnUtilityNetworkSelected(UtilitiesNetworkLayerPresenter.NetworkKind network) => _utilitiesNetworkLayer.Select(network);
        public void InspectPopulationFloor(int floor)
        {
            _mode.SwitchMode(InteractionMode.Inspect);
            _inspectSelection.DetailCard.Inspect(new TowerInspectionService(_sim).InspectPopulationFloor(floor));
        }
        public void InspectScrutiny()
        {
            _mode.SwitchMode(InteractionMode.Inspect);
            _inspectSelection.DetailCard.Inspect(new TowerInspectionService(_sim).InspectScrutiny());
        }
        public void InspectUtilities()
        {
            _mode.SwitchMode(InteractionMode.Inspect);
            _inspectSelection.DetailCard.Inspect(new TowerInspectionService(_sim).InspectUtilities());
        }
        private void UpdatePlacementCard() => _placementCard.SetPreview(_predictor.PredictAddition(_sim.CongestionProjection()), OnConfirmElevatorPlacement);
        public void ShowPlacementPreview() { if (_mode.CurrentMode != InteractionMode.Build) _mode.SwitchMode(InteractionMode.Build); if (_mode.Projection().SelectedBuildTool != "transit:elevator_car") _mode.SelectBuildTool("transit:elevator_car"); UpdatePlacementCard(); }
        public void OnConfirmElevatorPlacement()
        {
            var result = _sim.AddCapacity();
            _placementCard.Close();
            if (result.Accepted) _elevator.EnsureElevatorViews(_sim.ElevatorCarCount);
        }

        public void ResetCommuteSimulation()
        {
            if (IsGroundStart) _sim.ResetToGroundFloorStart(); else { _sim.Reset(); SeedMorningRush(); }
            _dataOverlays = new TowerDataOverlays(_sim);
            if (_gridPlacement != null) _gridPlacement.SimulationSession = _sim;
            if (_inspectSelection != null) _inspectSelection.SimulationSession = _sim;
            _inspectorCard.Close(); _placementCard.Close(); _overlayPresenter.SetVisible(_mode.CurrentMode == InteractionMode.Data);
            ClearWorldGeometry(); CreateWorldGeometry();
        }

        private void SeedMorningRush() => _sim?.SeedMorningRush();
        private void CreateWorldGeometry() { ClearWorldGeometry(); UpdateCamera(resetView: true); SyncPresenterGeometry(); _atmosphere?.UpdateSoundscape(_sim.Projection(), _sim.TopologyProjection()); }
        private void UpdateCamera(bool resetView = false) => TowerCameraController.EnsureTowerCamera(_sim?.FloorCount ?? InitialFloorCount, _gridPlacement, resetView);
        private void ClearWorldGeometry() { _structure.Clear(); _outside?.Clear(); _elevator.Clear(); _room.Clear(); _resident.Clear(); _atmosphere?.Clear(); }
        private void RenderVisualSnapshot() { var projection = _sim.Projection(); SyncPresenterGeometry(); _elevator.UpdateElevatorPositions(projection); _resident.UpdateResidentPositions(projection, _sim.TopologyProjection(), Time.time, _room, _elevator); _atmosphere?.UpdateSoundscape(projection, _sim.TopologyProjection()); _atmosphere?.UpdateDayNight(_sim.DayPhase, _sim.FloorCount); _outside?.UpdateLighting(_sim.DayPhase); }
        private static Material CreateWorldMaterial() => new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? throw new MissingReferenceException("No unlit shader found."));
    }
}
