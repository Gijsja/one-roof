using System;
using OneRoof.Application.Modes;
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
            get => _camera != null ? _camera : (_camera = Camera.main);
            set => _camera = value;
        }

        public float FloorOriginY => _floorOriginY;
        public float FloorHeight => _floorHeight;
        public float CellOriginX => _cellOriginX;
        public float CellWidth => _cellWidth;

        public event Action<CommandResult> PlacementExecuted;

        private void Update()
        {
            if (_modeSession == null || _simulationSession == null)
            {
                return;
            }

            var projection = _modeSession.Projection();
            if (!projection.IsBuildMode || string.IsNullOrEmpty(projection.SelectedBuildTool))
            {
                GhostPresenter.HideGhost();
                return;
            }

            if (TryGetScreenPointerPosition(out var screenPos) && TryGetCellFromScreen(screenPos, Camera, out var floor, out var cellX))
            {
                _modeSession.SetPlacementTarget(floor, cellX);

                var toolId = projection.SelectedBuildTool;
                var width = GetToolWidthInCells(toolId);
                var isValid = ValidatePlacement(toolId, floor, cellX, out _);

                var worldPos = CellToWorld(floor, cellX, width);
                var worldSize = new Vector2(width * _cellWidth, _floorHeight * 0.9f);
                GhostPresenter.ShowGhost(worldPos, worldSize, isValid);

                if (IsPrimaryPointerDown())
                {
                    TryExecutePlacement(toolId, floor, cellX, out _);
                }
            }
            else
            {
                GhostPresenter.HideGhost();
            }
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
                cam = Camera.main;
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

            if (toolId.StartsWith("residential:", StringComparison.OrdinalIgnoreCase) ||
                toolId.Equals("room:apartment", StringComparison.OrdinalIgnoreCase))
            {
                return 6;
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
                return 24;
            }

            return 4;
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
                if (!economy.CanAfford(TowerEconomyState.ElevatorCarCost))
                {
                    failureReason = $"Insufficient funds for elevator car ({TowerEconomyState.ElevatorCarCost} required).";
                    return false;
                }
                return true;
            }

            if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
            {
                var width = GetToolWidthInCells(toolId);
                var bounds = new CellBounds(floor, cellX, cellX + width - 1);
                var cost = economy.CalculateFloorSlabCost(bounds);
                if (!economy.CanAfford(cost))
                {
                    failureReason = $"Insufficient funds for floor slab ({cost} required).";
                    return false;
                }
                return true;
            }

            if (toolId.Equals("transit:elevator_shaft", StringComparison.OrdinalIgnoreCase))
            {
                var cost = economy.CalculateElevatorShaftCost(Math.Max(1, floor + 1), 2);
                if (!economy.CanAfford(cost))
                {
                    failureReason = $"Insufficient funds for elevator shaft ({cost} required).";
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

            var existingRooms = topology.GetRoomsOnFloor(floor);
            for (var i = 0; i < existingRooms.Count; i++)
            {
                if (existingRooms[i].Bounds.Overlaps(roomBounds))
                {
                    failureReason = $"Overlaps existing room '{existingRooms[i].ContentType.Value}' at [{existingRooms[i].Bounds.MinX}..{existingRooms[i].Bounds.MaxX}].";
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

            if (toolId.Equals("transit:elevator_car", StringComparison.OrdinalIgnoreCase))
            {
                _simulationSession.AddCapacity();
                result = CommandResult.Accept();
                PlacementExecuted?.Invoke(result);
                return true;
            }

            if (toolId.Equals("floor:slab", StringComparison.OrdinalIgnoreCase))
            {
                var width = GetToolWidthInCells(toolId);
                var cmd = new BuildFloorSlabCommand(floor, cellX, cellX + width - 1);
                result = _simulationSession.BuildFloorSlab(cmd);
                PlacementExecuted?.Invoke(result);
                return result.Accepted;
            }

            if (toolId.Equals("transit:elevator_shaft", StringComparison.OrdinalIgnoreCase))
            {
                var cmd = new AddElevatorShaftCommand(cellX, cellX + 1, 0, floor);
                result = _simulationSession.AddElevatorShaft(cmd);
                PlacementExecuted?.Invoke(result);
                return result.Accepted;
            }

            // Room placement
            var roomWidth = GetToolWidthInCells(toolId);
            var contentType = toolId.StartsWith("room:", StringComparison.OrdinalIgnoreCase)
                ? new ContentId("residential:studio")
                : new ContentId(toolId);

            var buildCmd = new BuildRoomCommand(floor, cellX, cellX + roomWidth - 1, contentType, capacity: 5);
            result = _simulationSession.BuildRoom(buildCmd);
            PlacementExecuted?.Invoke(result);
            return result.Accepted;
        }
    }
}
