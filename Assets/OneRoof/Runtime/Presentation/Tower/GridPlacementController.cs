using System;
using OneRoof.Application.Modes;
using OneRoof.Application.Prediction;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
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

        public event Action<CommandResult> PlacementExecuted;

        private int? _suppressedCellFloor;
        private int? _suppressedCellX;

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
                GhostPresenter.HideGhost();
                return;
            }

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
                if (IsPointerOverUI(screenPos))
                {
                    GhostPresenter.HideGhost();
                    return;
                }

                if (TryGetCellFromScreen(screenPos, Camera, out var floor, out var cellX))
                {
                    floor = ResolvePlacementFloor(projection.SelectedBuildTool, floor);

                    // Clear suppression once pointer moves away from the just-placed cell
                    if (_suppressedCellFloor.HasValue && (_suppressedCellFloor.Value != floor || _suppressedCellX.Value != cellX))
                    {
                        _suppressedCellFloor = null;
                        _suppressedCellX = null;
                    }

                    // While lingering over the just-placed cell, suppress red error ghost
                    if (_suppressedCellFloor.HasValue && _suppressedCellFloor.Value == floor && _suppressedCellX.Value == cellX)
                    {
                        GhostPresenter.HideGhost();
                        return;
                    }

                    _modeSession.SetPlacementTarget(floor, cellX);

                    var toolId = projection.SelectedBuildTool;
                    TryGetToolPlacementBounds(toolId, floor, cellX, out var placementBounds);
                    var isValid = ValidatePlacement(toolId, floor, cellX, out _);

                    var worldPos = CellToWorld(floor, placementBounds.MinX, placementBounds.Width);
                    var worldSize = new Vector2(placementBounds.Width * _cellWidth, _floorHeight * 0.9f);
                    Color? customGhostColor = null;

                    if (toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
                    {
                        var topology = _simulationSession?.Topology;
                        var topFloor = (topology != null && floor == topology.FloorCount - 1) ? floor : floor + 1;
                        var bottomFloor = (topology != null && floor == topology.FloorCount - 1) ? floor - 1 : floor;
                        var midY = (_floorOriginY + bottomFloor * _floorHeight + _floorOriginY + topFloor * _floorHeight) * 0.5f;
                        worldPos = new Vector3(worldPos.x, midY, worldPos.z);
                        worldSize = new Vector2(placementBounds.Width * _cellWidth, _floorHeight * 1.85f);
                    }
                    else if (toolId.Equals("demolish:room", StringComparison.OrdinalIgnoreCase) && isValid)
                    {
                        customGhostColor = new Color(0.95f, 0.45f, 0.20f, 0.65f);
                    }

                    GhostPresenter.ShowGhost(worldPos, worldSize, isValid, customGhostColor);

                    // Strictly block placement when placement is invalid (red ghost)
                    if (IsPrimaryPointerDown())
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

            GhostPresenter.HideGhost();
        }

        public static bool IsPointerOverUI(Vector3 screenPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            var guiY = Screen.height - screenPos.y;
            var guiPoint = new Vector2(screenPos.x, guiY);

            // Bottom UI area (Mode bar, context overlay, 3-row build palette).
            // The third row begins at Screen.height - 206, which remains inside this
            // deliberately padded hit region without consuming the low-resolution center.
            if (guiPoint.x >= 15 && guiPoint.x <= 650 && guiPoint.y >= Screen.height - 215 && guiPoint.y <= Screen.height - 15)
            {
                return true;
            }

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

            return 4;
        }

        /// <summary>
        /// Resolves the floor that a build tool should target from a hovered grid floor.
        /// A floor slab is an expansion action rather than an arbitrary overlay: it always
        /// previews the next unbuilt level so a player cannot accidentally target an
        /// existing lower floor while looking at the tower overview.
        /// </summary>
        public int ResolvePlacementFloor(string toolId, int hoveredFloor)
        {
            if (toolId != null && toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase) && _simulationSession != null)
            {
                return _simulationSession.Topology.FloorCount;
            }

            return hoveredFloor;
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
                    var floorRooms = _simulationSession.Topology.GetRoomsOnFloor(floor);
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
                    _simulationSession.Topology.FloorSlabs.TryGetValue(floor - 1, out var lowerSlab))
                {
                    minX = lowerSlab.MinX;
                    maxX = lowerSlab.MaxX;
                }
                bounds = new CellBounds(floor, minX, maxX);
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
                var floorRooms = _simulationSession.Topology.GetRoomsOnFloor(floor);
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
                var topology = _simulationSession.Topology;
                if (topology.FloorCount < 2)
                {
                    failureReason = "Stairwell requires at least 2 tower floors.";
                    return false;
                }

                var bottomFloor = (floor == topology.FloorCount - 1) ? floor - 1 : floor;
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
