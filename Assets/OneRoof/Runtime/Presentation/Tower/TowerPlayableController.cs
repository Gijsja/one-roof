using System;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.Application.Overlays;
using OneRoof.Application.Prediction;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Weather;
using EntityId = OneRoof.Domain.Identity.EntityId;
using OneRoof.Presentation.Overlays;
using OneRoof.Presentation.Population;
using OneRoof.UI.Inspectors;
using OneRoof.UI.Modes;
using OneRoof.UI.Management;
using OneRoof.UI.Prediction;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Which simulation the scene boots: first-playable, ground start, or the city-scale playground.</summary>
    public enum TowerStartMode
    {
        StandardFiveFloor = 0,
        GroundFloorStart = 1,
        GoldStandardCity = 2
    }

    /// <summary>Presentation coordinator routing projection snapshots to four deep presenters.</summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public sealed class TowerPlayableController : MonoBehaviour
    {
        public const int InitialResidentCount = 50, InitialFloorCount = 5;

        public void SetPaused(bool paused) => _isPaused = paused;

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
        private NoiseOverlayPresenter _noisePresenter;
        private FactionTensionOverlayPresenter _factionTensionPresenter;
        private UtilitiesNetworkLayerPresenter _utilitiesNetworkLayer;
        private CongestionInspectorCardView _inspectorCard; private PlacementPreviewCardView _placementCard;
        private GridPlacementController _gridPlacement; private PlacementGhostPresenter _ghostPresenter;
        private InspectSelectionController _inspectSelection; private InspectOutlinePresenter _inspectOutline;
        private TowerDashboardHudView _hudView;
        private readonly ManagementOnboarding _onboarding = new ManagementOnboarding();
        private DecisionRecordView _decisionRecord;
        private TowerAtmospherePresenter _atmosphere;
        private OutsideCityPresenter _outside;
        private PixelRainPresenter _pixelRain;
        private UndergroundOperationsView _undergroundOperationsView;

        private readonly TowerStructurePresenter _structure = new TowerStructurePresenter();
        private readonly FloorDeckPresenter _floorDecks = new FloorDeckPresenter();
        private readonly BuildingExteriorPresenter _exterior = new BuildingExteriorPresenter();
        private readonly ElevatorBankPresenter _elevator = new ElevatorBankPresenter();
        private readonly RoomPresenter _room = new RoomPresenter();
        private readonly TowerResidentPresenter _resident = new TowerResidentPresenter();

        private Material _worldMat; private MaterialPropertyBlock _colorBlock;
        private float _tickAcc, _tickInterval = 0.35f; private bool _isPaused;
        private long _lastUndergroundCrewVersion = -1;
        private UtilitiesOverlayProjection _lastUtilityNetworkOverlay;
        private TowerTopologyProjection _lastUtilityNetworkTopology;

        public TowerSimulationSession SimulationSession => _sim; public TowerSimulationSession TransitSession => _sim;
        public ModeShellSession ModeSession => _mode; public TowerDataOverlays DataOverlays => _dataOverlays;
        public ElevatorWaitOverlayPresenter OverlayPresenter => _overlayPresenter; public ElevatorPlacementPredictor Predictor => _predictor;
        public SatisfactionOverlayPresenter SatisfactionPresenter => _satisfactionPresenter;
        public PopulationOverlayPresenter PopulationPresenter => _populationPresenter;
        public ScrutinyOverlayPresenter ScrutinyPresenter => _scrutinyPresenter;
        public UtilitiesOverlayPresenter UtilitiesPresenter => _utilitiesPresenter;
        public GridPlacementController GridPlacement => _gridPlacement; public PlacementGhostPresenter GhostPresenter => _ghostPresenter;
        public InspectSelectionController InspectSelection => _inspectSelection; public InspectOutlinePresenter InspectOutline => _inspectOutline;
        public TowerStructurePresenter StructurePresenter => _structure; public FloorDeckPresenter FloorDeckPresenter => _floorDecks; public BuildingExteriorPresenter ExteriorPresenter => _exterior; public ElevatorBankPresenter ElevatorPresenter => _elevator;
        public RoomPresenter RoomPresenter => _room; public TowerResidentPresenter ResidentPresenter => _resident;
        public TowerAtmospherePresenter AtmospherePresenter => _atmosphere;
        public PixelRainPresenter RainPresenter => _pixelRain;
        private MonthlyWeatherCycle _weatherCycle = new MonthlyWeatherCycle();
        private int _weatherOverride = -1; // -1: Auto, 0: Clear, 1: Drizzle, 2: Rain, 3: Storm, 4: Fog, 5: Snow
        private WeatherSample _currentWeather = new WeatherSample(WeatherCondition.Clear, 0f, 0f, "Clear Skies");
        private long _lastWeatherSampleTick = -1;

        public MonthlyWeatherCycle WeatherCycle => _weatherCycle;
        public WeatherSample CurrentWeather => _currentWeather;
        public int WeatherOverride => _weatherOverride;
        public bool IsAutoWeather => _weatherOverride < 0;
        public string WeatherModeDescription => _weatherOverride < 0 ? $"{_currentWeather.Description} (Auto)" : $"{_currentWeather.Description} (Manual)";
        public TowerStartMode StartMode => _startMode;
        public bool IsGoldStandardCity => _startMode == TowerStartMode.GoldStandardCity;
        public bool IsGroundStart => _startMode == TowerStartMode.GroundFloorStart;
        public bool IsPaused => _isPaused; public static float FloorY(int floor) => TowerStructurePresenter.FloorY(floor);
        public ManagementOnboarding Onboarding => _onboarding;

        public void Initialize()
        {
            if (_sim != null) return;
            _sim = IsGoldStandardCity ? TowerSimulationSession.CreateGoldStandardCity() :
                IsGroundStart ? TowerSimulationSession.CreateGroundFloorStart() : new TowerSimulationSession();
            // Seed weather from world hash for determinism per save
            var worldSeed = IsGoldStandardCity ? OneRoof.Domain.Topology.GoldStandardCityFixture.Seed : (ulong)(uint)_sim.GetHashCode();
            if (worldSeed == 0) worldSeed = 0xDEADBEEFCAFEUL;
            _weatherCycle = new MonthlyWeatherCycle(worldSeed);
            _lastWeatherSampleTick = -1; // force re-sample on first frame
            if (!IsGroundStart && !IsGoldStandardCity) SeedMorningRush();
            _mode = new ModeShellSession();
            _dataOverlays = new TowerDataOverlays(_sim); _predictor = new ElevatorPlacementPredictor();
            _colorBlock = new MaterialPropertyBlock(); _worldMat = CreateWorldMaterial();

            _structure.Initialize(transform, _worldMat, _colorBlock);
            _floorDecks.Initialize(transform, _worldMat, _colorBlock);
            _structure.BindFloorDeckPresenter(_floorDecks);
            _exterior.Initialize(transform, _worldMat, _colorBlock);
            _elevator.Initialize(transform, _worldMat, _colorBlock);
            _room.Initialize(transform, _worldMat, _colorBlock); _resident.ViewPool = Ensure<NpcViewPool>(); _resident.Initialize(transform);
            InitSubcomponents(); CreateWorldGeometry();
        }

        private void Awake() => Initialize();
        private void OnEnable()
        {
            Initialize();
            SubscribeEvents();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void SubscribeEvents()
        {
            if (_mode != null) { _mode.ModeChanged -= OnModeChanged; _mode.ModeChanged += OnModeChanged; }
            if (_gridPlacement != null) { _gridPlacement.PlacementExecuted -= OnPlacement; _gridPlacement.PlacementExecuted += OnPlacement; }
            if (_populationPresenter != null) { _populationPresenter.FloorInspectionRequested -= InspectPopulationFloor; _populationPresenter.FloorInspectionRequested += InspectPopulationFloor; }
            if (_scrutinyPresenter != null) { _scrutinyPresenter.InspectionRequested -= InspectScrutiny; _scrutinyPresenter.InspectionRequested += InspectScrutiny; }
            if (_utilitiesPresenter != null)
            {
                _utilitiesPresenter.InspectionRequested -= InspectUtilities;
                _utilitiesPresenter.InspectionRequested += InspectUtilities;
                _utilitiesPresenter.NetworkSelected -= OnUtilityNetworkSelected;
                _utilitiesPresenter.NetworkSelected += OnUtilityNetworkSelected;
            }
            if (_noisePresenter != null) { _noisePresenter.RoomInspectionRequested -= InspectNoiseSource; _noisePresenter.RoomInspectionRequested += InspectNoiseSource; }
            if (_factionTensionPresenter != null) { _factionTensionPresenter.FloorInspectionRequested -= InspectFactionFloor; _factionTensionPresenter.FloorInspectionRequested += InspectFactionFloor; }
            if (_decisionRecord != null)
            {
                _decisionRecord.ResidentInspectionRequested -= InspectDecisionResident;
                _decisionRecord.ResidentInspectionRequested += InspectDecisionResident;
                _decisionRecord.BusinessInspectionRequested -= InspectDecisionBusiness;
                _decisionRecord.BusinessInspectionRequested += InspectDecisionBusiness;
                _decisionRecord.FloorInspectionRequested -= InspectFactionFloor;
                _decisionRecord.FloorInspectionRequested += InspectFactionFloor;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_mode != null) _mode.ModeChanged -= OnModeChanged;
            if (_gridPlacement != null) _gridPlacement.PlacementExecuted -= OnPlacement;
            if (_populationPresenter != null) _populationPresenter.FloorInspectionRequested -= InspectPopulationFloor;
            if (_scrutinyPresenter != null) _scrutinyPresenter.InspectionRequested -= InspectScrutiny;
            if (_utilitiesPresenter != null)
            {
                _utilitiesPresenter.InspectionRequested -= InspectUtilities;
                _utilitiesPresenter.NetworkSelected -= OnUtilityNetworkSelected;
            }
            if (_noisePresenter != null) _noisePresenter.RoomInspectionRequested -= InspectNoiseSource;
            if (_factionTensionPresenter != null) _factionTensionPresenter.FloorInspectionRequested -= InspectFactionFloor;
            if (_decisionRecord != null)
            {
                _decisionRecord.ResidentInspectionRequested -= InspectDecisionResident;
                _decisionRecord.BusinessInspectionRequested -= InspectDecisionBusiness;
                _decisionRecord.FloorInspectionRequested -= InspectFactionFloor;
            }
        }

        private void OnDestroy() { OnDisable(); _atmosphere?.Clear(); _outside?.Clear(); _exterior.Dispose(); if (_worldMat != null) { if (UnityEngine.Application.isPlaying) Destroy(_worldMat); else DestroyImmediate(_worldMat); } }

        private bool _needsVisualSnapshot = true;

        private void Update()
        {
            if (!UnityEngine.Application.isPlaying) return;
            if (_sim == null) Initialize();
            HandleKeyboard();
            var tickAdvanced = false;
            if (!_isPaused && (_tickAcc += Time.deltaTime) >= _tickInterval)
            {
                _tickAcc -= _tickInterval;
                _sim.AdvanceOneTick();
                tickAdvanced = true;
            }

            if (tickAdvanced || _needsVisualSnapshot)
            {
                UpdateOverlayAndPredictions();
            }

            RenderVisualSnapshot(tickAdvanced || _needsVisualSnapshot);
            _needsVisualSnapshot = false;
        }

        private void InitSubcomponents()
        {
            (_modeBar = Ensure<ModeShellBarController>()).Session = _mode;
            Ensure<PolicyDecreePanelView>().Bind(_sim, _mode);
            (_decisionRecord = Ensure<DecisionRecordView>()).Bind(_sim, _mode);
            (_overlayPresenter = Ensure<ElevatorWaitOverlayPresenter>()).SetVisible(false);
            (_satisfactionPresenter = Ensure<SatisfactionOverlayPresenter>()).SetVisible(false);
            (_populationPresenter = Ensure<PopulationOverlayPresenter>()).SetVisible(false);
            (_scrutinyPresenter = Ensure<ScrutinyOverlayPresenter>()).SetVisible(false);
            (_footTrafficPresenter = Ensure<FootTrafficOverlayPresenter>()).SetVisible(false);
            (_businessHealthPresenter = Ensure<BusinessHealthOverlayPresenter>()).SetVisible(false);
            (_utilitiesPresenter = Ensure<UtilitiesOverlayPresenter>()).SetVisible(false);
            (_noisePresenter = Ensure<NoiseOverlayPresenter>()).SetVisible(false);
            (_factionTensionPresenter = Ensure<FactionTensionOverlayPresenter>()).SetVisible(false);
            (_utilitiesNetworkLayer = Ensure<UtilitiesNetworkLayerPresenter>()).SetVisible(false);
            (_inspectorCard = Ensure<CongestionInspectorCardView>()).Session = _mode;
            _placementCard = Ensure<PlacementPreviewCardView>(); _ghostPresenter = Ensure<PlacementGhostPresenter>();
            _gridPlacement = Ensure<GridPlacementController>(); _gridPlacement.ModeSession = _mode;
            _gridPlacement.SimulationSession = _sim; _gridPlacement.GhostPresenter = _ghostPresenter;
            (_undergroundOperationsView = Ensure<UndergroundOperationsView>()).Bind(_sim, _mode, _gridPlacement);
            (_hudView = Ensure<TowerDashboardHudView>()).Controller = this;
            Ensure<ManagementOnboardingView>().Lesson = _onboarding;
            _inspectOutline = Ensure<InspectOutlinePresenter>(); (_inspectSelection = Ensure<InspectSelectionController>()).ModeSession = _mode;
            _inspectSelection.SimulationSession = _sim; _inspectSelection.RoomPresenter = _room; _inspectSelection.ElevatorPresenter = _elevator;
            _inspectSelection.ResidentPresenter = _resident; _inspectSelection.OutlinePresenter = _inspectOutline;
            _atmosphere = Ensure<TowerAtmospherePresenter>(); _atmosphere.Initialize();
            var camera = TowerCameraController.EnsureTowerCamera(_sim.FloorCount, _gridPlacement, resetView: true);
            _outside = Ensure<OutsideCityPresenter>(); _outside.Initialize(camera, _worldMat);
            _pixelRain = Ensure<PixelRainPresenter>();
            if (IsGroundStart && _mode.CurrentMode != InteractionMode.Build) _mode.SwitchMode(InteractionMode.Build);
        }

        private T Ensure<T>() where T : Component => GetComponent<T>() ?? gameObject.AddComponent<T>();

        private void OnModeChanged(ModeShellProjection p)
        {
            if (p.IsDataMode) _onboarding.ObserveOverlay(p.ActiveOverlayId);
            if (p.IsManageMode) _onboarding.ObserveManageMode(true);
            var satisfaction = p.IsDataMode && p.ActiveOverlayId == "overlay:satisfaction";
            var population = p.IsDataMode && p.ActiveOverlayId == "overlay:population";
            var scrutiny = p.IsDataMode && p.ActiveOverlayId == "overlay:scrutiny";
            var footTraffic = p.IsDataMode && p.ActiveOverlayId == "overlay:foot_traffic";
            var businessHealth = p.IsDataMode && p.ActiveOverlayId == "overlay:business_health";
            var utilities = p.IsDataMode && p.ActiveOverlayId == "overlay:utilities";
            var noise = p.IsDataMode && p.ActiveOverlayId == "overlay:noise";
            var factionTension = p.IsDataMode && p.ActiveOverlayId == "overlay:faction_tension";
            _overlayPresenter.SetVisible(p.IsDataMode && !satisfaction && !population && !scrutiny && !footTraffic && !businessHealth && !utilities && !noise && !factionTension); _satisfactionPresenter.SetVisible(satisfaction); _populationPresenter.SetVisible(population); _scrutinyPresenter.SetVisible(scrutiny); _footTrafficPresenter.SetVisible(footTraffic); _businessHealthPresenter.SetVisible(businessHealth); _utilitiesPresenter.SetVisible(utilities); _noisePresenter.SetVisible(noise); _factionTensionPresenter.SetVisible(factionTension);
            _utilitiesNetworkLayer.SetVisible(utilities);
            if (satisfaction) _satisfactionPresenter.UpdateOverlay(_dataOverlays.Satisfaction);
            else if (population) _populationPresenter.UpdateOverlay(_dataOverlays.Population);
            else if (scrutiny) _scrutinyPresenter.UpdateOverlay(_dataOverlays.Scrutiny);
            else if (footTraffic) _footTrafficPresenter.UpdateOverlay(_dataOverlays.FootTraffic);
            else if (businessHealth) _businessHealthPresenter.UpdateOverlay(_dataOverlays.BusinessHealth);
            else if (utilities) UpdateUtilitiesOverlay();
            else if (noise) _noisePresenter.UpdateOverlay(_dataOverlays.Noise);
            else if (factionTension) _factionTensionPresenter.UpdateOverlay(_dataOverlays.FactionTension, _sim.TopologyProjection());
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
            _elevator.EnsureShaftViews(minFloor, maxFloor); _structure.EnsureFloorViews(topo); _exterior.EnsureExteriorViews(topo, _sim?.UndergroundProjection()); _room.EnsureRoomViews(topo);
            _lastUndergroundCrewVersion = -1;
            if (!IsGoldStandardCity && topo != null && topo.TryGetFloorSlab(0, out var ground)) _outside?.SyncGround(ground, fl);
            _pixelRain?.SyncTopology(topo);
            _elevator.EnsureElevatorViews(_sim?.ElevatorCarCount ?? 1); _resident.EnsureResidentViews(_sim?.ResidentCount ?? InitialResidentCount);
            if (_utilitiesNetworkLayer != null && _utilitiesNetworkLayer.IsVisible && _dataOverlays != null) UpdateUtilitiesOverlay(topo);
        }

        private void HandleKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null) { if (kb.spaceKey.wasPressedThisFrame) _isPaused = !_isPaused; else if (kb.rKey.wasPressedThisFrame) ResetCommuteSimulation(); else if (kb.wKey.wasPressedThisFrame) CycleWeatherOverride(); else if (kb.tKey.wasPressedThisFrame && _isPaused) _sim.AdvanceOneTick(); }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) _isPaused = !_isPaused; else if (Input.GetKeyDown(KeyCode.R)) ResetCommuteSimulation(); else if (Input.GetKeyDown(KeyCode.W)) CycleWeatherOverride(); else if (Input.GetKeyDown(KeyCode.T) && _isPaused) _sim.AdvanceOneTick();
#endif
        }

        private void UpdateOverlayAndPredictions()
        {
            var c = _sim.CongestionProjection();
            _onboarding.ObserveQueue(c.TotalQueued, c.AverageWaitTicks);
            _onboarding.ObserveMeasuredWait(c.AverageWaitTicks);
            _onboarding.ObserveMeasuredQueue(c.TotalQueued);
            if (_overlayPresenter.IsVisible) _overlayPresenter.UpdateOverlay(_dataOverlays.ElevatorWait);
            if (_satisfactionPresenter.IsVisible) _satisfactionPresenter.UpdateOverlay(_dataOverlays.Satisfaction);
            if (_populationPresenter.IsVisible) _populationPresenter.UpdateOverlay(_dataOverlays.Population);
            if (_scrutinyPresenter.IsVisible) _scrutinyPresenter.UpdateOverlay(_dataOverlays.Scrutiny);
            if (_footTrafficPresenter.IsVisible) _footTrafficPresenter.UpdateOverlay(_dataOverlays.FootTraffic);
            if (_businessHealthPresenter.IsVisible) _businessHealthPresenter.UpdateOverlay(_dataOverlays.BusinessHealth);
            if (_utilitiesPresenter.IsVisible) UpdateUtilitiesOverlay();
            if (_noisePresenter.IsVisible) _noisePresenter.UpdateOverlay(_dataOverlays.Noise);
            if (_factionTensionPresenter.IsVisible) _factionTensionPresenter.UpdateOverlay(_dataOverlays.FactionTension, _sim.TopologyProjection());
            if (_placementCard.IsOpen) _placementCard.SetPreview(_predictor.PredictAddition(c), OnConfirmElevatorPlacement);
        }

        private void UpdateUtilitiesOverlay(TowerTopologyProjection topology = null)
        {
            var overlay = _dataOverlays.Utilities;
            _utilitiesPresenter.UpdateOverlay(overlay);
            topology = topology ?? _sim?.TopologyProjection();
            if (ReferenceEquals(overlay, _lastUtilityNetworkOverlay) &&
                ReferenceEquals(topology, _lastUtilityNetworkTopology)) return;

            _utilitiesNetworkLayer.UpdateOverlay(overlay, topology);
            _lastUtilityNetworkOverlay = overlay;
            _lastUtilityNetworkTopology = topology;
        }

        public void InspectBottleneck() { _mode.SwitchMode(InteractionMode.Inspect); var c = _sim.CongestionProjection(); var ov = _dataOverlays.ElevatorWait; _inspectorCard.Inspect(new ElevatorCongestionInspectorProjection(0, "Floor 0 Elevator Congestion", $"Morning commute bottleneck: {c.TotalQueued} residents waiting.", ov.ContributingCauses, ov.RecommendedAction, true, "transit:elevator_car")); _onboarding.ObserveInspector(true); }

        public void ToggleDataOverlay() { if (_mode.CurrentMode == InteractionMode.Data) _mode.SwitchMode(InteractionMode.Inspect); else { _mode.SwitchMode(InteractionMode.Data); _mode.SetActiveOverlay("overlay:elevator_wait"); } }
        public void ShowSatisfactionOverlay() { _mode.SetActiveOverlay("overlay:satisfaction"); }
        public void ShowPopulationOverlay() { _mode.SetActiveOverlay("overlay:population"); }
        public void ShowScrutinyOverlay() { _mode.SetActiveOverlay("overlay:scrutiny"); }
        public void ShowFootTrafficOverlay() { _mode.SetActiveOverlay("overlay:foot_traffic"); }
        public void ShowBusinessHealthOverlay() { _mode.SetActiveOverlay("overlay:business_health"); }
        public void ShowUtilitiesOverlay() { _mode.SetActiveOverlay("overlay:utilities"); }
        public void ShowNoiseOverlay() { _mode.SetActiveOverlay("overlay:noise"); }
        public void ShowFactionTensionOverlay() { _mode.SetActiveOverlay("overlay:faction_tension"); }
        private void InspectNoiseSource(EntityId roomId)
        {
            _mode.SwitchMode(InteractionMode.Inspect);
            _inspectSelection.ShowDetails(InspectTargetKind.Room, roomId.Value);
        }
        private void InspectFactionFloor(int floor)
        {
            _mode.SwitchMode(InteractionMode.Inspect);
            _inspectSelection.DetailCard.Inspect(new TowerInspectionService(_sim).InspectFactionFloor(floor));
        }
        private void InspectDecisionResident(int residentId)
        {
            _mode.SwitchMode(InteractionMode.Inspect);
            _inspectSelection.ShowDetails(InspectTargetKind.Resident, residentId);
        }
        private void InspectDecisionBusiness(int businessId)
        {
            _mode.SwitchMode(InteractionMode.Inspect);
            _inspectSelection.DetailCard.Inspect(new TowerInspectionService(_sim).InspectBusiness(businessId));
        }
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
        private void UpdatePlacementCard()
        {
            var preview = _predictor.PredictAddition(_sim.CongestionProjection());
            _placementCard.SetPreview(preview, OnConfirmElevatorPlacement);
            _onboarding.ObservePreview(preview.IsValid, preview.EstimatedImprovementPercentage);
        }
        public void ShowPlacementPreview() { if (_mode.CurrentMode != InteractionMode.Build) _mode.SwitchMode(InteractionMode.Build); if (_mode.Projection().SelectedBuildTool != "transit:elevator_car") _mode.SelectBuildTool("transit:elevator_car"); UpdatePlacementCard(); }
        public void OnConfirmElevatorPlacement()
        {
            var result = _sim.AddCapacity();
            _onboarding.ObserveCapacityBuilt(result.Accepted);
            _placementCard.Close();
            if (result.Accepted) _elevator.EnsureElevatorViews(_sim.ElevatorCarCount);
        }

        public void ResetCommuteSimulation()
        {
            if (IsGoldStandardCity) _sim.ResetToGoldStandardCity(); else if (IsGroundStart) _sim.ResetToGroundFloorStart(); else { _sim.Reset(); SeedMorningRush(); }
            _dataOverlays = new TowerDataOverlays(_sim);
            _lastUtilityNetworkOverlay = null;
            _lastUtilityNetworkTopology = null;
            _lastWeatherSampleTick = -1;
            if (_gridPlacement != null) _gridPlacement.SimulationSession = _sim;
            if (_inspectSelection != null) _inspectSelection.SimulationSession = _sim;
            _inspectorCard.Close(); _placementCard.Close(); _overlayPresenter.SetVisible(_mode.CurrentMode == InteractionMode.Data);
            ClearWorldGeometry(); CreateWorldGeometry();
        }

        private void SeedMorningRush() => _sim?.SeedMorningRush();
        private void CreateWorldGeometry()
        {
            ClearWorldGeometry();
            var camera = UpdateCamera(resetView: true);
            _pixelRain?.Initialize(camera);
            SyncPresenterGeometry();
            _atmosphere?.UpdateSoundscape(_sim.Projection(), _sim.TopologyProjection());
            // The authored city is already built; finish startup fades before its first frame.
            // Subsequent room placements still use the normal construction transition.
            if (IsGoldStandardCity)
                foreach (var effect in GetComponentsInChildren<VisualEffectsPresenter>()) effect.Advance(1f);
        }
        private Camera UpdateCamera(bool resetView = false) =>
            TowerCameraController.EnsureTowerCamera(_sim?.FloorCount ?? InitialFloorCount, _gridPlacement, resetView);
        private void ClearWorldGeometry() { _structure.Clear(); _floorDecks?.Clear(); _exterior.Clear(); _pixelRain?.Clear(); _outside?.Clear(); _elevator.Clear(); _room.Clear(); _resident.Clear(); _atmosphere?.Clear(); }
        public void CycleWeatherOverride()
        {
            _weatherOverride++;
            if (_weatherOverride > 5) _weatherOverride = -1;
            UpdateActiveWeather();
        }

        public void SetWeatherOverride(int conditionIndex)
        {
            _weatherOverride = conditionIndex >= -1 && conditionIndex <= 5 ? conditionIndex : -1;
            UpdateActiveWeather();
        }

        public void UpdateActiveWeather()
        {
            if (_weatherOverride >= 0)
            {
                // Manual override — always apply
                var cond = (WeatherCondition)_weatherOverride;
                var intensity = cond switch
                {
                    WeatherCondition.Clear => 0f,
                    WeatherCondition.Drizzle => 0.25f,
                    WeatherCondition.Rain => 0.65f,
                    WeatherCondition.Storm => 1.0f,
                    WeatherCondition.Fog => 0.40f,
                    WeatherCondition.Snow => 0.50f,
                    _ => 0f
                };
                var wind = cond switch
                {
                    WeatherCondition.Storm => -2.2f,
                    WeatherCondition.Rain => -0.9f,
                    WeatherCondition.Drizzle => -0.3f,
                    _ => 0f
                };
                _currentWeather = new WeatherSample(cond, intensity, wind, WeatherSample.DefaultDescription(cond));
            }
            else if (_sim != null)
            {
                // Auto mode — only re-sample when the tick advances (not every render frame)
                var tick = _sim.CurrentTick;
                if (tick != _lastWeatherSampleTick)
                {
                    _currentWeather = _weatherCycle.Sample(tick);
                    _lastWeatherSampleTick = tick;
                }
            }

            _pixelRain?.SetWeather(_currentWeather);
        }

        private void RenderVisualSnapshot(bool tickAdvanced)
        {
            var projection = _sim.Projection();
            if (tickAdvanced)
            {
                if (_lastUndergroundCrewVersion != _sim.Version)
                {
                    _exterior.SyncUndergroundCrew(_sim.UndergroundOperationsProjection());
                    _lastUndergroundCrewVersion = _sim.Version;
                }
                _room.UpdateHousingConditions(_sim.HousingProjection());
                _atmosphere?.UpdateSoundscape(projection, _sim.TopologyProjection());
                _atmosphere?.UpdateDayNight(_sim.DayPhase, _sim.FloorCount);
                _outside?.UpdateLighting(_sim.DayPhase);
                _exterior.UpdateLighting(_sim.DayPhase);
                UpdateActiveWeather();
                _pixelRain?.UpdateLighting(_sim.DayPhase);
            }
            _elevator.UpdateElevatorPositions(projection);
            _resident.UpdateResidentPositions(projection, _sim.TopologyProjection(), Time.time, _room, _elevator,
                _exterior.VisibleUndergroundCrewResidentIds);
            _pixelRain?.UpdateWeather(Time.deltaTime);
            _exterior.AnimateUndergroundCrew(Time.time);
        }
        private static Material CreateWorldMaterial()
        {
            var authored = Resources.Load<Material>("Materials/OneRoofWorldMaterial");
            if (authored != null) return new Material(authored) { name = "Tower World Material" };

            var shader = Shader.Find("OneRoof/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default")
                ?? throw new MissingReferenceException("No unlit shader found.");

            return new Material(shader) { name = "Tower World Material" };
        }
    }
}
