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
    /// <summary>Presentation coordinator routing projection snapshots to four deep presenters.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TowerPlayableController : MonoBehaviour
    {
        public const int InitialResidentCount = 50, InitialFloorCount = 5;

        private TowerSimulationSession _sim; private ModeShellSession _mode;
        private ElevatorWaitOverlayService _overlaySvc; private ElevatorPlacementPredictor _predictor;
        private ModeShellBarController _modeBar; private ElevatorWaitOverlayPresenter _overlayPresenter;
        private SatisfactionOverlayService _satisfactionService; private SatisfactionOverlayPresenter _satisfactionPresenter;
        private CongestionInspectorCardView _inspectorCard; private PlacementPreviewCardView _placementCard;
        private GridPlacementController _gridPlacement; private PlacementGhostPresenter _ghostPresenter;
        private InspectSelectionController _inspectSelection; private InspectOutlinePresenter _inspectOutline;
        private TowerDashboardHudView _hudView;

        private readonly TowerStructurePresenter _structure = new TowerStructurePresenter();
        private readonly ElevatorBankPresenter _elevator = new ElevatorBankPresenter();
        private readonly RoomPresenter _room = new RoomPresenter();
        private readonly TowerResidentPresenter _resident = new TowerResidentPresenter();

        private Material _worldMat; private MaterialPropertyBlock _colorBlock;
        private float _tickAcc, _tickInterval = 0.35f; private bool _isPaused;

        public TowerSimulationSession SimulationSession => _sim; public TowerSimulationSession TransitSession => _sim;
        public ModeShellSession ModeSession => _mode; public ElevatorWaitOverlayService OverlayService => _overlaySvc;
        public ElevatorWaitOverlayPresenter OverlayPresenter => _overlayPresenter; public ElevatorPlacementPredictor Predictor => _predictor;
        public SatisfactionOverlayPresenter SatisfactionPresenter => _satisfactionPresenter;
        public GridPlacementController GridPlacement => _gridPlacement; public PlacementGhostPresenter GhostPresenter => _ghostPresenter;
        public InspectSelectionController InspectSelection => _inspectSelection; public InspectOutlinePresenter InspectOutline => _inspectOutline;
        public TowerStructurePresenter StructurePresenter => _structure; public ElevatorBankPresenter ElevatorPresenter => _elevator;
        public RoomPresenter RoomPresenter => _room; public TowerResidentPresenter ResidentPresenter => _resident;
        public bool IsPaused => _isPaused; public static float FloorY(int floor) => TowerStructurePresenter.FloorY(floor);

        public void Initialize()
        {
            if (_sim != null) return;
            _sim = new TowerSimulationSession(); SeedMorningRush(); _mode = new ModeShellSession();
            _overlaySvc = new ElevatorWaitOverlayService(); _predictor = new ElevatorPlacementPredictor();
            _satisfactionService = new SatisfactionOverlayService();
            _colorBlock = new MaterialPropertyBlock(); _worldMat = CreateWorldMaterial();

            _structure.Initialize(transform, _worldMat, _colorBlock); _elevator.Initialize(transform, _worldMat, _colorBlock);
            _room.Initialize(transform, _worldMat, _colorBlock); _resident.Initialize(transform);
            InitSubcomponents(); _mode.ModeChanged += OnModeChanged; CreateWorldGeometry();
        }

        private void Awake() => Initialize(); private void OnEnable() => Initialize();
        private void OnDisable() { if (_mode != null) _mode.ModeChanged -= OnModeChanged; if (_gridPlacement != null) _gridPlacement.PlacementExecuted -= OnPlacement; }
        private void OnDestroy() { OnDisable(); if (_worldMat != null) { if (UnityEngine.Application.isPlaying) Destroy(_worldMat); else DestroyImmediate(_worldMat); } }

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
            (_inspectorCard = Ensure<CongestionInspectorCardView>()).Session = _mode;
            _placementCard = Ensure<PlacementPreviewCardView>(); _ghostPresenter = Ensure<PlacementGhostPresenter>();
            _gridPlacement = Ensure<GridPlacementController>(); _gridPlacement.ModeSession = _mode;
            _gridPlacement.SimulationSession = _sim; _gridPlacement.GhostPresenter = _ghostPresenter;
            _gridPlacement.PlacementExecuted += OnPlacement; (_hudView = Ensure<TowerDashboardHudView>()).Controller = this;
            _inspectOutline = Ensure<InspectOutlinePresenter>(); (_inspectSelection = Ensure<InspectSelectionController>()).ModeSession = _mode;
            _inspectSelection.SimulationSession = _sim; _inspectSelection.RoomPresenter = _room; _inspectSelection.ElevatorPresenter = _elevator;
            _inspectSelection.ResidentPresenter = _resident; _inspectSelection.OutlinePresenter = _inspectOutline;
            TowerCameraController.EnsureTowerCamera(_sim.FloorCount, _gridPlacement, resetView: true);
        }

        private T Ensure<T>() where T : Component => GetComponent<T>() ?? gameObject.AddComponent<T>();

        private void OnModeChanged(ModeShellProjection p)
        {
            var satisfaction = p.IsDataMode && p.ActiveOverlayId == "overlay:satisfaction";
            _overlayPresenter.SetVisible(p.IsDataMode && !satisfaction); _satisfactionPresenter.SetVisible(satisfaction);
            if (satisfaction) _satisfactionPresenter.UpdateOverlay(_satisfactionService.CreateOverlay(_sim));
            else if (p.IsDataMode) _overlayPresenter.UpdateOverlay(_overlaySvc.CreateOverlay(_sim.CongestionProjection()));
            if (p.IsBuildMode && p.SelectedBuildTool == "transit:elevator_car") UpdatePlacementCard();
            else if (!p.IsBuildMode && _placementCard.IsOpen) _placementCard.Close();
            if (!p.IsInspectMode && _inspectorCard.IsOpen) _inspectorCard.Close();
        }

        private void OnPlacement(CommandResult r) { if (r.Accepted) { UpdateCamera(resetView: false); SyncPresenterGeometry(); } }

        public void SyncPresenterGeometry()
        {
            var topo = _sim?.Topology; var fl = topo != null ? topo.FloorCount : InitialFloorCount;
            var minFloor = _sim?.ElevatorBank?.MinFloor ?? 0;
            var maxFloor = _sim?.ElevatorBank?.MaxFloor ?? (fl - 1);
            _elevator.EnsureShaftViews(minFloor, maxFloor); _structure.EnsureFloorViews(topo); _room.EnsureRoomViews(topo);
            _elevator.EnsureElevatorViews(_sim?.ElevatorBank.Cars.Count ?? 1); _resident.EnsureResidentViews(_sim?.ResidentCount ?? InitialResidentCount);
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
            if (_overlayPresenter.IsVisible) _overlayPresenter.UpdateOverlay(_overlaySvc.CreateOverlay(c));
            if (_satisfactionPresenter.IsVisible) _satisfactionPresenter.UpdateOverlay(_satisfactionService.CreateOverlay(_sim));
            if (_placementCard.IsOpen) _placementCard.SetPreview(_predictor.PredictAddition(c), OnConfirmElevatorPlacement);
        }

        public void InspectBottleneck() { _mode.SwitchMode(InteractionMode.Inspect); var c = _sim.CongestionProjection(); var ov = _overlaySvc.CreateOverlay(c); _inspectorCard.Inspect(new ElevatorCongestionInspectorProjection(0, "Floor 0 Elevator Congestion", $"Morning commute bottleneck: {c.TotalQueued} residents waiting.", ov.ContributingCauses, ov.RecommendedAction, true, "transit:elevator_car")); }

        public void ToggleDataOverlay() { if (_mode.CurrentMode == InteractionMode.Data) _mode.SwitchMode(InteractionMode.Inspect); else { _mode.SwitchMode(InteractionMode.Data); _mode.SetActiveOverlay("overlay:elevator_wait"); } }
        public void ShowSatisfactionOverlay() { _mode.SetActiveOverlay("overlay:satisfaction"); }
        private void UpdatePlacementCard() => _placementCard.SetPreview(_predictor.PredictAddition(_sim.CongestionProjection()), OnConfirmElevatorPlacement);
        public void ShowPlacementPreview() { if (_mode.CurrentMode != InteractionMode.Build) _mode.SwitchMode(InteractionMode.Build); if (_mode.Projection().SelectedBuildTool != "transit:elevator_car") _mode.SelectBuildTool("transit:elevator_car"); UpdatePlacementCard(); }
        public void OnConfirmElevatorPlacement() { _sim.AddCapacity(); _placementCard.Close(); _elevator.EnsureElevatorViews(_sim.ElevatorBank.Cars.Count); }

        public void ResetCommuteSimulation()
        {
            _sim.Reset(); SeedMorningRush(); if (_gridPlacement != null) _gridPlacement.SimulationSession = _sim;
            if (_inspectSelection != null) _inspectSelection.SimulationSession = _sim;
            _inspectorCard.Close(); _placementCard.Close(); _overlayPresenter.SetVisible(_mode.CurrentMode == InteractionMode.Data);
            ClearWorldGeometry(); CreateWorldGeometry();
        }

        private void SeedMorningRush() => _sim?.SeedMorningRush();
        private void CreateWorldGeometry() { ClearWorldGeometry(); UpdateCamera(resetView: true); SyncPresenterGeometry(); }
        private void UpdateCamera(bool resetView = false) => TowerCameraController.EnsureTowerCamera(_sim?.FloorCount ?? InitialFloorCount, _gridPlacement, resetView);
        private void ClearWorldGeometry() { _structure.Clear(); _elevator.Clear(); _room.Clear(); _resident.Clear(); }
        private void RenderVisualSnapshot() { SyncPresenterGeometry(); _elevator.UpdateElevatorPositions(_sim.Projection()); _resident.UpdateResidentPositions(_sim.Projection(), _sim.Topology, Time.time, _room); }
        private static Material CreateWorldMaterial() => new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? throw new MissingReferenceException("No unlit shader found."));
    }
}
