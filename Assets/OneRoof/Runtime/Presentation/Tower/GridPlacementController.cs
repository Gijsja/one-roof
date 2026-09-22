using System;
using OneRoof.Application.Modes;
using OneRoof.Application.Prediction;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.UI.Modes;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Presentation controller managing interactive grid cell hovering and placement in Build mode.
    /// Converts screen and world raycasts to discrete CellCoordinates (floor, cellX), validates
    /// placement against building slabs, room overlaps, and treasury funds, and dispatches building commands.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridPlacementController : MonoBehaviour
    {
        public const float DefaultFloorOriginY = -3.2f;
        public const float DefaultFloorHeight = 1.75f;
        public const float DefaultCellOriginX = -2.4f;
        public const float DefaultCellWidth = 0.5f;

        public const int DefaultFloorSlabMinX = -14;
        public const int DefaultFloorSlabMaxX = 17;
        public const int GroundSlabExpansionWidth = 6;

        [SerializeField] private float _floorOriginY = DefaultFloorOriginY;
        [SerializeField] private float _floorHeight = DefaultFloorHeight;
        [SerializeField] private float _cellOriginX = DefaultCellOriginX;
        [SerializeField] private float _cellWidth = DefaultCellWidth;

        private ModeShellSession _modeSession;
        private TowerSimulationSession _simulationSession;
        private PlacementGhostPresenter _ghostPresenter;
        private Camera _camera;

        public ModeShellSession ModeSession
        {
            get => _modeSession;
            set => _modeSession = value;
        }

        public TowerSimulationSession SimulationSession
        {
            get => _simulationSession;
            set => _simulationSession = value;
        }

        public PlacementGhostPresenter GhostPresenter
        {
            get => _ghostPresenter ?? (_ghostPresenter = GetComponent<PlacementGhostPresenter>() ?? gameObject.AddComponent<PlacementGhostPresenter>());
            set => _ghostPresenter = value;
        }

        public Camera Camera
        {
            get
            {
                if (_camera != null) return _camera;
                _camera = Camera.main;
                if (_camera == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
#else
                    _camera = UnityEngine.Object.FindObjectOfType<Camera>();
#endif
                }
                return _camera;
            }
            set => _camera = value;
        }

        public float FloorOriginY => _floorOriginY;
        public float FloorHeight => _floorHeight;
        public float CellOriginX => _cellOriginX;
        public float CellWidth => _cellWidth;

        /// <summary>Most recent hover validation outcome for Build-mode hint UI.</summary>
        public bool LastPlacementValid { get; private set; } = true;

        /// <summary>Human-readable hover hint: validity plus the blocking reason when invalid.</summary>
        public string LastPlacementHint { get; private set; }

        /// <summary>
        /// Pure hint formatter (Docs/04_UX_CONTRACT.md): every invalid hover must
        /// name its blocking condition; every valid hover names the target.
        /// The ✖/✔ prefix keeps meaning off the colour channel.
        /// </summary>
        public static string FormatPlacementHint(string toolId, int floor, CellBounds bounds, bool isValid, string failureReason)
        {
            if (isValid)
            {
                return $"✔ {toolId}: floor {floor}, cells {bounds.MinX}–{bounds.MaxX}";
            }
            var reason = string.IsNullOrEmpty(failureReason) ? "Placement invalid." : failureReason;
            return $"✖ {reason}";
        }

        public event Action<CommandResult> PlacementExecuted;

        private int? _suppressedCellFloor;
        private int? _suppressedCellX;

        // Tracks the last seen build tool so the click that selects a tool from the
        // IMGUI palette (which runs after Update in the same frame) never also
        // places a room in the tower beneath it.
        private string _lastSeenToolId = string.Empty;
        private Vector3 _lastHintScreenPos;
        private GUIStyle _hintStyle;

        // Single-entry cache for the stairwell snap search: Update calls it every
        // frame while hovering, and each search runs domain validation per candidate.
        private string _resolveCellCacheTool;
        private int _resolveCellCacheFloor;
        private int _resolveCellCacheHover;
        private TowerTopologyProjection _resolveCellCacheTopology;
        private int _resolveCellCacheResult;
        private bool _hasResolveCellCache;

        private void Update()
        {
            if (_modeSession == null || _simulationSession == null)
            {
                return;
            }

            var projection = _modeSession.Projection();
            if (!projection.IsBuildMode || string.IsNullOrEmpty(projection.SelectedBuildTool))
            {
                _suppressedCellFloor = null;
                _suppressedCellX = null;
                _lastSeenToolId = string.Empty;
                LastPlacementHint = null;
                GhostPresenter.HideGhost();
                return;
            }

            // If the selected tool changed since last frame, this frame's primary
            // click most likely selected the tool in the palette — never treat it
            // as a tower placement click.
            var currentToolId = projection.SelectedBuildTool ?? string.Empty;
            var toolChangedThisFrame = !string.Equals(currentToolId, _lastSeenToolId, StringComparison.Ordinal);
            _lastSeenToolId = currentToolId;

            // Right-click deselects the current build tool
            if (IsSecondaryPointerDown())
            {
                _suppressedCellFloor = null;
                _suppressedCellX = null;
                _modeSession.CancelOrEscape();
                GhostPresenter.HideGhost();
                return;
            }

            if (TryGetScreenPointerPosition(out var screenPos))
            {
                // Disallow grid hover and clicks when pointer is interacting with UI
                if (IsPointerOverUI(screenPos, includeBuildPalette: string.IsNullOrEmpty(projection.SelectedBuildTool)))
                {
                    LastPlacementHint = null;
                    GhostPresenter.HideGhost();
                    return;
                }

                if (TryGetCellFromScreen(screenPos, Camera, out var floor, out var cellX))
                {
                    floor = ResolvePlacementFloor(projection.SelectedBuildTool, floor);
                    cellX = ResolvePlacementCell(projection.SelectedBuildTool, floor, cellX);

                    // Clear suppression once pointer moves away from the just-placed cell
                    if (_suppressedCellFloor.HasValue && (_suppressedCellFloor.Value != floor || _suppressedCellX.Value != cellX))
                    {
                        _suppressedCellFloor = null;
                        _suppressedCellX = null;
                    }

                    // While lingering over the just-placed cell, suppress red error ghost
                    if (_suppressedCellFloor.HasValue && _suppressedCellFloor.Value == floor && _suppressedCellX.Value == cellX)
                    {
                        LastPlacementHint = null;
                        GhostPresenter.HideGhost();
                        return;
                    }

                    _modeSession.SetPlacementTarget(floor, cellX);

                    var toolId = projection.SelectedBuildTool;
                    TryGetToolPlacementBounds(toolId, floor, cellX, out var placementBounds);
                    var isValid = ValidatePlacement(toolId, floor, cellX, out var failureReason);
                    LastPlacementValid = isValid;
                    LastPlacementHint = FormatPlacementHint(toolId, floor, placementBounds, isValid, failureReason);
                    _lastHintScreenPos = screenPos;

                    var worldPos = CellToWorld(floor, placementBounds.MinX, placementBounds.Width);
                    var worldSize = new Vector2(placementBounds.Width * _cellWidth, _floorHeight * 0.9f);
                    Color? customGhostColor = null;

                    if (toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
                    {
                        var sessionFloorCount = _simulationSession?.FloorCount ?? 0;
                        var topFloor = (_simulationSession != null && floor == sessionFloorCount - 1) ? floor : floor + 1;
                        var bottomFloor = (_simulationSession != null && floor == sessionFloorCount - 1) ? floor - 1 : floor;
                        var midY = (_floorOriginY + bottomFloor * _floorHeight + _floorOriginY + topFloor * _floorHeight) * 0.5f;
                        worldPos = new Vector3(worldPos.x, midY, worldPos.z);
                        worldSize = new Vector2(placementBounds.Width * _cellWidth, _floorHeight * 1.85f);
                    }
                    else if (toolId.Equals("demolish:room", StringComparison.OrdinalIgnoreCase) && isValid)
                    {
                        customGhostColor = new Color(0.95f, 0.45f, 0.20f, 0.65f);
                    }

                    GhostPresenter.ShowGhost(worldPos, worldSize, isValid, customGhostColor);

                    // Strictly block placement when placement is invalid (red ghost).
                    // Also skip when the tool just changed: that click selected the
                    // tool in the palette rather than the tower.
                    if (!toolChangedThisFrame && IsPrimaryPointerDown())
                    {
                        if (isValid && TryExecutePlacement(toolId, floor, cellX, out var result) && result.Accepted)
                        {
                            _suppressedCellFloor = floor;
                            _suppressedCellX = cellX;
                            GhostPresenter.HideGhost();
                        }
                    }
                    return;
                }
            }

            LastPlacementHint = null;
            GhostPresenter.HideGhost();
        }

        private void OnGUI()
        {
            if (!UnityEngine.Application.isPlaying) return;
            if (_modeSession == null) return;
            if (string.IsNullOrEmpty(LastPlacementHint)) return;

            var projection = _modeSession.Projection();
            if (!projection.IsBuildMode || string.IsNullOrEmpty(projection.SelectedBuildTool)) return;

            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = LastPlacementValid ? new Color(0.35f, 1f, 0.6f) : new Color(1f, 0.55f, 0.45f) }
                };
            }
            _hintStyle.normal.textColor = LastPlacementValid ? new Color(0.35f, 1f, 0.6f) : new Color(1f, 0.55f, 0.45f);

            var guiY = Screen.height - _lastHintScreenPos.y;
            GUI.Label(new Rect(_lastHintScreenPos.x + 16, guiY + 14, 430, 24), LastPlacementHint, _hintStyle);
        }

        public static bool IsPointerOverUI(Vector3 screenPos, bool includeBuildPalette = true)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            var guiY = Screen.height - screenPos.y;
            var guiPoint = new Vector2(screenPos.x, guiY);

            // The chooser disappears after tool selection; the mode bar and
            // context strip do not. Shield only the visible palette but always
            // shield those controls, including on the click that reopens Build.
            const float margin = 8f;
            if (ContainsWithMargin(ModeShellBarController.ModeBarRect(Screen.height), guiPoint, margin) ||
                ContainsWithMargin(ModeShellBarController.ContextRect(Screen.height, isBuildMode: true), guiPoint, margin) ||
                (includeBuildPalette && ContainsWithMargin(ModeShellBarController.BuildPaletteRect(Screen.height), guiPoint, margin)))
                return true;

            // Top-left HUD area
            var hudWidth = Mathf.Min(420f, Screen.width * 0.35f);
            var hudHeight = Mathf.Min(300f, Screen.height * 0.35f);
            if (guiPoint.x >= 15 && guiPoint.x <= hudWidth && guiPoint.y >= 15 && guiPoint.y <= hudHeight)
            {
                return true;
            }

            // Right side cards (Placement preview or Congestion inspector)
            var cardWidth = Mathf.Min(400f, Screen.width * 0.35f);
            if (guiPoint.x >= Screen.width - cardWidth && guiPoint.x <= Screen.width - 15)
            {
                if (guiPoint.y >= 15 && guiPoint.y <= Mathf.Min(360f, Screen.height * 0.45f)) return true; // Inspector
                if (guiPoint.y >= Screen.height - Mathf.Min(360f, Screen.height * 0.45f) && guiPoint.y <= Screen.height - 15) return true; // Preview
            }

            return false;
        }

        private static bool ContainsWithMargin(Rect rect, Vector2 point, float margin) =>
            point.x >= rect.xMin - margin && point.x <= rect.xMax + margin &&
            point.y >= rect.yMin - margin && point.y <= rect.yMax + margin;

        private static bool TryGetScreenPointerPosition(out Vector3 screenPos)
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                var pos = mouse.position.ReadValue();
                screenPos = new Vector3(pos.x, pos.y, 0f);
                return true;
            }
            screenPos = Vector3.zero;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            screenPos = Input.mousePosition;
            return true;
#else
            screenPos = Vector3.zero;
            return false;
#endif
        }

        private static bool IsPrimaryPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static bool IsSecondaryPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.rightButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        public Vector3 CellToWorld(int floor, int cellX, int widthInCells = 1)
        {
            var worldLeft = _cellOriginX + cellX * _cellWidth;
            var worldRight = _cellOriginX + (cellX + widthInCells) * _cellWidth;
            var worldX = (worldLeft + worldRight) * 0.5f;
            var worldY = _floorOriginY + floor * _floorHeight;
            return new Vector3(worldX, worldY, 0f);
        }

        public bool TryGetCellFromWorld(Vector3 worldPos, out int floor, out int cellX)
        {
            floor = Mathf.RoundToInt((worldPos.y - _floorOriginY) / _floorHeight);
            cellX = Mathf.FloorToInt((worldPos.x - _cellOriginX) / _cellWidth);
            return true;
        }

        public bool TryGetCellFromScreen(Vector3 screenPos, Camera cam, out int floor, out int cellX)
        {
            if (cam == null)
            {
                cam = Camera;
            }

            if (cam == null)
            {
                floor = 0;
                cellX = 0;
                return false;
            }

            var ray = cam.ScreenPointToRay(screenPos);
            var towerPlane = new Plane(Vector3.forward, Vector3.zero);

            if (towerPlane.Raycast(ray, out var enter))
            {
                var worldHit = ray.GetPoint(enter);
                return TryGetCellFromWorld(worldHit, out floor, out cellX);
            }

            floor = 0;
            cellX = 0;
            return false;
        }

        public static int GetToolWidthInCells(string toolId)
        {
            if (string.IsNullOrEmpty(toolId)) return 1;

            if (BuildingCatalog.TryGetRoomDefinition(toolId, out var roomDefinition))
            {
                return roomDefinition.WidthInCells;
            }

            if (toolId.Equals("demolish:room", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (toolId.StartsWith("residential:", StringComparison.OrdinalIgnoreCase) ||
                toolId.Equals("room:apartment", StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }

            if (toolId.Equals("commercial:office", StringComparison.OrdinalIgnoreCase))
            {
                return 8;
            }

            if (toolId.StartsWith("commercial:", StringComparison.OrdinalIgnoreCase) ||
                toolId.Equals("room:diner", StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }

            if (toolId.Equals("transit:elevator_car", StringComparison.OrdinalIgnoreCase) ||
                toolId.Equals("transit:elevator_shaft", StringComparison.OrdinalIgnoreCase) ||
                toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultFloorSlabMaxX - DefaultFloorSlabMinX + 1;
            }

            if (toolId.Equals("floor:ground_expansion", StringComparison.OrdinalIgnoreCase)) return GroundSlabExpansionWidth;

            return 4;
        }

        /// <summary>
        /// Resolves the floor that a build tool should target from a hovered grid floor.
        /// A floor slab is an expansion action rather than an arbitrary overlay: it always
        /// previews the next unbuilt level so a player cannot accidentally target an
        /// existing lower floor while looking at the tower overview.
        /// Vertical transit prefers the hovered floor when it is in range so lower
        /// floors stay repairable; out-of-range hovers fall back to the newest floor
        /// for the common top-extension case.
        /// </summary>
        public int ResolvePlacementFloor(string toolId, int hoveredFloor)
        {
            if (toolId != null && _simulationSession != null)
            {
                if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
                {
                    return _simulationSession.FloorCount;
                }

                if (toolId.Equals("floor:ground_expansion", StringComparison.OrdinalIgnoreCase)) return 0;

                if (toolId.Equals("transit:elevator_shaft", StringComparison.OrdinalIgnoreCase))
                {
                    var topFloor = Math.Max(1, _simulationSession.FloorCount - 1);
                    if (hoveredFloor >= 1 && hoveredFloor <= topFloor) return hoveredFloor;
                    return topFloor;
                }

                if (toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
                {
                    var topFloor = Math.Max(0, _simulationSession.FloorCount - 1);
                    if (hoveredFloor >= 0 && hoveredFloor <= topFloor) return hoveredFloor;
                    return topFloor;
                }
            }

            return hoveredFloor;
        }

        /// <summary>
        /// Finds a buildable two-cell landing for a stair extension when the hovered
        /// cells are occupied on the lower of the two connected floors. This delegates
        /// validity to the domain command seam and preserves player control whenever
        /// the hovered span is already usable.
        /// </summary>
        public int ResolvePlacementCell(string toolId, int floor, int hoveredCellX)
        {
            if (!string.Equals(toolId, "transit:stairwell", StringComparison.OrdinalIgnoreCase) || _simulationSession == null)
            {
                return hoveredCellX;
            }

            if (ValidatePlacement(toolId, floor, hoveredCellX, out _))
            {
                return hoveredCellX;
            }

            var session = _simulationSession;
            if (_hasResolveCellCache &&
                string.Equals(_resolveCellCacheTool, toolId, StringComparison.OrdinalIgnoreCase) &&
                _resolveCellCacheFloor == floor &&
                _resolveCellCacheHover == hoveredCellX &&
                ReferenceEquals(_resolveCellCacheTopology, session.TopologyProjection()))
            {
                return _resolveCellCacheResult;
            }

            var bottomFloor = floor == session.FloorCount - 1 ? floor - 1 : floor;
            if (bottomFloor < 0 || !session.TryGetFloorSlab(bottomFloor, out var lowerSlab))
            {
                return hoveredCellX;
            }

            var bestCell = hoveredCellX;
            var bestDistance = int.MaxValue;
            for (var candidate = lowerSlab.MinX; candidate < lowerSlab.MaxX; candidate++)
            {
                if (ValidatePlacement(toolId, floor, candidate, out _))
                {
                    var distance = Math.Abs(candidate - hoveredCellX);
                    if (distance < bestDistance)
                    {
                        bestCell = candidate;
                        bestDistance = distance;
                    }
                }
            }

            _resolveCellCacheTool = toolId;
            _resolveCellCacheFloor = floor;
            _resolveCellCacheHover = hoveredCellX;
            _resolveCellCacheTopology = session.TopologyProjection();
            _resolveCellCacheResult = bestCell;
            _hasResolveCellCache = true;
            return bestCell;
        }

        public bool TryGetToolPlacementBounds(string toolId, int floor, int cellX, out CellBounds bounds)
        {
            if (string.IsNullOrEmpty(toolId))
            {
                bounds = new CellBounds(floor, cellX, cellX);
                return false;
            }

            if (toolId.Equals("demolish:room", StringComparison.OrdinalIgnoreCase))
            {
                if (_simulationSession != null)
                {
                    var floorRooms = _simulationSession.GetRoomsOnFloor(floor);
                    for (var i = 0; i < floorRooms.Count; i++)
                    {
                        if (cellX >= floorRooms[i].Bounds.MinX && cellX <= floorRooms[i].Bounds.MaxX)
                        {
                            bounds = floorRooms[i].Bounds;
                            return true;
                        }
                    }
                }
                bounds = new CellBounds(floor, cellX, cellX);
                return false;
            }

            if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
            {
                var minX = DefaultFloorSlabMinX;
                var maxX = DefaultFloorSlabMaxX;
                if (_simulationSession != null && floor > 0 &&
                    _simulationSession.TryGetFloorSlab(floor - 1, out var lowerSlab))
                {
                    minX = lowerSlab.MinX;
                    maxX = lowerSlab.MaxX;
                }
                bounds = new CellBounds(floor, minX, maxX);
                return true;
            }

            if (toolId.Equals("floor:ground_expansion", StringComparison.OrdinalIgnoreCase))
            {
                if (_simulationSession == null || !_simulationSession.TryGetFloorSlab(0, out var groundSlab))
                {
                    bounds = new CellBounds(0, DefaultFloorSlabMinX, DefaultFloorSlabMaxX);
                    return false;
                }
                var expandLeft = cellX < groundSlab.MinX || (cellX <= groundSlab.MaxX && cellX - groundSlab.MinX < groundSlab.MaxX - cellX);
                bounds = expandLeft ? new CellBounds(0, groundSlab.MinX - GroundSlabExpansionWidth, groundSlab.MaxX) : new CellBounds(0, groundSlab.MinX, groundSlab.MaxX + GroundSlabExpansionWidth);
                return true;
            }

            if (toolId.Equals("transit:elevator_shaft", StringComparison.OrdinalIgnoreCase) ||
                toolId.Equals("transit:elevator_car", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new CellBounds(floor, 0, 1);
                return true;
            }

            if (toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new CellBounds(floor, cellX, cellX + 1);
                return true;
            }

            var width = GetToolWidthInCells(toolId);
            bounds = new CellBounds(floor, cellX, cellX + width - 1);
            return true;
        }

        public bool TryCreateCommand(string toolId, int floor, int cellX, out ICommand command, out string failureReason)
        {
            command = null;
            failureReason = null;

            if (string.IsNullOrEmpty(toolId))
            {
                failureReason = "No active tool selected.";
                return false;
            }

            if (_simulationSession == null)
            {
                failureReason = "No active simulation session.";
                return false;
            }

            if (toolId.Equals("transit:elevator_car", StringComparison.OrdinalIgnoreCase))
            {
                if (cellX < -3 || cellX > 3)
                {
                    failureReason = "Elevator car must be placed within the central elevator shaft corridor.";
                    return false;
                }

                command = new AddElevatorCarCommand(startingFloor: floor);
                return true;
            }

            if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
            {
                if (floor < 0)
                {
                    failureReason = "Floor level cannot be negative.";
                    return false;
                }

                TryGetToolPlacementBounds(toolId, floor, cellX, out var slabBounds);
                command = new BuildFloorSlabCommand(floor, slabBounds.MinX, slabBounds.MaxX);
                return true;
            }

            if (toolId.Equals("floor:ground_expansion", StringComparison.OrdinalIgnoreCase))
            {
                TryGetToolPlacementBounds(toolId, 0, cellX, out var expandedBounds);
                command = new ExpandGroundSlabCommand(expandedBounds.MinX, expandedBounds.MaxX);
                return true;
            }

            if (toolId.Equals("transit:elevator_shaft", StringComparison.OrdinalIgnoreCase))
            {
                if (floor <= 0)
                {
                    failureReason = "Elevator shaft must extend to floor 1 or higher.";
                    return false;
                }

                if (cellX < -1 || cellX > 1)
                {
                    failureReason = "Elevator shaft must align with the central elevator column [0..1].";
                    return false;
                }

                command = new AddElevatorShaftCommand(0, 1, 0, floor);
                return true;
            }

            if (toolId.Equals("demolish:room", StringComparison.OrdinalIgnoreCase))
            {
                var floorRooms = _simulationSession.GetRoomsOnFloor(floor);
                Room targetRoom = null;
                for (var i = 0; i < floorRooms.Count; i++)
                {
                    if (cellX >= floorRooms[i].Bounds.MinX && cellX <= floorRooms[i].Bounds.MaxX)
                    {
                        targetRoom = floorRooms[i];
                        break;
                    }
                }

                if (targetRoom == null)
                {
                    failureReason = "No room at hovered grid cell to demolish.";
                    return false;
                }

                command = new DemolishRoomCommand(targetRoom.Id);
                return true;
            }

            if (toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
            {
                if (_simulationSession.FloorCount < 2)
                {
                    failureReason = "Stairwell requires at least 2 tower floors.";
                    return false;
                }

                var bottomFloor = (floor == _simulationSession.FloorCount - 1) ? floor - 1 : floor;
                var topFloor = bottomFloor + 1;

                command = new BuildStairwellCommand(cellX, cellX + 1, bottomFloor, topFloor);
                return true;
            }

            // Room placement. Catalogued zones keep their authored footprint and capacity;
            // unknown future room tools retain the existing generic placement behavior.
            if (BuildingCatalog.TryGetRoomDefinition(toolId, out var roomDefinition))
            {
                command = new BuildRoomCommand(
                    floor,
                    cellX,
                    cellX + roomDefinition.WidthInCells - 1,
                    roomDefinition.ContentType,
                    capacity: roomDefinition.Capacity);
                return true;
            }

            var roomWidth = GetToolWidthInCells(toolId);
            var contentType = toolId.StartsWith("room:", StringComparison.OrdinalIgnoreCase)
                ? new ContentId("residential:studio")
                : new ContentId(toolId);

            var capacity = toolId.Equals("commercial:office", StringComparison.OrdinalIgnoreCase) ? 8 : 5;
            command = new BuildRoomCommand(floor, cellX, cellX + roomWidth - 1, contentType, capacity: capacity);
            return true;
        }

        public bool ValidatePlacement(string toolId, int floor, int cellX, out string failureReason)
        {
            if (!TryCreateCommand(toolId, floor, cellX, out var command, out failureReason))
            {
                return false;
            }

            var result = _simulationSession.CanExecute(command);
            if (!result.Accepted)
            {
                failureReason = result.Rejections.Count > 0 ? result.Rejections[0].Message : "Placement invalid.";
                return false;
            }

            failureReason = null;
            return true;
        }

        public bool TryExecutePlacement(string toolId, int floor, int cellX, out CommandResult result)
        {
            if (!TryCreateCommand(toolId, floor, cellX, out var command, out var failureReason))
            {
                result = CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("placement:invalid"), failureReason) });
                PlacementExecuted?.Invoke(result);
                return false;
            }

            result = _simulationSession.ExecuteCommand(command);
            PlacementExecuted?.Invoke(result);
            return result.Accepted;
        }
    }
}
