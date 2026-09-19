using System;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OneRoof.Presentation.Tower
{
    public enum InspectTargetKind
    {
        None,
        Resident,
        ElevatorShaft,
        Room
    }

    /// <summary>
    /// Presentation controller managing interactive hover and selection in Inspect mode.
    /// Raycasts pointer positions against residents, the elevator shaft, and rooms,
    /// triggering pixel-perfect outline silhouettes and updating ModeShellSession selection.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InspectSelectionController : MonoBehaviour
    {
        private ModeShellSession _modeSession;
        private TowerSimulationSession _simulationSession;
        private RoomPresenter _roomPresenter;
        private ElevatorBankPresenter _elevatorPresenter;
        private TowerResidentPresenter _residentPresenter;
        private InspectOutlinePresenter _outlinePresenter;
        private Camera _camera;

        public ModeShellSession ModeSession
        {
            get => _modeSession;
            set
            {
                if (_modeSession != null) _modeSession.ModeChanged -= OnModeChanged;
                _modeSession = value;
                if (_modeSession != null) _modeSession.ModeChanged += OnModeChanged;
            }
        }

        public TowerSimulationSession SimulationSession
        {
            get => _simulationSession;
            set => _simulationSession = value;
        }

        public RoomPresenter RoomPresenter
        {
            get => _roomPresenter;
            set => _roomPresenter = value;
        }

        public ElevatorBankPresenter ElevatorPresenter
        {
            get => _elevatorPresenter;
            set => _elevatorPresenter = value;
        }

        public TowerResidentPresenter ResidentPresenter
        {
            get => _residentPresenter;
            set => _residentPresenter = value;
        }

        public InspectOutlinePresenter OutlinePresenter
        {
            get => _outlinePresenter ?? (_outlinePresenter = GetComponent<InspectOutlinePresenter>() ?? gameObject.AddComponent<InspectOutlinePresenter>());
            set => _outlinePresenter = value;
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

        public InspectTargetKind HoveredKind { get; private set; } = InspectTargetKind.None;
        public int? HoveredId { get; private set; }

        public InspectTargetKind SelectedKind { get; private set; } = InspectTargetKind.None;
        public int? SelectedId { get; private set; }

        private void OnDisable()
        {
            if (_modeSession != null) _modeSession.ModeChanged -= OnModeChanged;
            if (OutlinePresenter != null) OutlinePresenter.ClearAll();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        private void OnModeChanged(ModeShellProjection projection)
        {
            if (!projection.IsInspectMode)
            {
                HoveredKind = InspectTargetKind.None;
                HoveredId = null;
                SelectedKind = InspectTargetKind.None;
                SelectedId = null;
                if (OutlinePresenter != null) OutlinePresenter.ClearAll();
            }
            else if (!projection.SelectedEntityId.HasValue)
            {
                SelectedKind = InspectTargetKind.None;
                SelectedId = null;
                if (OutlinePresenter != null) OutlinePresenter.ClearSelection();
            }
        }

        private void Update()
        {
            if (_modeSession == null) return;

            var projection = _modeSession.Projection();
            if (!projection.IsInspectMode)
            {
                if (HoveredKind != InspectTargetKind.None || SelectedKind != InspectTargetKind.None)
                {
                    HoveredKind = InspectTargetKind.None;
                    HoveredId = null;
                    SelectedKind = InspectTargetKind.None;
                    SelectedId = null;
                    OutlinePresenter.ClearAll();
                }
                return;
            }

            // Right-click or Escape cancels selection
            if (IsSecondaryPointerDown() || IsCancelPressed())
            {
                _modeSession.ClearSelection();
                SelectedKind = InspectTargetKind.None;
                SelectedId = null;
                OutlinePresenter.ClearSelection();
                return;
            }

            if (TryGetScreenPointerPosition(out var screenPos))
            {
                if (IsPointerOverUI(screenPos))
                {
                    HoveredKind = InspectTargetKind.None;
                    HoveredId = null;
                    OutlinePresenter.ClearHover();
                    return;
                }

                var cam = Camera;
                if (cam == null) return;

                var worldPos = (Vector2)cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
                var targetHit = HitTest(worldPos, out var kind, out var id, out var floor, out var bounds, out var sprite, out var tr);

                // Process Hover
                if (targetHit)
                {
                    HoveredKind = kind;
                    HoveredId = id;

                    if (kind == InspectTargetKind.Resident && sprite != null)
                    {
                        OutlinePresenter.HighlightHoverSprite(sprite, tr, bounds);
                    }
                    else
                    {
                        OutlinePresenter.HighlightHoverBox(bounds);
                    }
                }
                else
                {
                    HoveredKind = InspectTargetKind.None;
                    HoveredId = null;
                    OutlinePresenter.ClearHover();
                }

                // Process Click
                if (IsPrimaryPointerDown())
                {
                    if (targetHit)
                    {
                        SelectedKind = kind;
                        SelectedId = id;
                        _modeSession.SelectEntity(id, floor);

                        if (kind == InspectTargetKind.Resident && sprite != null)
                        {
                            OutlinePresenter.HighlightSelectSprite(sprite, tr, bounds);
                        }
                        else
                        {
                            OutlinePresenter.HighlightSelectBox(bounds);
                        }
                    }
                    else
                    {
                        // Clicking empty space deselects
                        _modeSession.ClearSelection();
                        SelectedKind = InspectTargetKind.None;
                        SelectedId = null;
                        OutlinePresenter.ClearSelection();
                    }
                }
            }
        }

        public bool HitTest(Vector2 worldPos, out InspectTargetKind kind, out int id, out int? floor, out Bounds bounds, out Sprite sprite, out Transform tr)
        {
            kind = InspectTargetKind.None;
            id = 0;
            floor = null;
            bounds = default;
            sprite = null;
            tr = null;

            // 1. Prioritize Residents
            if (_residentPresenter != null && _residentPresenter.TryGetResidentAt(worldPos, 0.45f, out var resIdx, out var resBounds, out var resSprite, out var resTr))
            {
                kind = InspectTargetKind.Resident;
                id = resIdx + 1; // 1-based resident ID
                bounds = resBounds;
                sprite = resSprite;
                tr = resTr;
                return true;
            }

            // 2. Elevator Shaft
            if (_elevatorPresenter != null && _elevatorPresenter.IsPointerInShaft(worldPos, out var shaftBounds))
            {
                kind = InspectTargetKind.ElevatorShaft;
                id = 0;
                floor = 0;
                bounds = shaftBounds;
                return true;
            }

            // 3. Rooms
            var topo = _simulationSession?.Topology;
            if (_roomPresenter != null && topo != null && _roomPresenter.TryGetRoomAt(worldPos, topo, out var roomId, out var roomBounds))
            {
                kind = InspectTargetKind.Room;
                id = roomId.Value;
                if (topo.TryGetRoom(roomId, out var roomRecord))
                {
                    floor = roomRecord.Floor;
                }
                bounds = roomBounds;
                return true;
            }

            return false;
        }

        private static bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static bool TryGetScreenPointerPosition(out Vector2 screenPos)
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            screenPos = Input.mousePosition;
            return true;
#endif
            screenPos = default;
            return false;
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

        private static bool IsCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
