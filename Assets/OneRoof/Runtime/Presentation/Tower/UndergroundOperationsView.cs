using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Underground;
using OneRoof.Presentation.Population;
using OneRoof.UI;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Read-only undercity inspector, systems policies, and visible noncombat visitor.</summary>
    [DisallowMultipleComponent]
    public sealed class UndergroundOperationsView : MonoBehaviour
    {
        private TowerSimulationSession _simulation;
        private ModeShellSession _modes;
        private GridPlacementController _placement;
        private NpcSkeletalHierarchy _investigator;
        private int _selectedRoomId;
        private string _message = string.Empty;
        private Vector2 _scroll;
        private UndergroundOperationsProjection _cachedProjection;
        private long _cachedVersion = -1;

        public void Bind(TowerSimulationSession simulation, ModeShellSession modes, GridPlacementController placement)
        {
            _simulation = simulation;
            _modes = modes;
            _placement = placement;
            _cachedProjection = null;
            _cachedVersion = -1;
        }

        private UndergroundOperationsProjection Projection()
        {
            if (_cachedProjection != null && _cachedVersion == _simulation.Version) return _cachedProjection;
            _cachedProjection = _simulation.UndergroundOperationsProjection();
            _cachedVersion = _simulation.Version;
            return _cachedProjection;
        }

        private void Update()
        {
            if (_simulation == null) return;
            var operation = Projection();
            UpdateInvestigator(operation);
            if (_modes == null || _modes.CurrentMode != InteractionMode.Inspect || _placement == null) return;
            if (!PrimaryPointerDown()) return;
            Vector3 pointer;
#if ENABLE_INPUT_SYSTEM
            pointer = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            pointer = Input.mousePosition;
#else
            return;
#endif
            if (!_placement.TryGetUndergroundCellFromScreen(pointer, Camera.main, out var x, out var depth)) return;
            for (var i = 0; i < operation.Rooms.Count; i++)
            {
                var room = operation.Rooms[i];
                if (x >= room.X && x < room.X + room.Width && depth >= room.Depth && depth < room.Depth + room.Height)
                {
                    _selectedRoomId = room.Id;
                    return;
                }
            }
        }

        private static bool PrimaryPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private void UpdateInvestigator(UndergroundOperationsProjection state)
        {
            if (state.Phase == InvestigatorPhase.None)
            {
                if (_investigator != null) _investigator.gameObject.SetActive(false);
                return;
            }
            if (_investigator == null)
            {
                var go = new GameObject("Underground Investigator");
                go.transform.SetParent(transform, false);
                _investigator = go.AddComponent<NpcSkeletalHierarchy>();
                _investigator.Initialize(999);
                _investigator.SetCaption("INVESTIGATOR");
                if (_investigator.StatusPlateRenderer != null)
                    _investigator.StatusPlateRenderer.color = new Color(.96f, .45f, .25f);
            }
            _investigator.gameObject.SetActive(true);
            var topology = _simulation.TopologyProjection();
            var groundCenter = -1.4f;
            if (topology.TryGetFloorSlab(0, out var slab))
                groundCenter = GridPlacementController.DefaultCellOriginX +
                    (slab.MinX + slab.MaxX + 1) * GridPlacementController.DefaultCellWidth * .5f;
            var left = groundCenter - UndergroundDigState.GridWidthCells * .5f;
            var baseline = TowerStructurePresenter.FloorY(0) - .74f;
            var outsideX = GridPlacementController.DefaultCellOriginX + 20 * GridPlacementController.DefaultCellWidth;
            var targetX = state.Phase == InvestigatorPhase.Street ||
                          (state.Phase == InvestigatorPhase.Return && state.InvestigatorDepth < 0)
                ? outsideX : state.Phase == InvestigatorPhase.Lobby ? outsideX - 1.1f
                : left + state.InvestigatorX + .5f;
            var targetY = state.Phase == InvestigatorPhase.Street || state.Phase == InvestigatorPhase.Lobby ||
                          (state.Phase == InvestigatorPhase.Return && state.InvestigatorDepth < 0)
                ? TowerStructurePresenter.FloorY(0) - .35f
                : baseline - (Mathf.Max(0, state.InvestigatorDepth) + .5f);
            var target = new Vector3(targetX, targetY, -.22f);
            if (_investigator.transform.position == Vector3.zero) _investigator.transform.position = target;
            else _investigator.transform.position = Vector3.MoveTowards(_investigator.transform.position,
                target, 2.4f * Time.deltaTime);
            _investigator.SetFacing(targetX < _investigator.transform.position.x ? -1f : 1f);
            _investigator.SetAnimationClip(state.Phase == InvestigatorPhase.Inspect ?
                OneRoof.Content.NpcAnimationClip.QueueWait : OneRoof.Content.NpcAnimationClip.Walk);
            _investigator.ApplyProceduralAnimation(Time.time);
        }

        private void OnGUI()
        {
            if (_simulation == null || _modes == null ||
                (_modes.CurrentMode != InteractionMode.Manage && _modes.CurrentMode != InteractionMode.Inspect)) return;
            var state = Projection();
            if (state.Rooms.Count == 0 && _modes.CurrentMode == InteractionMode.Inspect) return;
            var width = Mathf.Min(380f, Screen.width * .34f);
            var area = new Rect(Screen.width - width - 16f, 112f, width, Mathf.Min(430f, Screen.height - 220f));
            GUILayout.BeginArea(area, StewardTheme.Panel);
            GUILayout.Label("UNDERCITY / OPERATIONS", StewardTheme.Label(15, StewardTheme.Text, true));
            GUILayout.Label($"Supplies {state.Supplies}  •  Intel {state.Intel}  •  Research {state.ResearchPoints}",
                StewardTheme.Label(11, StewardTheme.Text));
            GUILayout.Label($"Exposure {state.Exposure:P0}  •  Visitor {state.Phase}  •  Income +{state.LastContractIncome} / Cost -{state.LastDailyCost}",
                StewardTheme.Label(11, StewardTheme.Muted));
            GUILayout.Label($"Support: power {state.BackupPowerCapacity}  •  repairs {state.RepairBoost:P0}  •  shelter {state.ShelterCapacity}",
                StewardTheme.Label(10, StewardTheme.Muted));
            GUILayout.Label($"Wellbeing: care {state.CareBoost:P0}  •  commons {state.CommonsMoraleBoost:P0}",
                StewardTheme.Label(10, StewardTheme.Muted));
            if (_modes.CurrentMode == InteractionMode.Manage)
            {
                DrawPolicy("Cover", state.CoverPriority, 0, state);
                DrawPolicy("Staffing", state.StaffingPriority, 1, state);
                DrawPolicy("Security", state.SecurityPosture, 2, state);
                GUILayout.Label("Policies affect the next operating day; residents choose shifts autonomously.",
                    StewardTheme.Label(10, StewardTheme.Muted));
            }
            if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message, StewardTheme.Label(10, StewardTheme.Text));
            _scroll = GUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < state.Rooms.Count; i++)
            {
                var room = state.Rooms[i];
                var selected = room.Id == _selectedRoomId;
                if (GUILayout.Button($"{(selected ? "▶ " : "")}{room.Type}  [{room.Staff}/{room.RequiredStaff}]  {(room.IsDisrupted ? "!" : room.IsReachable ? "✓" : "×")}"))
                    _selectedRoomId = room.Id;
                if (selected)
                {
                    GUILayout.Label(room.Cause, StewardTheme.Label(11, StewardTheme.Text));
                    if (room.HasUtilityDiagnostics)
                    {
                        GUILayout.Label("Service routes (diagnostic)", StewardTheme.Label(10, StewardTheme.Muted));
                        GUILayout.Label($"Power route: {room.PowerStatus}", StewardTheme.Label(10, StewardTheme.Text));
                        GUILayout.Label($"Water route: {room.WaterStatus}", StewardTheme.Label(10, StewardTheme.Text));
                    }
                    GUILayout.Label($"Capacity {room.Capacity} • Upkeep {room.DailyUpkeep}/day • Level {room.Depth / 3 + 1}",
                        StewardTheme.Label(10, StewardTheme.Muted));
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPolicy(string name, int value, int field, UndergroundOperationsProjection state)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{name}: {value}", StewardTheme.Label(11, StewardTheme.Text));
            if (GUILayout.Button("−", GUILayout.Width(28))) SetPolicy(field, Mathf.Max(0, value - 1), state);
            if (GUILayout.Button("+", GUILayout.Width(28))) SetPolicy(field, Mathf.Min(2, value + 1), state);
            GUILayout.EndHorizontal();
        }

        private void SetPolicy(int field, int value, UndergroundOperationsProjection state)
        {
            var cover = field == 0 ? value : state.CoverPriority;
            var staffing = field == 1 ? value : state.StaffingPriority;
            var security = field == 2 ? value : state.SecurityPosture;
            var result = _simulation.ExecuteCommand(new SetUndergroundPolicyCommand(cover, staffing, security));
            _message = result.Accepted ? "Policy recorded. Effects begin at the next settlement."
                : result.Rejections[0].Message;
        }

        private void OnDestroy()
        {
            if (_investigator != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_investigator.gameObject);
                else DestroyImmediate(_investigator.gameObject);
            }
        }
    }
}
