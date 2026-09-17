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
        public const int DefaultFloorSlabMaxX = 16;

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

            // Bottom UI area (Mode bar, context overlay, 2-row build palette)
            if (guiPoint.x >= 15 && guiPoint.x <= 650 && guiPoint.y >= Screen.height - 215 && guiPoint.y <= Screen.height - 15)
            {
                return true;
            }

            // Top-left HUD area
            if (guiPoint.x >= 15 && guiPoint.x <= 420 && guiPoint.y >= 15 && guiPoint.y <= 300)
            {
                return true;
            }

            // Right side cards (Placement preview or Congestion inspector)
            if (guiPoint.x >= Screen.width - 400 && guiPoint.x <= Screen.width - 15)
            {
                if (guiPoint.y >= 15 && guiPoint.y <= 360) return true; // Inspector
                if (guiPoint.y >= Screen.height - 360 && guiPoint.y <= Screen.height - 15) return true; // Preview
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

        public bool ValidatePlacement(string toolId, int floor, int cellX, out string failureReason)
        {
            failureReason = null;
            if (_simulationSession == null)
            {
                failureReason = "No active simulation session.";
                return false;
            }

            var topology = _simulationSession.Topology;
            var economy = _simulationSession.Economy;

            if (toolId.Equals("transit:elevator_car", StringComparison.OrdinalIgnoreCase))
            {
                if (floor < 0 || floor >= topology.FloorCount)
                {
                    failureReason = $"Elevator car must be placed within active tower floors (0..{topology.FloorCount - 1}).";
                    return false;
                }

                if (cellX < -3 || cellX > 3)
                {
                    failureReason = "Elevator car must be placed within the central elevator shaft corridor.";
                    return false;
                }

                var congestion = _simulationSession.CongestionProjection();
                if (congestion != null && congestion.Elevators.Count >= ElevatorPlacementPredictor.MaxCarsPerBank)
                {
                    failureReason = $"Elevator bank has reached maximum capacity ({ElevatorPlacementPredictor.MaxCarsPerBank} cars).";
                    return false;
                }

                if (!economy.CanAfford(TowerEconomyState.ElevatorCarCost))
                {
                    failureReason = $"Insufficient funds for elevator car ({TowerEconomyState.ElevatorCarCost} required).";
                    return false;
                }
                return true;
            }

            if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
            {
                if (floor < 0)
                {
                    failureReason = "Floor level cannot be negative.";
                    return false;
                }

                if (topology.FloorSlabs.ContainsKey(floor))
                {
                    failureReason = $"Floor {floor} slab already exists.";
                    return false;
                }

                if (floor > 0 && !topology.FloorSlabs.ContainsKey(floor - 1))
                {
                    failureReason = $"Cannot construct floor {floor} slab without floor {floor - 1} slab below.";
                    return false;
                }

                TryGetToolPlacementBounds(toolId, floor, cellX, out var slabBounds);
                var cost = economy.CalculateFloorSlabCost(slabBounds);
                if (!economy.CanAfford(cost))
                {
                    failureReason = $"Insufficient funds for floor slab ({cost} required).";
                    return false;
                }
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

                if (!topology.FloorSlabs.ContainsKey(floor))
                {
                    failureReason = $"Floor {floor} has no slab. Build a floor slab first.";
                    return false;
                }

                var existingRooms = topology.GetRoomsOnFloor(floor);
                for (var i = 0; i < existingRooms.Count; i++)
                {
                    if (existingRooms[i].ContentType == new ContentId("transit:elevator_shaft"))
                    {
                        failureReason = $"Elevator shaft already exists on floor {floor}.";
                        return false;
                    }

                    if (existingRooms[i].Bounds.Overlaps(new CellBounds(floor, 0, 1)))
                    {
                        failureReason = $"Elevator shaft overlaps existing room '{existingRooms[i].ContentType.Value}'.";
                        return false;
                    }
                }

                var cost = economy.CalculateElevatorShaftCost(Math.Max(1, floor + 1), 2);
                if (!economy.CanAfford(cost))
                {
                    failureReason = $"Insufficient funds for elevator shaft ({cost} required).";
                    return false;
                }
                return true;
            }

            if (toolId.Equals("demolish:room", StringComparison.OrdinalIgnoreCase))
            {
                var floorRooms = topology.GetRoomsOnFloor(floor);
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

                var contentVal = targetRoom.ContentType.Value ?? "";
                if (contentVal.Contains("lobby"))
                {
                    failureReason = "Cannot demolish main reception lobby.";
                    return false;
                }

                if (contentVal.Contains("elevator_shaft"))
                {
                    failureReason = "Elevator shafts cannot be demolished with room bulldozer.";
                    return false;
                }

                if (contentVal.StartsWith("residential:"))
                {
                    var households = _simulationSession.Simulation.Population.Households;
                    for (var i = 0; i < households.Count; i++)
                    {
                        if (households[i].HomeRoomId.Equals(targetRoom.Id))
                        {
                            failureReason = "Cannot demolish occupied apartment with active tenants.";
                            return false;
                        }
                    }
                }

                return true;
            }

            if (toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
            {
                if (topology.FloorCount < 2)
                {
                    failureReason = "Stairwell requires at least 2 tower floors.";
                    return false;
                }

                var bottomFloor = (floor == topology.FloorCount - 1) ? floor - 1 : floor;
                var topFloor = bottomFloor + 1;

                if (!topology.FloorSlabs.TryGetValue(bottomFloor, out var bottomSlab))
                {
                    failureReason = $"Floor {bottomFloor} has no slab.";
                    return false;
                }

                if (!topology.FloorSlabs.TryGetValue(topFloor, out var topSlab))
                {
                    failureReason = $"Floor {topFloor} has no slab.";
                    return false;
                }

                if (cellX < bottomSlab.MinX || cellX + 1 > bottomSlab.MaxX ||
                    cellX < topSlab.MinX || cellX + 1 > topSlab.MaxX)
                {
                    failureReason = "Stairwell must be fully contained within slabs of both floors.";
                    return false;
                }

                if (cellX <= 1 && cellX + 1 >= 0)
                {
                    failureReason = "Stairwell cannot overlap central elevator shaft column [0..1].";
                    return false;
                }

                var stairContentType = new ContentId("amenity:stairwell");
                for (var f = bottomFloor; f <= topFloor; f++)
                {
                    var existingRooms = topology.GetRoomsOnFloor(f);
                    for (var i = 0; i < existingRooms.Count; i++)
                    {
                        var existing = existingRooms[i];
                        if (existing.Bounds.Overlaps(new CellBounds(f, cellX, cellX + 1)))
                        {
                            if (existing.ContentType == stairContentType && existing.Bounds.MinX == cellX && existing.Bounds.MaxX == cellX + 1)
                            {
                                continue;
                            }

                            failureReason = $"Stairwell overlaps existing room '{existing.ContentType.Value}' on floor {f}.";
                            return false;
                        }
                    }
                }

                var cost = economy.CalculateStairwellCost(1, 2);
                if (!economy.CanAfford(cost))
                {
                    failureReason = $"Insufficient funds for stairwell ({cost} required).";
                    return false;
                }

                return true;
            }

            // Room placement
            var roomWidth = GetToolWidthInCells(toolId);
            var roomBounds = new CellBounds(floor, cellX, cellX + roomWidth - 1);

            if (!topology.FloorSlabs.TryGetValue(floor, out var slab))
            {
                failureReason = $"Floor {floor} has no slab. Build a floor slab first.";
                return false;
            }

            if (roomBounds.MinX < slab.MinX || roomBounds.MaxX > slab.MaxX)
            {
                failureReason = $"Room exceeds floor {floor} slab boundaries ({slab.MinX}..{slab.MaxX}).";
                return false;
            }

            if (floor > 0)
            {
                if (!topology.FloorSlabs.TryGetValue(floor - 1, out var lowerSlab) ||
                    roomBounds.MinX < lowerSlab.MinX || roomBounds.MaxX > lowerSlab.MaxX)
                {
                    failureReason = $"Room must be supported by continuous floor slab below on floor {floor - 1}.";
                    return false;
                }
            }

            var floorRoomsList = topology.GetRoomsOnFloor(floor);
            for (var i = 0; i < floorRoomsList.Count; i++)
            {
                if (floorRoomsList[i].Bounds.Overlaps(roomBounds))
                {
                    failureReason = $"Overlaps existing room '{floorRoomsList[i].ContentType.Value}' at [{floorRoomsList[i].Bounds.MinX}..{floorRoomsList[i].Bounds.MaxX}].";
                    return false;
                }
            }

            var contentType = toolId.StartsWith("room:", StringComparison.OrdinalIgnoreCase)
                ? new ContentId("residential:studio")
                : new ContentId(toolId);

            var roomCost = economy.CalculateRoomCost(contentType, roomBounds);
            if (!economy.CanAfford(roomCost))
            {
                failureReason = $"Insufficient funds for room ({roomCost} required).";
                return false;
            }

            return true;
        }

        public bool TryExecutePlacement(string toolId, int floor, int cellX, out CommandResult result)
        {
            if (!ValidatePlacement(toolId, floor, cellX, out var failureReason))
            {
                result = CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("placement:invalid"), failureReason) });
                PlacementExecuted?.Invoke(result);
                return false;
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
                    result = CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("demolish:no_room"), "No room at cursor to demolish.") });
                    PlacementExecuted?.Invoke(result);
                    return false;
                }

                result = _simulationSession.DemolishRoom(new DemolishRoomCommand(targetRoom.Id));
                PlacementExecuted?.Invoke(result);
                return result.Accepted;
            }

            if (toolId.Equals("transit:elevator_car", StringComparison.OrdinalIgnoreCase))
            {
                _simulationSession.AddCapacity();
                result = CommandResult.Accept();
                PlacementExecuted?.Invoke(result);
                return true;
            }

            if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
            {
                TryGetToolPlacementBounds(toolId, floor, cellX, out var slabBounds);
                var cmd = new BuildFloorSlabCommand(floor, slabBounds.MinX, slabBounds.MaxX);
                result = _simulationSession.BuildFloorSlab(cmd);
                PlacementExecuted?.Invoke(result);
                return result.Accepted;
            }

            if (toolId.Equals("transit:elevator_shaft", StringComparison.OrdinalIgnoreCase))
            {
                var cmd = new AddElevatorShaftCommand(0, 1, 0, floor);
                result = _simulationSession.AddElevatorShaft(cmd);
                PlacementExecuted?.Invoke(result);
                return result.Accepted;
            }

            if (toolId.Equals("transit:stairwell", StringComparison.OrdinalIgnoreCase))
            {
                var topology = _simulationSession.Topology;
                var bottomFloor = (floor == topology.FloorCount - 1) ? floor - 1 : floor;
                var topFloor = bottomFloor + 1;

                var cmd = new BuildStairwellCommand(cellX, cellX + 1, bottomFloor, topFloor);
                result = _simulationSession.BuildStairwell(cmd);
                PlacementExecuted?.Invoke(result);
                return result.Accepted;
            }

            // Room placement
            var roomWidth = GetToolWidthInCells(toolId);
            var contentType = toolId.StartsWith("room:", StringComparison.OrdinalIgnoreCase)
                ? new ContentId("residential:studio")
                : new ContentId(toolId);

            var capacity = toolId.Equals("commercial:office", StringComparison.OrdinalIgnoreCase) ? 8 : 5;
            var buildCmd = new BuildRoomCommand(floor, cellX, cellX + roomWidth - 1, contentType, capacity: capacity);
            result = _simulationSession.BuildRoom(buildCmd);
            PlacementExecuted?.Invoke(result);
            return result.Accepted;
        }
    }
}
